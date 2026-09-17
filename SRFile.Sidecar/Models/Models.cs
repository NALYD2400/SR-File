using System;
using System.Collections.Generic;

namespace SRFile.Sidecar.Models
{
    public record AppStatus(
        bool Running,
        string Version,
        string? GtaFolder,
        bool IsGen9,
        bool KeysLoaded,
        int LoadedRpfsCount,
        long UptimeSeconds
    );

    public record ConfigGtaRequest(
        string Path,
        bool? IsGen9,
        string? AesKey
    );

    public record RpfOpenRequest(
        string FilePath
    );

    public record RpfInfo(
        string Name,
        string FilePath,
        long FileSize,
        uint Version,
        uint EntryCount,
        uint TotalFileCount,
        uint TotalFolderCount,
        uint TotalResourceCount,
        string Encryption
    );

    public record RpfEntryDto(
        string Name,
        string Path,
        bool IsDirectory,
        bool IsResource,
        long Size,
        long CompressedSize,
        string ResourceType,
        string Encryption
    );

    public record TextureItemDto(
        string Name,
        int Width,
        int Height,
        string Format,
        int MipCount
    );

    public record AudioStreamDto(
        int Index,
        string Name,
        float Length,
        int SampleRate,
        int Channels,
        string Type,
        long ByteLength
    );

    // ==========================================
    // 1. Mod Manager Models
    // ==========================================
    public record ModPackageInfo(
        string Id,
        string Name,
        string Type, // "OIV", "DLC", "ASI", "Loose"
        bool IsEnabled,
        string Version,
        string Author,
        string Description,
        string SourcePath,
        string InstallPath,
        long Size,
        int FileCount,
        List<string> Files,
        List<string> Conflicts
    );

    public record OivManifestInfo(
        string Name,
        string Author,
        string Version,
        string Description,
        List<string> TargetComponents,
        int ActionCount,
        string RawXml
    );

    public record ModConflictDto(
        string FilePath,
        List<string> ConflictingMods
    );

    public record InstallModRequest(
        string FilePath,
        bool? EnableAfterInstall = true,
        string? GtaFolder = null
    );

    public record ToggleModRequest(
        string ModId,
        bool Enabled,
        string? GtaFolder = null
    );

    public record ReadFileTextResponse(
        string FilePath,
        string Content,
        long Size
    );

    public record SaveFileTextRequest(
        string FilePath,
        string Content
    );

    public record ReadFileBytesResponse(
        string FilePath,
        long TotalSize,
        long Offset,
        int Length,
        string Base64Data
    );

    public record ModInstallQueueItem(
        string Id,
        string PackagePath,
        string PackageName,
        string Status, // "Queued", "Installing", "Completed", "Failed"
        float Progress,
        string Message
    );

    // ==========================================
    // 2. Gen9 Converter Models
    // ==========================================
    public record Gen9ConversionRequest(
        string InputPath,
        string OutputPath,
        string Preset, // "gen9_to_pc", "pc_to_gen9", "textures_only"
        bool ProcessSubfolders,
        bool OverwriteExisting,
        bool CopyUnconverted
    );

    public record Gen9ConversionStatus(
        bool IsConverting,
        float Progress,
        string CurrentFile,
        int TotalFiles,
        int ProcessedFiles,
        List<string> Logs,
        string? Error
    );

    // ==========================================
    // 3. Crypto & Jenkins Models
    // ==========================================
    public record JoaatRequest(
        string Text,
        string? Encoding // "utf8", "ascii"
    );

    public record JoaatResponse(
        string Text,
        uint HashUint,
        int HashInt,
        string HashHex
    );

    public record BatchJoaatRequest(
        List<string> Texts,
        string? Encoding
    );

    public record DictionarySearchResponse(
        uint HashUint,
        string HashHex,
        string Text,
        double Score
    );

    public record CryptoKeysStatus(
        bool AesKeyLoaded,
        string? AesKeyHex,
        string? AesKeyBase64,
        int NgKeysCount,
        int DecryptTablesCount,
        bool AwcKeyLoaded
    );

    public record SetAesKeyRequest(
        string Key
    );

    public record InspectRpfEncryptionRequest(
        string FilePath
    );

    public record RpfEncryptionInfo(
        string FilePath,
        string Encryption,
        bool IsValidHeader,
        uint Version
    );

