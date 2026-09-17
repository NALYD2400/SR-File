using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using CodeWalker.GameFiles;
using CodeWalker.Utils;
using SRFile.Sidecar.Models;
using SRFile.Sidecar.Utils;

namespace SRFile.Sidecar.Services
{
    public class RpfService
    {
        private readonly ConcurrentDictionary<string, RpfFile> _loadedRpfs = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, RpfIndex> _rpfIndexes = new(StringComparer.OrdinalIgnoreCase);
        private readonly DateTime _startTime = DateTime.UtcNow;
        private long _cacheHits;
        private long _cacheMisses;

        public long CacheHits => Interlocked.Read(ref _cacheHits);
        public long CacheMisses => Interlocked.Read(ref _cacheMisses);

        public string? GtaFolder { get; private set; }
        public bool IsGen9 { get; private set; }
        public string? AesKey { get; private set; }
        public bool KeysLoaded => GTA5Keys.PC_AES_KEY != null && GTA5Keys.PC_AES_KEY.Length > 0;

        public AppStatus GetStatus()
        {
            long uptime = (long)(DateTime.UtcNow - _startTime).TotalSeconds;
            return new AppStatus(
                Running: true,
                Version: "1.0.0",
                GtaFolder: GtaFolder,
                IsGen9: IsGen9,
                KeysLoaded: KeysLoaded,
                LoadedRpfsCount: _loadedRpfs.Count,
                UptimeSeconds: uptime
            );
        }

        public bool ConfigureGtaFolder(string path, bool? isGen9, string? aesKey)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            GtaFolder = Path.GetFullPath(path);
            IsGen9 = isGen9 ?? false;
            AesKey = aesKey;

            try
            {
                GTA5Keys.LoadFromPath(GtaFolder, IsGen9, AesKey);
                return KeysLoaded;
            }
            catch (Exception)
            {
                // Fallback attempt without exe if key provided or using default magic
                try
                {
                    GTA5Keys.LoadFromPath(".\\Keys", IsGen9, AesKey);
                    return KeysLoaded;
                }
                catch
                {
                    return false;
                }
            }
        }

