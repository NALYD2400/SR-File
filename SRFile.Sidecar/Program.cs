using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SRFile.Sidecar.Models;
using SRFile.Sidecar.Services;

var builder = WebApplication.CreateBuilder(args);

// Port argument handling (e.g. --port 5890)
int port = 5890;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out int p))
    {
        port = p;
    }
}

builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

// Add services
builder.Services.AddSingleton<RpfService>();
builder.Services.AddSingleton<ModManagerService>();
builder.Services.AddSingleton<Gen9ConverterService>();
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddSingleton<ProjectEditorService>();
builder.Services.AddSingleton<SystemService>();
builder.Services.AddSingleton<TextService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// ==========================================
// Base & RPF Endpoints
// ==========================================
app.MapGet("/api/status", (RpfService service) =>
{
    return Results.Ok(service.GetStatus());
});

app.MapPost("/api/config/gta-folder", (ConfigGtaRequest req, RpfService service) =>
{
    bool success = service.ConfigureGtaFolder(req.Path, req.IsGen9, req.AesKey);
    return Results.Ok(new { success, status = service.GetStatus() });
});

app.MapPost("/api/rpf/open", (RpfOpenRequest req, RpfService service) =>
{
    try
    {
        var info = service.OpenRpf(req.FilePath);
        return Results.Ok(info);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/info", (string rpfPath, RpfService service) =>
{
    try
    {
        var info = service.GetRpfInfo(rpfPath);
        return info != null ? Results.Ok(info) : Results.NotFound(new { error = "RPF not loaded" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/entries", (string rpfPath, string? dirPath, RpfService service) =>
{
    try
    {
        var entries = service.GetEntries(rpfPath, dirPath ?? "");
        return Results.Ok(entries);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/search", (string rpfPath, string q, int? limit, RpfService service) =>
{
    try
    {
        var results = service.SearchEntries(rpfPath, q, limit ?? 250);
        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/file", (string rpfPath, string entryPath, RpfService service) =>
{
    try
    {
        byte[] bytes = service.ExtractFile(rpfPath, entryPath);
        string filename = Path.GetFileName(entryPath);
        return Results.File(bytes, "application/octet-stream", fileDownloadName: filename);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/file/text", (string rpfPath, string entryPath, RpfService service) =>
{
    try
    {
        string text = service.ExtractFileText(rpfPath, entryPath);
        return Results.Ok(new { content = text });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/textures", (string rpfPath, string entryPath, RpfService service) =>
{
    try
    {
        var textures = service.GetTextures(rpfPath, entryPath);
        return Results.Ok(textures);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/texture/png", (string rpfPath, string entryPath, string name, int? mip, RpfService service) =>
{
    try
    {
        byte[] pngBytes = service.GetTexturePng(rpfPath, entryPath, name, mip ?? 0);
        return Results.File(pngBytes, "image/png");
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/texture/dds", (string rpfPath, string entryPath, string name, RpfService service) =>
{
    try
    {
        byte[] ddsBytes = service.GetTextureDds(rpfPath, entryPath, name);
        return Results.File(ddsBytes, "image/vnd-ms.dds", fileDownloadName: $"{name}.dds");
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/audio/streams", (string rpfPath, string entryPath, RpfService service) =>
{
    try
    {
        var streams = service.GetAudioStreams(rpfPath, entryPath);
        return Results.Ok(streams);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/audio/wav", (string rpfPath, string entryPath, int? stream, RpfService service) =>
{
    try
    {
        var wavStream = service.GetAudioWav(rpfPath, entryPath, stream ?? 0);
        return Results.Stream(wavStream, "audio/wav", enableRangeProcessing: true);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/rpf/cache/stats", (RpfService service) =>
{
    return Results.Ok(service.GetCacheStats());
});

app.MapPost("/api/rpf/cache/clear", (RpfService service) =>
{
    int cleared = service.ClearCache();
    return Results.Ok(new { success = true, evictedCount = cleared });
});

app.MapPost("/api/rpf/close", (CloseRpfRequest req, RpfService service) =>
{
    bool closed = service.CloseRpf(req.FilePath);
    return Results.Ok(new { success = closed, filePath = req.FilePath });
});

app.MapPost("/api/rpf/extract-folder", (ExtractFolderRequest req, RpfService service) =>
{
    try
    {
        if (req.AsZip == true)
        {
            byte[] zipData = service.ExtractFolderToZip(req.RpfPath, req.FolderPath, req.Recursive ?? true);
            string folderName = Path.GetFileName(req.FolderPath.TrimEnd('/', '\\'));
            if (string.IsNullOrEmpty(folderName)) folderName = "archive_folder";
            return Results.File(zipData, "application/zip", fileDownloadName: $"{folderName}.zip");
        }
        else
        {
            string outDir = string.IsNullOrWhiteSpace(req.OutputDirectory)
                ? Path.Combine(Environment.CurrentDirectory, "Export", Path.GetFileName(req.FolderPath.TrimEnd('/', '\\')))
                : req.OutputDirectory;
            var result = service.ExtractFolderToDisk(req.RpfPath, req.FolderPath, outDir, req.Recursive ?? true);
            return Results.Ok(result);
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/rpf/extract-batch", (ExtractBatchRequest req, RpfService service) =>
{
    try
    {
        if (req.AsZip == true)
        {
            byte[] zipData = service.ExtractBatchToZip(req.RpfPath, req.EntryPaths);
            return Results.File(zipData, "application/zip", fileDownloadName: "batch_export.zip");
        }
        else
        {
            string outDir = string.IsNullOrWhiteSpace(req.OutputDirectory)
                ? Path.Combine(Environment.CurrentDirectory, "Export", "Batch")
                : req.OutputDirectory;
            var result = service.ExtractBatchToDisk(req.RpfPath, req.EntryPaths, outDir);
            return Results.Ok(result);
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==========================================
// 1. Mod Manager Endpoints
// ==========================================
app.MapGet("/api/mods", (string? gtaFolder, ModManagerService service) =>
{
    try
    {
        var mods = service.GetMods(gtaFolder);
        return Results.Ok(mods);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/mods/toggle", (ToggleModRequest req, ModManagerService service) =>
{
    try
    {
        bool success = service.ToggleMod(req.ModId, req.Enabled, req.GtaFolder);
        return Results.Ok(new { success });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/mods/inspect-oiv", (InspectRpfEncryptionRequest req, ModManagerService service) =>
{
    try
    {
        var manifest = service.InspectOiv(req.FilePath);
        return Results.Ok(manifest);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/mods/install", (InstallModRequest req, ModManagerService service) =>
{
    try
    {
        var item = service.QueueInstall(req.FilePath, req.EnableAfterInstall ?? true, req.GtaFolder);
        return Results.Ok(item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/mods/queue", (ModManagerService service) =>
{
    return Results.Ok(service.GetQueueStatus());
});

app.MapDelete("/api/mods/{id}", (string id, ModManagerService service) =>
{
    try
    {
        bool success = service.UninstallMod(id);
        return Results.Ok(new { success });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==========================================
// 2. Gen9 Asset Converter Endpoints
// ==========================================
app.MapPost("/api/gen9/convert", (Gen9ConversionRequest req, Gen9ConverterService service) =>
{
    try
    {
        var status = service.StartConversion(req);
        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/gen9/status", (Gen9ConverterService service) =>
{
    return Results.Ok(service.GetStatus());
});

app.MapPost("/api/gen9/stop", (Gen9ConverterService service) =>
{
    service.Stop();
    return Results.Ok(new { message = "Arrêt de la conversion demandé" });
});

app.MapPost("/api/gen9/clear-logs", (Gen9ConverterService service) =>
{
    service.ClearLogs();
    return Results.Ok(new { message = "Journaux réinitialisés" });
});

// ==========================================
// 3. Advanced Crypto & Jenkins Endpoints
// ==========================================
app.MapPost("/api/crypto/joaat", (JoaatRequest req, CryptoService service) =>
{
    try
    {
        var result = service.ComputeJoaat(req.Text, req.Encoding);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/crypto/joaat/batch", (BatchJoaatRequest req, CryptoService service) =>
{
    try
    {
        var results = service.BatchCompute(req.Texts, req.Encoding);
        return Results.Ok(results);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/crypto/joaat/lookup", (uint hash, CryptoService service) =>
{
    string? text = service.LookupHash(hash);
    return text != null ? Results.Ok(new { hash, text }) : Results.NotFound(new { error = "Hash non résolu dans le dictionnaire" });
});

app.MapGet("/api/crypto/dictionary/search", (string? q, int? limit, CryptoService service) =>
{
    var results = service.SearchDictionary(q ?? "", limit ?? 50);
    return Results.Ok(results);
});

app.MapGet("/api/crypto/keys", (CryptoService service) =>
{
    return Results.Ok(service.GetKeysStatus());
});

app.MapPost("/api/crypto/set-aes", (SetAesKeyRequest req, CryptoService service) =>
{
    try
    {
        bool success = service.SetAesKey(req.Key);
        return Results.Ok(new { success, status = service.GetKeysStatus() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/crypto/inspect-rpf", (InspectRpfEncryptionRequest req, CryptoService service) =>
{
    try
    {
        var info = service.InspectRpfEncryption(req.FilePath);
        return Results.Ok(info);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==========================================
// 4. Map & Project Editor Endpoints
// ==========================================
app.MapGet("/api/project/current", (ProjectEditorService service) =>
{
    return Results.Ok(service.GetCurrentProject());
});

app.MapPost("/api/project/new", (OpenProjectRequest req, ProjectEditorService service) =>
{
    var proj = service.CreateProject(req.FilePath);
    return Results.Ok(proj);
});

app.MapPost("/api/project/open", (OpenProjectRequest req, ProjectEditorService service) =>
{
    try
    {
        var proj = service.OpenProject(req.FilePath);
        return Results.Ok(proj);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/project/save", (SaveProjectRequest req, ProjectEditorService service) =>
{
    try
    {
        bool success = service.SaveProject(req);
        return Results.Ok(new { success });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/project/entities", (ProjectEditorService service) =>
{
    return Results.Ok(service.GetEntities());
});

app.MapGet("/api/project/archetypes", (ProjectEditorService service) =>
{
    return Results.Ok(service.GetArchetypes());
});

app.MapPost("/api/project/entity", (int? index, YmapEntityDto entity, ProjectEditorService service) =>
{
    service.UpdateEntity(index ?? -1, entity);
    return Results.Ok(new { success = true, entities = service.GetEntities() });
});

app.MapDelete("/api/project/entity/{index}", (int index, ProjectEditorService service) =>
{
    service.DeleteEntity(index);
    return Results.Ok(new { success = true, entities = service.GetEntities() });
});

app.MapPost("/api/project/ymap/export-xml", (ExportYmapXmlRequest req, ProjectEditorService service) =>
{
    try
    {
        string xml = service.ExportYmapXml(req.YmapName, req.Entities);
        return Results.Ok(new { xml });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/project/ymap/import-xml", (ImportYmapXmlRequest req, ProjectEditorService service) =>
{
    try
    {
        var entities = service.ImportYmapXml(req.XmlContent);
        return Results.Ok(entities);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==========================================
// 5. Loose File Endpoints (Code Editor & Hex Viewer)
// ==========================================
app.MapGet("/api/file/read-text", (string filePath) =>
{
    try
    {
        if (!File.Exists(filePath))
            return Results.NotFound(new { error = $"Fichier introuvable: {filePath}" });

        var info = new FileInfo(filePath);
        string text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        return Results.Ok(new ReadFileTextResponse(filePath, text, info.Length));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/file/save-text", (SaveFileTextRequest req) =>
{
    try
    {
        string? dir = Path.GetDirectoryName(req.FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(req.FilePath, req.Content, System.Text.Encoding.UTF8);
        return Results.Ok(new { success = true, filePath = req.FilePath, size = req.Content.Length });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/file/read-bytes", (string filePath, long? offset, int? length) =>
{
    try
    {
        if (!File.Exists(filePath))
            return Results.NotFound(new { error = $"Fichier introuvable: {filePath}" });

        var fileInfo = new FileInfo(filePath);
        long fileLen = fileInfo.Length;
        long startOffset = Math.Max(0, Math.Min(offset ?? 0, fileLen));
        int readLen = Math.Min(length ?? 65536, (int)Math.Min(int.MaxValue, fileLen - startOffset));

        byte[] buffer = new byte[readLen];
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            fs.Seek(startOffset, SeekOrigin.Begin);
            int bytesRead = fs.Read(buffer, 0, readLen);
            if (bytesRead < readLen)
            {
                Array.Resize(ref buffer, bytesRead);
            }
        }

        string base64 = Convert.ToBase64String(buffer);
        return Results.Ok(new ReadFileBytesResponse(
            FilePath: filePath,
            TotalSize: fileLen,
            Offset: startOffset,
            Length: buffer.Length,
            Base64Data: base64
        ));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// ==========================================
// 6. Diagnostics & System Metrics
// ==========================================
app.MapGet("/api/system/metrics", (SystemService systemService) =>
{
    return Results.Ok(systemService.GetMetrics());
});

// ==========================================
// 7. GXT2 & GTA V Text Table Endpoints
// ==========================================
app.MapGet("/api/text/gxt2", (string rpfPath, string entryPath, RpfService rpfService, TextService textService) =>
{
    try
    {
        var table = textService.GetGxt2FromRpf(rpfService, rpfPath, entryPath);
        return Results.Ok(table);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/text/gxt2/search", (string rpfPath, string entryPath, string q, int? limit, RpfService rpfService, TextService textService) =>
{
    try
    {
        var table = textService.GetGxt2FromRpf(rpfService, rpfPath, entryPath);
        var matches = textService.SearchGxt2(table, q, limit ?? 100);
        return Results.Ok(matches);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/text/search-rpf", (string rpfPath, string q, int? limit, RpfService rpfService, TextService textService) =>
{
    try
    {
        var matches = textService.SearchRpfGxt2(rpfService, rpfPath, q, limit ?? 250);
        return Results.Ok(matches);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/text/gxt2/export-text", (Gxt2ExportRequest req, TextService textService) =>
{
    try
    {
        string text = textService.ExportToText(req.Entries);
        return Results.Ok(new { text });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/text/gxt2/build", (Gxt2BuildRequest req, TextService textService) =>
{
    try
    {
        byte[] gxt2Bytes = textService.BuildGxt2(req.TextContent, req.EntryName ?? "text.gxt2");
        string dlName = req.EntryName ?? "text.gxt2";
        return Results.File(gxt2Bytes, "application/octet-stream", fileDownloadName: dlName);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Shutdown
app.MapPost("/api/shutdown", (IHostApplicationLifetime lifetime) =>
{
    lifetime.StopApplication();
    return Results.Ok(new { message = "Shutting down sidecar" });
});

Console.WriteLine($"[SRFile.Sidecar] Listening on http://127.0.0.1:{port}");
app.Run();