    // ==========================================
    // 4. Map & Project Editor Models
    // ==========================================
    public record Vector3Dto(float X, float Y, float Z);
    public record Vector4Dto(float X, float Y, float Z, float W);

    public record ProjectSummaryDto(
        string Name,
        int Version,
        string? Filepath,
        List<string> YmapFiles,
        List<string> YtypFiles,
        List<string> YbnFiles
    );

    public record OpenProjectRequest(
        string FilePath
    );

    public record SaveProjectRequest(
        string FilePath,
        string Name,
        int Version,
        List<string> YmapFiles,
        List<string> YtypFiles,
        List<string> YbnFiles
    );

    public record YmapEntityDto(
        string Name,
        string ArchetypeName,
        Vector3Dto Position,
        Vector4Dto Rotation,
        Vector3Dto EulerRotation,
        float LodDist,
        float ChildLodDist,
        uint Flags,
        uint Guid
    );

    public record ArchetypeDto(
        string Name,
        string TextureDictionary,
        string PhysicsDictionary,
        float LodDist,
        float HdTextureDist,
        Vector3Dto BbMin,
        Vector3Dto BbMax,
        Vector3Dto BsCentre,
        float BsRadius
    );

    public record ExportYmapXmlRequest(
        string YmapName,
        List<YmapEntityDto> Entities
    );

    public record ImportYmapXmlRequest(
        string XmlContent
    );

    // ==========================================
    // 5. Cache & Batch Extraction Models
    // ==========================================
    public record RpfCacheItemDto(
        string Name,
        string FilePath,
        long FileSize,
        uint EntryCount
    );

    public record RpfCacheStats(
        int LoadedRpfsCount,
        long TotalIndexedEntries,
        long CacheHits,
        long CacheMisses,
        double HitRatePercent,
        List<RpfCacheItemDto> OpenArchives
    );

    public record ExtractFolderRequest(
        string RpfPath,
        string FolderPath,
        string? OutputDirectory,
        bool? AsZip,
        bool? Recursive
    );

    public record ExtractBatchRequest(
        string RpfPath,
        List<string> EntryPaths,
        string? OutputDirectory,
        bool? AsZip
    );

    public record BatchExtractResultDto(
        bool Success,
        int ExtractedCount,
        int ErrorCount,
        long TotalBytes,
        long DurationMs,
        string? OutputDirectory,
        List<string> Errors
    );

    public record CloseRpfRequest(
        string FilePath
    );

    // ==========================================
    // 6. Diagnostics & System Metrics Models
    // ==========================================
    public record SystemMetricsDto(
        // Memory
        long ProcessWorkingSetBytes,
        double ProcessWorkingSetMB,
        long ProcessPrivateMemoryBytes,
        double ProcessPrivateMemoryMB,
        long ProcessVirtualMemoryBytes,
        long GcTotalMemoryBytes,
        double GcTotalMemoryMB,
        int GcGen0Collections,
        int GcGen1Collections,
        int GcGen2Collections,
        long HeapSizeBytes,
        // System & Process
        int ProcessId,
        int ThreadCount,
        int HandleCount,
        long UptimeSeconds,
        DateTime StartTime,
        int ProcessorCount,
        string OsPlatform,
        string OsArchitecture,
        string ProcessArchitecture,
        string FrameworkDescription,
        // Archive Cache
        int OpenArchivesCount,
        List<RpfCacheItemDto> OpenArchives,
        long TotalIndexedEntries,
        long CacheHits,
        long CacheMisses
    );

    // ==========================================
    // 7. GXT2 & Text Search Models
    // ==========================================
    public record Gxt2EntryDto(
        uint Hash,
        string HexHash,
        string Text,
        string? ResolvedKey
    );

    public record Gxt2TableDto(
        string FileName,
        uint EntryCount,
        List<Gxt2EntryDto> Entries
    );

    public record Gxt2SearchResultDto(
        string RpfPath,
        string EntryPath,
        uint Hash,
        string HexHash,
        string Text,
        string? ResolvedKey
    );

    public record Gxt2ExportRequest(
        string? FileName,
        List<Gxt2EntryDto> Entries
    );

    public record Gxt2BuildRequest(
        string TextContent,
        string? EntryName
    );
}