        public RpfInfo OpenRpf(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"RPF archive not found at: {fullPath}");
            }

            if (_loadedRpfs.TryGetValue(fullPath, out var cached))
            {
                return ToRpfInfo(cached);
            }

            var rpf = new RpfFile(fullPath, Path.GetFileName(fullPath));
            rpf.ScanStructure(status => { }, error => { });

            if (rpf.LastException != null)
            {
                throw new InvalidOperationException($"Failed to scan RPF structure: {rpf.LastError ?? rpf.LastException.Message}", rpf.LastException);
            }

            _loadedRpfs[fullPath] = rpf;
            _rpfIndexes[fullPath] = RpfIndex.Build(rpf);
            return ToRpfInfo(rpf);
        }

        public RpfInfo? GetRpfInfo(string rpfPath)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            return rpf != null ? ToRpfInfo(rpf) : null;
        }

        public List<RpfEntryDto> GetEntries(string rpfPath, string dirPath = "")
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var results = new List<RpfEntryDto>();

            RpfDirectoryEntry? currentDir = FindDirectory(rpf, dirPath);
            if (currentDir == null) return results;

            // Directories first
            if (currentDir.Directories != null)
            {
                foreach (var dir in currentDir.Directories.OrderBy(d => d.Name))
                {
                    results.Add(new RpfEntryDto(
                        Name: dir.Name,
                        Path: dir.Path,
                        IsDirectory: true,
                        IsResource: false,
                        Size: 0,
                        CompressedSize: 0,
                        ResourceType: "Directory",
                        Encryption: "None"
                    ));
                }
            }

            // Files next
            if (currentDir.Files != null)
            {
                foreach (var file in currentDir.Files.OrderBy(f => f.Name))
                {
                    bool isRes = file is RpfResourceFileEntry;
                    string resType = Path.GetExtension(file.Name).TrimStart('.').ToLowerInvariant();
                    long size = file.FileSize;
                    long compSize = 0;
                    string enc = "None";

                    if (file is RpfBinaryFileEntry bin)
                    {
                        size = bin.FileUncompressedSize;
                        compSize = bin.FileSize;
                        if (bin.IsEncrypted) enc = "Encrypted";
                    }

                    if (file.NameLower != null && file.NameLower.EndsWith(".rpf"))
                    {
                        resType = "rpf";
                    }

                    results.Add(new RpfEntryDto(
                        Name: file.Name,
                        Path: file.Path,
                        IsDirectory: false,
                        IsResource: isRes,
                        Size: size,
                        CompressedSize: compSize,
                        ResourceType: resType,
                        Encryption: enc
                    ));
                }
            }

            return results;
        }

        public List<RpfEntryDto> SearchEntries(string rpfPath, string query, int maxResults = 250)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var results = new List<RpfEntryDto>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            string q = query.Trim().ToLowerInvariant();

            foreach (var currentRpf in GetAllRpfs(rpf))
            {
                if (currentRpf.AllEntries == null) continue;

                foreach (var entry in currentRpf.AllEntries)
                {
                    if (entry.NameLower != null && entry.NameLower.Contains(q))
                    {
                        bool isDir = entry is RpfDirectoryEntry;
                        bool isRes = entry is RpfResourceFileEntry;
                        long size = 0;
                        long compSize = 0;
                        string enc = "None";

                        if (entry is RpfBinaryFileEntry bin)
                        {
                            size = bin.FileUncompressedSize;
                            compSize = bin.FileSize;
                            if (bin.IsEncrypted) enc = "Encrypted";
                        }
                        else if (entry is RpfFileEntry fe)
                        {
                            size = fe.FileSize;
                        }

                        string resType = isDir ? "Directory" : Path.GetExtension(entry.Name).TrimStart('.').ToLowerInvariant();
                        if (!isDir && entry.NameLower != null && entry.NameLower.EndsWith(".rpf"))
                        {
                            resType = "rpf";
                        }

                        results.Add(new RpfEntryDto(
                            Name: entry.Name,
                            Path: entry.Path,
                            IsDirectory: isDir,
                            IsResource: isRes,
                            Size: size,
                            CompressedSize: compSize,
                            ResourceType: resType,
                            Encryption: enc
                        ));

                        if (results.Count >= maxResults) return results;
                    }
                }
            }

            return results;
        }

        public byte[] ExtractFile(string rpfPath, string entryPath)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[]? data = entry.File.ExtractFile(entry);
            if (data == null) throw new InvalidDataException($"Failed to extract data for '{entryPath}' from archive '{rpfPath}'.");

            return data;
        }

        public string ExtractFileText(string rpfPath, string entryPath, int maxChars = 100000)
        {
            byte[] bytes = ExtractFile(rpfPath, entryPath);
            if (bytes == null || bytes.Length == 0) return string.Empty;

            // Check if it might be binary (high proportion of null bytes in first 512 bytes)
            int checkLen = Math.Min(bytes.Length, 512);
            int nullCount = 0;
            for (int i = 0; i < checkLen; i++)
            {
                if (bytes[i] == 0) nullCount++;
            }
            if (nullCount > checkLen / 10)
            {
                return $"[Fichier binaire non textuel - {bytes.Length} octets]";
            }

            int bytesToDecode = Math.Min(bytes.Length, maxChars * 2);
            string text = Encoding.UTF8.GetString(bytes, 0, bytesToDecode);
            if (text.Length > maxChars)
            {
                text = text.Substring(0, maxChars) + "\n... [Contenu tronqué pour fluidité]";
            }
            return text;
        }

        public List<TextureItemDto> GetTextures(string rpfPath, string entryPath)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[] data = entry.File.ExtractFile(entry);
            var ytd = new YtdFile();
            ytd.Load(data, entry);

            var list = new List<TextureItemDto>();
            if (ytd.TextureDict?.Textures?.data_items != null)
            {
                foreach (var tex in ytd.TextureDict.Textures.data_items)
                {
                    if (tex == null) continue;
                    list.Add(new TextureItemDto(
                        Name: tex.Name ?? "Unnamed",
                        Width: (int)tex.Width,
                        Height: (int)tex.Height,
                        Format: tex.Format.ToString(),
                        MipCount: (int)tex.Levels
                    ));
                }
            }

            return list;
        }

        public byte[] GetTexturePng(string rpfPath, string entryPath, string textureName, int mip = 0)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[] data = entry.File.ExtractFile(entry);
            var ytd = new YtdFile();
            ytd.Load(data, entry);

            var tex = ytd.TextureDict?.Textures?.data_items?
                .FirstOrDefault(t => string.Equals(t?.Name, textureName, StringComparison.OrdinalIgnoreCase));

            if (tex == null) throw new FileNotFoundException($"Texture '{textureName}' not found in {entryPath}");

            int clampedMip = Math.Max(0, mip);
            if (tex.Levels > 0)
            {
                clampedMip = Math.Min(clampedMip, (int)tex.Levels - 1);
            }

            byte[] pixels = DDSIO.GetPixels(tex, clampedMip);
            if (pixels == null) throw new InvalidOperationException($"Could not decompress pixels for texture '{textureName}'");

            int mipW = Math.Max(1, (int)tex.Width >> clampedMip);
            int mipH = Math.Max(1, (int)tex.Height >> clampedMip);

            return PngEncoder.EncodeRgba(pixels, mipW, mipH);
        }

        public byte[] GetTextureDds(string rpfPath, string entryPath, string textureName)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[] data = entry.File.ExtractFile(entry);
            var ytd = new YtdFile();
            ytd.Load(data, entry);

            var tex = ytd.TextureDict?.Textures?.data_items?
                .FirstOrDefault(t => string.Equals(t?.Name, textureName, StringComparison.OrdinalIgnoreCase));

            if (tex == null) throw new FileNotFoundException($"Texture '{textureName}' not found in {entryPath}");

            return DDSIO.GetDDSFile(tex);
        }

        public List<AudioStreamDto> GetAudioStreams(string rpfPath, string entryPath)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[] data = entry.File.ExtractFile(entry);
            var awc = new AwcFile();
            awc.Load(data, entry);

            var list = new List<AudioStreamDto>();
            if (awc.Streams != null)
            {
                for (int i = 0; i < awc.Streams.Length; i++)
                {
                    var stream = awc.Streams[i];
                    if (stream == null) continue;

                    list.Add(new AudioStreamDto(
                        Index: i,
                        Name: stream.Name ?? $"Track_{i}",
                        Length: stream.Length,
                        SampleRate: stream.SamplesPerSecond,
                        Channels: stream.ChannelStreams != null ? stream.ChannelStreams.Length : 1,
                        Type: stream.Type ?? "Audio",
                        ByteLength: stream.ByteLength
                    ));
                }
            }

            return list;
        }

        public Stream GetAudioWav(string rpfPath, string entryPath, int streamIndex)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var entry = FindFileEntry(rpf, entryPath);
            if (entry == null) throw new FileNotFoundException($"Entry not found: {entryPath}");

            byte[] data = entry.File.ExtractFile(entry);
            var awc = new AwcFile();
            awc.Load(data, entry);

            if (awc.Streams == null || streamIndex < 0 || streamIndex >= awc.Streams.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(streamIndex), "Invalid audio stream index.");
            }

            var stream = awc.Streams[streamIndex];
            var wavStream = stream.GetWavStream();
            if (wavStream == null)
            {
                throw new InvalidOperationException($"Could not produce WAV stream for audio stream {streamIndex}");
            }

            if (wavStream.CanSeek)
            {
                wavStream.Position = 0;
            }
            return wavStream;
        }

        public static IEnumerable<RpfFile> GetAllRpfs(RpfFile root)
        {
            if (root == null) yield break;
            yield return root;
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                {
                    foreach (var sub in GetAllRpfs(child))
                    {
                        yield return sub;
                    }
                }
            }
        }

        private RpfFile? GetOrLoadRpf(string rpfPath)
        {
            string full = Path.GetFullPath(rpfPath);
            if (_loadedRpfs.TryGetValue(full, out var rpf)) return rpf;
            if (File.Exists(full))
            {
                OpenRpf(full);
                _loadedRpfs.TryGetValue(full, out rpf);
                return rpf;
            }
            return null;
        }

        public RpfFile? GetLoadedRpf(string rpfPath) => GetOrLoadRpf(rpfPath);

        public bool CloseRpf(string filePath)
        {
            string full = Path.GetFullPath(filePath);
            bool removed = _loadedRpfs.TryRemove(full, out _);
            _rpfIndexes.TryRemove(full, out _);
            return removed;
        }

        public int ClearCache()
        {
            int count = _loadedRpfs.Count;
            _loadedRpfs.Clear();
            _rpfIndexes.Clear();
            return count;
        }

        public RpfCacheStats GetCacheStats()
        {
            long hits = CacheHits;
            long misses = CacheMisses;
            long total = hits + misses;
            double rate = total > 0 ? Math.Round((double)hits / total * 100.0, 2) : 0.0;

            long totalIndexed = _rpfIndexes.Values.Sum(idx => (long)idx.FileLookup.Count + idx.DirLookup.Count);

            var archives = _loadedRpfs.Select(kvp => new RpfCacheItemDto(
                Name: kvp.Value.Name ?? Path.GetFileName(kvp.Key),
                FilePath: kvp.Key,
                FileSize: kvp.Value.FileSize,
                EntryCount: kvp.Value.EntryCount
            )).ToList();

            return new RpfCacheStats(
                LoadedRpfsCount: _loadedRpfs.Count,
                TotalIndexedEntries: totalIndexed,
                CacheHits: hits,
                CacheMisses: misses,
                HitRatePercent: rate,
                OpenArchives: archives
            );
        }

        public BatchExtractResultDto ExtractFolderToDisk(string rpfPath, string folderPath, string outputDir, bool recursive = true)
        {
            var sw = Stopwatch.StartNew();
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var dir = FindDirectory(rpf, folderPath);
            if (dir == null) throw new DirectoryNotFoundException($"Directory '{folderPath}' not found in RPF.");

            var fileList = new List<(RpfFileEntry File, string RelPath)>();
            CollectDirectoryFiles(dir, "", recursive, fileList);

            int extracted = 0;
            int errors = 0;
            long totalBytes = 0;
            var errorList = new List<string>();

            Directory.CreateDirectory(outputDir);

            foreach (var (entry, relPath) in fileList)
            {
                try
                {
                    byte[]? data = entry.File.ExtractFile(entry);
                    if (data == null)
                    {
                        errors++;
                        errorList.Add($"{entry.Path}: Échec d'extraction de données (clé ou archive corrompue).");
                        continue;
                    }

                    string dest = Path.Combine(outputDir, relPath);
                    string? pDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(pDir) && !Directory.Exists(pDir))
                    {
                        Directory.CreateDirectory(pDir);
                    }
                    File.WriteAllBytes(dest, data);
                    extracted++;
                    totalBytes += data.Length;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorList.Add($"{entry.Path}: {ex.Message}");
                }
            }

            sw.Stop();
            return new BatchExtractResultDto(
                Success: errors == 0,
                ExtractedCount: extracted,
                ErrorCount: errors,
                TotalBytes: totalBytes,
                DurationMs: sw.ElapsedMilliseconds,
                OutputDirectory: outputDir,
                Errors: errorList
            );
        }

        public Stream ExtractFolderToZipStream(string rpfPath, string folderPath, bool recursive = true)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var dir = FindDirectory(rpf, folderPath);
            if (dir == null) throw new DirectoryNotFoundException($"Directory '{folderPath}' not found in RPF.");

            var fileList = new List<(RpfFileEntry File, string RelPath)>();
            CollectDirectoryFiles(dir, "", recursive, fileList);

            string tempFile = Path.Combine(Path.GetTempPath(), $"srfile_export_{Guid.NewGuid():N}.zip");
            var fs = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose);

            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (entry, relPath) in fileList)
                {
                    try
                    {
                        byte[]? data = entry.File.ExtractFile(entry);
                        if (data == null) continue;

                        var zipEntry = archive.CreateEntry(relPath.Replace('\\', '/'), CompressionLevel.Fastest);
                        using var s = zipEntry.Open();
                        s.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        // Ignore individual extraction failures in zip archive
                    }
                }
            }

            fs.Position = 0;
            return fs;
        }

        public byte[] ExtractFolderToZip(string rpfPath, string folderPath, bool recursive = true)
        {
            using var stream = ExtractFolderToZipStream(rpfPath, folderPath, recursive);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public BatchExtractResultDto ExtractBatchToDisk(string rpfPath, List<string> entryPaths, string outputDir)
        {
            var sw = Stopwatch.StartNew();
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            int extracted = 0;
            int errors = 0;
            long totalBytes = 0;
            var errorList = new List<string>();

            Directory.CreateDirectory(outputDir);
            var writtenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in entryPaths)
            {
                try
                {
                    var entry = FindFileEntry(rpf, path);
                    if (entry == null)
                    {
                        errors++;
                        errorList.Add($"Entry not found: {path}");
                        continue;
                    }

                    byte[]? data = entry.File.ExtractFile(entry);
                    if (data == null)
                    {
                        errors++;
                        errorList.Add($"Failed to extract: {path}");
                        continue;
                    }

                    string outFilename = entry.Name;
                    if (!writtenNames.Add(outFilename))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(entry.Name);
                        string ext = Path.GetExtension(entry.Name);
                        outFilename = $"{nameWithoutExt}_{extracted + 1}{ext}";
                        writtenNames.Add(outFilename);
                    }

                    string dest = Path.Combine(outputDir, outFilename);
                    File.WriteAllBytes(dest, data);
                    extracted++;
                    totalBytes += data.Length;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorList.Add($"{path}: {ex.Message}");
                }
            }

            sw.Stop();
            return new BatchExtractResultDto(
                Success: errors == 0,
                ExtractedCount: extracted,
                ErrorCount: errors,
                TotalBytes: totalBytes,
                DurationMs: sw.ElapsedMilliseconds,
                OutputDirectory: outputDir,
                Errors: errorList
            );
        }

        public Stream ExtractBatchToZipStream(string rpfPath, List<string> entryPaths)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            string tempFile = Path.Combine(Path.GetTempPath(), $"srfile_batch_{Guid.NewGuid():N}.zip");
            var fs = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, FileOptions.DeleteOnClose);

            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
            {
                var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in entryPaths)
                {
                    try
                    {
                        var entry = FindFileEntry(rpf, path);
                        if (entry == null) continue;

                        byte[]? data = entry.File.ExtractFile(entry);
                        if (data == null) continue;

                        string entryName = entry.Name;
                        if (!addedNames.Add(entryName))
                        {
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(entry.Name);
                            string ext = Path.GetExtension(entry.Name);
                            entryName = $"{nameWithoutExt}_{addedNames.Count}{ext}";
                            addedNames.Add(entryName);
                        }

                        var zipEntry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        using var s = zipEntry.Open();
                        s.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        // Ignore individual extraction failures
                    }
                }
            }

            fs.Position = 0;
            return fs;
        }

        public byte[] ExtractBatchToZip(string rpfPath, List<string> entryPaths)
        {
            using var stream = ExtractBatchToZipStream(rpfPath, entryPaths);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public List<string> FindGxt2Files(string rpfPath)
        {
            var rpf = GetOrLoadRpf(rpfPath);
            if (rpf == null) throw new FileNotFoundException("RPF not open");

            var idx = GetOrBuildIndex(rpf);
            return idx.Gxt2Entries.Select(e => e.Path ?? e.Name).ToList();
        }

        private static void CollectDirectoryFiles(RpfDirectoryEntry dir, string currentRel, bool recursive, List<(RpfFileEntry File, string RelPath)> collector)
        {
            if (dir.Files != null)
            {
                foreach (var f in dir.Files)
                {
                    string rel = string.IsNullOrEmpty(currentRel) ? f.Name : Path.Combine(currentRel, f.Name);
                    collector.Add((f, rel));
                }
            }
            if (recursive && dir.Directories != null)
            {
                foreach (var sub in dir.Directories)
                {
                    string rel = string.IsNullOrEmpty(currentRel) ? sub.Name : Path.Combine(currentRel, sub.Name);
                    CollectDirectoryFiles(sub, rel, recursive, collector);
                }
            }
        }

        public RpfIndex GetOrBuildIndex(RpfFile rootRpf)
        {
            string key = rootRpf.FilePath ?? rootRpf.Name ?? "root";
            return _rpfIndexes.GetOrAdd(key, _ => RpfIndex.Build(rootRpf));
        }

        private RpfDirectoryEntry? FindDirectory(RpfFile rootRpf, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/" || relativePath == "\\")
            {
                return rootRpf.Root;
            }

            string norm = RpfIndex.Normalize(relativePath);
            var index = GetOrBuildIndex(rootRpf);

            if (index.DirLookup.TryGetValue(norm, out var cachedDir))
            {
                Interlocked.Increment(ref _cacheHits);
                return cachedDir;
            }

            Interlocked.Increment(ref _cacheMisses);

            foreach (var rpf in GetAllRpfs(rootRpf))
            {
                // 1. Path matches root of this RPF
                if (rpf.Root != null && (string.Equals(rpf.Path?.ToLowerInvariant(), norm, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rpf.NameLower, norm, StringComparison.OrdinalIgnoreCase) ||
                    (rpf.Root.Path != null && string.Equals(rpf.Root.Path.ToLowerInvariant(), norm, StringComparison.OrdinalIgnoreCase))))
                {
                    index.DirLookup.TryAdd(norm, rpf.Root);
                    return rpf.Root;
                }

                // 2. Direct match in AllEntries
                if (rpf.AllEntries != null)
                {
                    var match = rpf.AllEntries.OfType<RpfDirectoryEntry>().FirstOrDefault(d =>
                        string.Equals(d.Path?.ToLowerInvariant(), norm, StringComparison.OrdinalIgnoreCase) ||
                        (d.Path != null && d.Path.ToLowerInvariant().EndsWith("\\" + norm)));
                    if (match != null)
                    {
                        index.DirLookup.TryAdd(norm, match);
                        return match;
                    }
                }

                // 3. Hierarchical path walk
                var current = rpf.Root;
                if (current != null)
                {
                    string rel = norm;
                    if (rpf.Path != null && rel.StartsWith(rpf.Path.ToLowerInvariant() + "\\"))
                    {
                        rel = rel.Substring(rpf.Path.Length + 1);
                    }
                    else if (rel.StartsWith(rpf.NameLower + "\\"))
                    {
                        rel = rel.Substring(rpf.NameLower.Length + 1);
                    }

                    string[] parts = rel.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    bool walked = true;
                    var walkDir = current;
                    foreach (var part in parts)
                    {
                        var next = walkDir.Directories?.FirstOrDefault(d => string.Equals(d.Name, part, StringComparison.OrdinalIgnoreCase));
                        if (next == null)
                        {
                            walked = false;
                            break;
                        }
                        walkDir = next;
                    }
                    if (walked && walkDir != null)
                    {
                        index.DirLookup.TryAdd(norm, walkDir);
                        return walkDir;
                    }
                }
            }

            return null;
        }

        private RpfFileEntry? FindFileEntry(RpfFile rootRpf, string entryPath)
        {
            if (string.IsNullOrWhiteSpace(entryPath)) return null;

            string norm = RpfIndex.Normalize(entryPath);
            var index = GetOrBuildIndex(rootRpf);

            if (index.FileLookup.TryGetValue(norm, out var cached))
            {
                Interlocked.Increment(ref _cacheHits);
                return cached;
            }

            Interlocked.Increment(ref _cacheMisses);

            bool hasDirectory = norm.Contains('\\') || norm.Contains('/');
            string fileName = Path.GetFileName(norm);

            foreach (var rpf in GetAllRpfs(rootRpf))
            {
                if (rpf.AllEntries == null) continue;

                // 1. Exact path match or ends with directory+file
                var match = rpf.AllEntries.OfType<RpfFileEntry>().FirstOrDefault(f =>
                    string.Equals(f.Path?.ToLowerInvariant(), norm, StringComparison.OrdinalIgnoreCase) ||
                    (f.Path != null && f.Path.ToLowerInvariant().EndsWith("\\" + norm)) ||
                    (!hasDirectory && string.Equals(f.NameLower, fileName, StringComparison.OrdinalIgnoreCase)) ||
                    (!hasDirectory && string.Equals(f.NameLower, norm, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    index.FileLookup.TryAdd(norm, match);
                    return match;
                }
            }

            return null;
        }

        private static RpfInfo ToRpfInfo(RpfFile rpf)
        {
            return new RpfInfo(
                Name: rpf.Name,
                FilePath: rpf.FilePath,
                FileSize: rpf.FileSize,
                Version: rpf.Version,
                EntryCount: rpf.EntryCount,
                TotalFileCount: rpf.TotalFileCount,
                TotalFolderCount: rpf.TotalFolderCount,
                TotalResourceCount: rpf.TotalResourceCount,
                Encryption: rpf.Encryption.ToString()
            );
        }
    }

    public class RpfIndex
    {
        public ConcurrentDictionary<string, RpfFileEntry> FileLookup { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<string, RpfDirectoryEntry> DirLookup { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<RpfFileEntry> Gxt2Entries { get; } = new();

        public static RpfIndex Build(RpfFile rootRpf)
        {
            var index = new RpfIndex();
            string rootPrefix = Normalize(rootRpf.Name ?? rootRpf.Path ?? "");

            foreach (var rpf in RpfService.GetAllRpfs(rootRpf))
            {
                string rpfPrefix = Normalize(rpf.Name ?? rpf.Path ?? "");

                if (rpf.Root != null)
                {
                    index.DirLookup[""] = rpf.Root;
                    index.DirLookup["/"] = rpf.Root;
                    index.DirLookup["\\"] = rpf.Root;
                    if (!string.IsNullOrEmpty(rpf.Root.Path))
                    {
                        string normPath = Normalize(rpf.Root.Path);
                        index.DirLookup[normPath] = rpf.Root;
                        index.DirLookup.TryAdd(Normalize(rpf.Root.Name), rpf.Root);
                    }
                }

                if (rpf.AllEntries != null)
                {
                    foreach (var entry in rpf.AllEntries)
                    {
                        if (entry is RpfDirectoryEntry dir)
                        {
                            if (!string.IsNullOrEmpty(dir.Path))
                            {
                                string normPath = Normalize(dir.Path);
                                index.DirLookup[normPath] = dir;

                                // Also index path relative to root RPF
                                if (!string.IsNullOrEmpty(rootPrefix) && normPath.StartsWith(rootPrefix + "\\"))
                                {
                                    string rel = normPath.Substring(rootPrefix.Length + 1);
                                    index.DirLookup[rel] = dir;
                                }

                                // If inside child RPF, index relative to that RPF
                                if (rpf != rootRpf && !string.IsNullOrEmpty(rpfPrefix) && normPath.StartsWith(rpfPrefix + "\\"))
                                {
                                    string rel = normPath.Substring(rpfPrefix.Length + 1);
                                    index.DirLookup[rel] = dir;
                                }
                            }

                            if (!string.IsNullOrEmpty(dir.Name))
                            {
                                index.DirLookup.TryAdd(Normalize(dir.Name), dir);
                            }
                        }
                        else if (entry is RpfFileEntry file)
                        {
                            if (!string.IsNullOrEmpty(file.Path))
                            {
                                string normPath = Normalize(file.Path);
                                index.FileLookup[normPath] = file;

                                // Also index path relative to root RPF
                                if (!string.IsNullOrEmpty(rootPrefix) && normPath.StartsWith(rootPrefix + "\\"))
                                {
                                    string rel = normPath.Substring(rootPrefix.Length + 1);
                                    index.FileLookup[rel] = file;
                                }

                                // If inside child RPF, index relative to that child RPF
                                if (rpf != rootRpf && !string.IsNullOrEmpty(rpfPrefix) && normPath.StartsWith(rpfPrefix + "\\"))
                                {
                                    string rel = normPath.Substring(rpfPrefix.Length + 1);
                                    index.FileLookup[rel] = file;
                                }
                            }

                            if (!string.IsNullOrEmpty(file.Name))
                            {
                                index.FileLookup.TryAdd(Normalize(file.Name), file);
                            }

                            if (file.NameLower != null && file.NameLower.EndsWith(".gxt2"))
                            {
                                index.Gxt2Entries.Add(file);
                            }
                        }
                    }
                }
            }
            return index;
        }

        public static string Normalize(string path)
        {
            return path.Replace('/', '\\').Trim('\\').ToLowerInvariant();
        }
    }
}

