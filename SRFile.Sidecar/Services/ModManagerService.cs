using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class ModManagerService
    {
        private readonly RpfService _rpfService;
        private readonly ConcurrentQueue<ModInstallQueueItem> _installQueue = new();
        private readonly ConcurrentDictionary<string, ModInstallQueueItem> _queueItems = new();
        private bool _isProcessingQueue = false;
        private readonly object _queueLock = new();

        public ModManagerService(RpfService rpfService)
        {
            _rpfService = rpfService;
        }

        public string GetModsDirectory(string? customGtaFolder = null)
        {
            string? gta = customGtaFolder ?? _rpfService.GetStatus().GtaFolder;
            if (!string.IsNullOrWhiteSpace(gta) && Directory.Exists(gta))
            {
                string modsPath = Path.Combine(gta, "mods");
                if (!Directory.Exists(modsPath))
                {
                    try { Directory.CreateDirectory(modsPath); } catch { }
                }
                return modsPath;
            }

            // Fallback to local app storage
            string localFallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SRFile",
                "mods"
            );
            if (!Directory.Exists(localFallback))
            {
                Directory.CreateDirectory(localFallback);
            }
            return localFallback;
        }

        public List<ModPackageInfo> GetMods(string? gtaFolder = null)
        {
            string modsDir = GetModsDirectory(gtaFolder);
            var mods = new List<ModPackageInfo>();

            if (!Directory.Exists(modsDir))
                return mods;

            var dirs = Directory.GetDirectories(modsDir);
            foreach (var dir in dirs)
            {
                var dirName = Path.GetFileName(dir);
                if (string.Equals(dirName, "disabled", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isEnabled = !dirName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string cleanName = isEnabled ? dirName : dirName.Substring(0, dirName.Length - ".disabled".Length);
                string modId = cleanName.ToLowerInvariant();

                // Scan files
                var files = new List<string>();
                long totalSize = 0;
                try
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        var rel = Path.GetRelativePath(dir, f).Replace('/', '\\');
                        files.Add(rel);
                        try
                        {
                            totalSize += new FileInfo(f).Length;
                        }
                        catch { }
                    }
                }
                catch { }

                // Determine mod type & metadata
                string type = "Loose";
                string version = "1.0.0";
                string author = "Unknown";
                string description = "Custom GTA V modification package";

                // Check for OIV / assembly.xml
                string assemblyPath = Path.Combine(dir, "assembly.xml");
                if (File.Exists(assemblyPath))
                {
                    type = "OIV";
                    try
                    {
                        var doc = new XmlDocument();
                        doc.Load(assemblyPath);
                        var nameNode = doc.SelectSingleNode("//metadata/name") ?? doc.SelectSingleNode("//package/name") ?? doc.SelectSingleNode("//name") ?? doc.SelectSingleNode("//Name");
                        var authorNode = doc.SelectSingleNode("//metadata/author") ?? doc.SelectSingleNode("//package/author") ?? doc.SelectSingleNode("//author") ?? doc.SelectSingleNode("//Author");
                        var versionNode = doc.SelectSingleNode("//metadata/version") ?? doc.SelectSingleNode("//package/version") ?? doc.SelectSingleNode("//version") ?? doc.SelectSingleNode("//Version");
                        var descNode = doc.SelectSingleNode("//metadata/description") ?? doc.SelectSingleNode("//package/description") ?? doc.SelectSingleNode("//description") ?? doc.SelectSingleNode("//Description");

                        if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
                            cleanName = nameNode.InnerText.Trim();
                        if (authorNode != null) author = authorNode.InnerText.Trim();
                        if (versionNode != null) version = versionNode.InnerText.Trim();
                        if (descNode != null) description = descNode.InnerText.Trim();
                    }
                    catch { }
                }
                else if (files.Any(f => f.EndsWith("dlc.rpf", StringComparison.OrdinalIgnoreCase)))
                {
                    type = "DLC";
                }
                else if (files.Any(f => f.EndsWith(".asi", StringComparison.OrdinalIgnoreCase)))
                {
                    type = "ASI";
                }

                mods.Add(new ModPackageInfo(
                    Id: modId,
                    Name: cleanName,
                    Type: type,
                    IsEnabled: isEnabled,
                    Version: version,
                    Author: author,
                    Description: description,
                    SourcePath: dir,
                    InstallPath: dir,
                    Size: totalSize,
                    FileCount: files.Count,
                    Files: files,
                    Conflicts: new List<string>()
                ));
            }

            // Conflict detection across enabled mods
            var enabledMods = mods.Where(m => m.IsEnabled).ToList();
            var fileMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var mod in enabledMods)
            {
                foreach (var f in mod.Files)
                {
                    if (!fileMap.TryGetValue(f, out var list))
                    {
                        list = new List<string>();
                        fileMap[f] = list;
                    }
                    list.Add(mod.Name);
                }
            }

            var conflictingFiles = fileMap.Where(kv => kv.Value.Count > 1).ToDictionary(kv => kv.Key, kv => kv.Value);

            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                if (!mod.IsEnabled) continue;

                var modConflicts = new HashSet<string>();
                foreach (var f in mod.Files)
                {
                    if (conflictingFiles.TryGetValue(f, out var otherMods))
                    {
                        foreach (var other in otherMods)
                        {
                            if (!string.Equals(other, mod.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                modConflicts.Add($"{f} (conflit avec {other})");
                            }
                        }
                    }
                }

                if (modConflicts.Count > 0)
                {
                    mods[i] = mod with { Conflicts = modConflicts.ToList() };
                }
            }

            return mods;
        }

        public bool ToggleMod(string modId, bool enabled, string? gtaFolder = null)
        {
            string modsDir = GetModsDirectory(gtaFolder);
            if (!Directory.Exists(modsDir)) return false;

            var cleanId = modId.Trim().ToLowerInvariant();
            var dirs = Directory.GetDirectories(modsDir);

            foreach (var dir in dirs)
            {
                var dirName = Path.GetFileName(dir);
                bool isCurrentlyEnabled = !dirName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string baseName = isCurrentlyEnabled ? dirName : dirName.Substring(0, dirName.Length - ".disabled".Length);

                if (string.Equals(baseName.ToLowerInvariant(), cleanId, StringComparison.OrdinalIgnoreCase))
                {
                    if (enabled && !isCurrentlyEnabled)
                    {
                        string targetDir = Path.Combine(modsDir, baseName);
                        if (Directory.Exists(targetDir))
                            Directory.Delete(targetDir, true);
                        Directory.Move(dir, targetDir);
                        return true;
                    }
                    else if (!enabled && isCurrentlyEnabled)
                    {
                        string targetDir = Path.Combine(modsDir, baseName + ".disabled");
                        if (Directory.Exists(targetDir))
                            Directory.Delete(targetDir, true);
                        Directory.Move(dir, targetDir);
                        return true;
                    }
                    return true;
                }
            }

            return false;
        }

        public OivManifestInfo InspectOiv(string oivFilePath)
        {
            if (!File.Exists(oivFilePath))
                throw new FileNotFoundException($"Fichier OIV introuvable: {oivFilePath}");

            using var archive = ZipFile.OpenRead(oivFilePath);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, "assembly.xml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(e.FullName), "assembly.xml", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                throw new InvalidDataException("Le package OIV ne contient pas de fichier manifest 'assembly.xml'.");
            }

            using var reader = new StreamReader(entry.Open());
            string xmlContent = reader.ReadToEnd();

            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            string name = Path.GetFileNameWithoutExtension(oivFilePath);
            string author = "Auteur inconnu";
            string version = "1.0.0";
            string description = "Package OpenIV d'installation";

            var nameNode = doc.SelectSingleNode("//metadata/name") ?? doc.SelectSingleNode("//package/name") ?? doc.SelectSingleNode("//name") ?? doc.SelectSingleNode("//Name");
            if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
                name = nameNode.InnerText.Trim();

            var authorNode = doc.SelectSingleNode("//metadata/author") ?? doc.SelectSingleNode("//package/author") ?? doc.SelectSingleNode("//author") ?? doc.SelectSingleNode("//Author");
            if (authorNode != null && !string.IsNullOrWhiteSpace(authorNode.InnerText))
                author = authorNode.InnerText.Trim();

            var versionNode = doc.SelectSingleNode("//metadata/version") ?? doc.SelectSingleNode("//package/version") ?? doc.SelectSingleNode("//version") ?? doc.SelectSingleNode("//Version");
            if (versionNode != null && !string.IsNullOrWhiteSpace(versionNode.InnerText))
                version = versionNode.InnerText.Trim();

            var descNode = doc.SelectSingleNode("//metadata/description") ?? doc.SelectSingleNode("//package/description") ?? doc.SelectSingleNode("//description") ?? doc.SelectSingleNode("//Description");
            if (descNode != null && !string.IsNullOrWhiteSpace(descNode.InnerText))
                description = descNode.InnerText.Trim();

            var targets = new List<string>();
            var archiveNodes = doc.SelectNodes("//archive");
            if (archiveNodes != null)
            {
                foreach (XmlNode n in archiveNodes)
                {
                    var p = n.Attributes?["path"]?.Value;
                    if (!string.IsNullOrEmpty(p) && !targets.Contains(p))
                        targets.Add(p);
                }
            }

            var addNodes = doc.SelectNodes("//add");
            if (addNodes != null)
            {
                foreach (XmlNode n in addNodes)
                {
                    var p = n.Attributes?["source"]?.Value;
                    if (!string.IsNullOrEmpty(p) && !targets.Contains(p))
                        targets.Add(p);
                }
            }

            int actions = (archiveNodes?.Count ?? 0) + (addNodes?.Count ?? 0);

            return new OivManifestInfo(
                Name: name,
                Author: author,
                Version: version,
                Description: description,
                TargetComponents: targets,
                ActionCount: Math.Max(actions, 1),
                RawXml: xmlContent
            );
        }

        public ModInstallQueueItem QueueInstall(string packagePath, bool enableAfterInstall = true, string? gtaFolder = null)
        {
            if (!File.Exists(packagePath) && !Directory.Exists(packagePath))
                throw new FileNotFoundException($"Fichier ou dossier d'installation introuvable: {packagePath}");

            string id = Guid.NewGuid().ToString("N");
            string pkgName = Path.GetFileNameWithoutExtension(packagePath);

            var item = new ModInstallQueueItem(
                Id: id,
                PackagePath: packagePath,
                PackageName: pkgName,
                Status: "Queued",
                Progress: 0.0f,
                Message: "En attente de traitement..."
            );

            _queueItems[id] = item;
            _installQueue.Enqueue(item);

            TriggerQueueProcessing(gtaFolder, enableAfterInstall);
            return item;
        }

        public List<ModInstallQueueItem> GetQueueStatus()
        {
            return _queueItems.Values.OrderBy(i => i.Id).ToList();
        }

        public bool UninstallMod(string modId, string? gtaFolder = null)
        {
            string modsDir = GetModsDirectory(gtaFolder);
            if (!Directory.Exists(modsDir)) return false;

            var cleanId = modId.Trim().ToLowerInvariant();
            var dirs = Directory.GetDirectories(modsDir);

            foreach (var dir in dirs)
            {
                var dirName = Path.GetFileName(dir);
                bool isEnabled = !dirName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                string baseName = isEnabled ? dirName : dirName.Substring(0, dirName.Length - ".disabled".Length);

                if (string.Equals(baseName.ToLowerInvariant(), cleanId, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        throw new IOException($"Erreur lors de la suppression du mod: {ex.Message}");
                    }
                }
            }
            return false;
        }

        private void TriggerQueueProcessing(string? gtaFolder, bool enableAfterInstall)
        {
            lock (_queueLock)
            {
                if (_isProcessingQueue) return;
                _isProcessingQueue = true;
            }

            Task.Run(() =>
            {
                try
                {
                    while (_installQueue.TryDequeue(out var item))
                    {
                        ProcessQueueItem(item, gtaFolder, enableAfterInstall);
                    }
                }
                finally
                {
                    lock (_queueLock)
                    {
                        _isProcessingQueue = false;
                    }
                }
            });
        }

        private void ProcessQueueItem(ModInstallQueueItem item, string? gtaFolder, bool enableAfterInstall)
        {
            try
            {
                UpdateQueueItem(item.Id, "Installing", 0.1f, "Vérification du paquet...");

                string modsDir = GetModsDirectory(gtaFolder);
                string ext = Path.GetExtension(item.PackagePath).ToLowerInvariant();

                if (ext == ".oiv")
                {
                    UpdateQueueItem(item.Id, "Installing", 0.3f, "Extraction du package OIV...");
                    string targetModDir = Path.Combine(modsDir, item.PackageName + (enableAfterInstall ? "" : ".disabled"));
                    if (Directory.Exists(targetModDir))
                        Directory.Delete(targetModDir, true);

                    ZipFile.ExtractToDirectory(item.PackagePath, targetModDir);
                    UpdateQueueItem(item.Id, "Completed", 1.0f, "Installation OIV réussie !");
                }
                else if (ext == ".rpf")
                {
                    UpdateQueueItem(item.Id, "Installing", 0.4f, "Déploiement du conteneur RPF...");
                    string dlcpacksDir = Path.Combine(modsDir, "update", "x64", "dlcpacks", item.PackageName);
                    if (!enableAfterInstall) dlcpacksDir += ".disabled";
                    Directory.CreateDirectory(dlcpacksDir);

                    string destFile = Path.Combine(dlcpacksDir, "dlc.rpf");
                    File.Copy(item.PackagePath, destFile, true);
                    UpdateQueueItem(item.Id, "Completed", 1.0f, "Archive DLC RPF déployée avec succès.");
                }
                else if (ext == ".asi")
                {
                    UpdateQueueItem(item.Id, "Installing", 0.5f, "Copie du script ASI...");
                    string asiDir = Path.Combine(modsDir, item.PackageName + (enableAfterInstall ? "" : ".disabled"));
                    Directory.CreateDirectory(asiDir);
                    File.Copy(item.PackagePath, Path.Combine(asiDir, Path.GetFileName(item.PackagePath)), true);
                    UpdateQueueItem(item.Id, "Completed", 1.0f, "Plugin ASI installé avec succès.");
                }
                else if (Directory.Exists(item.PackagePath))
                {
                    UpdateQueueItem(item.Id, "Installing", 0.5f, "Copie du dossier de mod...");
                    string targetModDir = Path.Combine(modsDir, item.PackageName + (enableAfterInstall ? "" : ".disabled"));
                    CopyDirectory(item.PackagePath, targetModDir);
                    UpdateQueueItem(item.Id, "Completed", 1.0f, "Mod installé avec succès.");
                }
                else
                {
                    UpdateQueueItem(item.Id, "Failed", 1.0f, $"Type de fichier non supporté: {ext}");
                }
            }
            catch (Exception ex)
            {
                UpdateQueueItem(item.Id, "Failed", 1.0f, $"Erreur: {ex.Message}");
            }
        }

        private void UpdateQueueItem(string id, string status, float progress, string message)
        {
            if (_queueItems.TryGetValue(id, out var current))
            {
                _queueItems[id] = current with { Status = status, Progress = progress, Message = message };
            }
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }
            foreach (var sub in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(sub, Path.Combine(targetDir, Path.GetFileName(sub)));
            }
        }
    }
}
