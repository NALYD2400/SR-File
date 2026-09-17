using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeWalker.Core.Utils;
using CodeWalker.GameFiles;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class Gen9ConverterService
    {
        private readonly ConcurrentQueue<string> _logs = new();
        private const int MaxLogLines = 1000;
        private CancellationTokenSource? _cts;
        private readonly object _stateLock = new();

        private bool _isConverting = false;
        private float _progress = 0.0f;
        private string _currentFile = string.Empty;
        private int _totalFiles = 0;
        private int _processedFiles = 0;
        private string? _lastError = null;

        public Gen9ConversionStatus GetStatus()
        {
            lock (_stateLock)
            {
                return new Gen9ConversionStatus(
                    IsConverting: _isConverting,
                    Progress: _progress,
                    CurrentFile: _currentFile,
                    TotalFiles: _totalFiles,
                    ProcessedFiles: _processedFiles,
                    Logs: _logs.ToList(),
                    Error: _lastError
                );
            }
        }

        public void ClearLogs()
        {
            while (_logs.TryDequeue(out _)) { }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (_isConverting && _cts != null)
                {
                    _cts.Cancel();
                    AddLog("[ANNULATION] Arrêt de la conversion demandé par l'utilisateur.");
                }
            }
        }

        public Gen9ConversionStatus StartConversion(Gen9ConversionRequest req)
        {
            lock (_stateLock)
            {
                if (_isConverting)
                {
                    throw new InvalidOperationException("Une conversion est déjà en cours d'exécution.");
                }

                if (string.IsNullOrWhiteSpace(req.InputPath) || (!Directory.Exists(req.InputPath) && !File.Exists(req.InputPath)))
                {
                    throw new FileNotFoundException($"Chemin source introuvable: {req.InputPath}");
                }

                if (string.IsNullOrWhiteSpace(req.OutputPath))
                {
                    throw new ArgumentException("Le dossier de destination ne peut pas être vide.", nameof(req.OutputPath));
                }

                _isConverting = true;
                _progress = 0.0f;
                _currentFile = string.Empty;
                _totalFiles = 0;
                _processedFiles = 0;
                _lastError = null;
                _cts = new CancellationTokenSource();
            }

            AddLog($"[DÉMARRAGE] Initialisation de la conversion (Preset: {req.Preset})...");
            AddLog($"Source: {req.InputPath}");
            AddLog($"Destination: {req.OutputPath}");

            var token = _cts.Token;
            Task.Run(() => ExecuteConversion(req, token));

            return GetStatus();
        }

        private void ExecuteConversion(Gen9ConversionRequest req, CancellationToken token)
        {
            var exgen9 = RpfManager.IsGen9;
            RpfManager.IsGen9 = true;

            try
            {
                if (File.Exists(req.InputPath))
                {
                    // Single file conversion
                    lock (_stateLock)
                    {
                        _totalFiles = 1;
                        _currentFile = Path.GetFileName(req.InputPath);
                    }

                    Directory.CreateDirectory(req.OutputPath);
                    string destPath = Path.Combine(req.OutputPath, Path.GetFileName(req.InputPath));

                    ConvertSingleFile(req.InputPath, destPath, req.Preset, req.CopyUnconverted, token);

                    lock (_stateLock)
                    {
                        _processedFiles = 1;
                        _progress = 1.0f;
                    }
                }
                else if (Directory.Exists(req.InputPath))
                {
                    // Directory conversion
                    var searchOption = req.ProcessSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var allFiles = Directory.GetFiles(req.InputPath, "*", searchOption);

                    lock (_stateLock)
                    {
                        _totalFiles = allFiles.Length;
                    }

                    AddLog($"Total de fichiers découverts: {allFiles.Length}");

                    for (int i = 0; i < allFiles.Length; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            AddLog("[ARRÊTÉ] Processus interrompu par l'utilisateur.");
                            break;
                        }

                        var file = allFiles[i];
                        var rel = Path.GetRelativePath(req.InputPath, file);
                        var targetFile = Path.Combine(req.OutputPath, rel);

                        lock (_stateLock)
                        {
                            _currentFile = rel;
                            _processedFiles = i + 1;
                            _progress = (float)(i + 1) / Math.Max(1, allFiles.Length);
                        }

                        if (!req.OverwriteExisting && File.Exists(targetFile))
                        {
                            AddLog($"[IGNORÉ] Le fichier existe déjà: {rel}");
                            continue;
                        }

                        string? targetDir = Path.GetDirectoryName(targetFile);
                        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        try
                        {
                            ConvertSingleFile(file, targetFile, req.Preset, req.CopyUnconverted, token);
                        }
                        catch (Exception ex)
                        {
                            AddLog($"[ERREUR] Impossible de convertir {rel}: {ex.Message}");
                        }
                    }
                }

                AddLog("[TERMINÉ] Traitement du lot de conversion finalisé avec succès.");
            }
            catch (Exception ex)
            {
                lock (_stateLock)
                {
                    _lastError = ex.Message;
                }
                AddLog($"[ERREUR FATALE] {ex.Message}");
            }
            finally
            {
                RpfManager.IsGen9 = exgen9;
                lock (_stateLock)
                {
                    _isConverting = false;
                }
            }
        }

        private void ConvertSingleFile(string sourceFile, string targetFile, string preset, bool copyUnconverted, CancellationToken token)
        {
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();

            // Handle RPF archives directly
            if (ext == ".rpf")
            {
                ConvertRpf(sourceFile, targetFile, preset, copyUnconverted, token);
                return;
            }

            // Check if file type is targeted by current preset
            bool isTargetType = ext == ".ytd" ||
                (preset != "textures_only" && (ext == ".ydr" || ext == ".ydd" || ext == ".yft" || ext == ".ydb" || ext == ".ypt"));

            if (isTargetType)
            {
                byte[] inData = File.ReadAllBytes(sourceFile);
                bool converted;
                byte[]? outData = Gen9Converter.TryConvert(inData, ext, msg => AddLog($"  {msg}"), Path.GetFileName(sourceFile), copyUnconverted, out converted);

                if (outData != null && (converted || copyUnconverted))
                {
                    File.WriteAllBytes(targetFile, outData);
                    AddLog($"[SUCCÈS] {Path.GetFileName(sourceFile)} -> {(converted ? "Converti" : "Copié")}");
                    return;
                }
            }

            // Fallback for non-resource or unconverted files
            if (copyUnconverted)
            {
                File.Copy(sourceFile, targetFile, true);
                AddLog($"[COPIÉ] {Path.GetFileName(sourceFile)}");
            }
            else
            {
                AddLog($"[IGNORÉ] Aucun convertisseur pour {Path.GetFileName(sourceFile)}");
            }
        }

        private void ConvertRpf(string sourceFile, string targetFile, string preset, bool copyUnconverted, CancellationToken token)
        {
            AddLog($"[RPF DÉBUT] Analyse et conversion du conteneur {Path.GetFileName(sourceFile)}...");
            if (File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
            File.Copy(sourceFile, targetFile, true);

            var rpf = new RpfFile(targetFile, Path.GetFileName(targetFile));
            rpf.ScanStructure(msg => AddLog($"  {msg}"), msg => AddLog($"  {msg}"));

            var rpflist = new List<RpfFile>();
            var rpfstack = new Stack<RpfFile>();
            rpfstack.Push(rpf);
            while (rpfstack.Count > 0)
            {
                var trpf = rpfstack.Pop();
                if (trpf == null) continue;
                if (trpf.Children != null)
                {
                    foreach (var crpf in trpf.Children)
                    {
                        rpfstack.Push(crpf);
                    }
                }
                rpflist.Add(trpf);
            }
            rpflist.Reverse();

            var changedparents = new HashSet<RpfFile>();
            foreach (var trpf in rpflist)
            {
                if (token.IsCancellationRequested) break;
                if (trpf?.AllEntries == null) continue;

                bool changed = changedparents.Contains(trpf);
                var allentries = new List<RpfResourceFileEntry>();
                foreach (var entry in trpf.AllEntries)
                {
                    if (entry is RpfResourceFileEntry rfe) allentries.Add(rfe);
                }
                allentries.Sort((a, b) => a.FileOffset.CompareTo(b.FileOffset));

                foreach (var rfe in allentries)
                {
                    if (token.IsCancellationRequested) break;
                    string entryExt = Path.GetExtension(rfe.NameLower);
                    if (preset == "textures_only" && entryExt != ".ytd") continue;
                    if (!Gen9Converter.RequiresConversion(rfe)) continue;

                    var dir = rfe.Parent;
                    var name = rfe.Name;
                    var datain = trpf.ExtractFile(rfe);
                    if (datain == null) continue;

                    datain = ResourceBuilder.Compress(datain);
                    datain = ResourceBuilder.AddResourceHeader(rfe, datain);

                    var dataout = Gen9Converter.TryConvert(datain, entryExt, msg => AddLog($"    {msg}"), rfe.Path, false, out var converted);
                    if (converted && dataout != null)
                    {
                        RpfFile.CreateFile(dir, name, dataout, true);
                        changed = true;
                    }
                }

                if (changed)
                {
                    AddLog($"  [DÉFRAGMENTATION] Optimisation de {trpf.Name}...");
                    RpfFile.Defragment(trpf, null, false);
                    if (trpf.Parent != null)
                    {
                        changedparents.Add(trpf.Parent);
                    }
                }
            }

            AddLog($"[RPF SUCCÈS] {Path.GetFileName(sourceFile)} conteneur converti avec succès.");
        }

        private void AddLog(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            _logs.Enqueue(line);
            while (_logs.Count > MaxLogLines)
            {
                _logs.TryDequeue(out _);
            }
        }
    }
}
