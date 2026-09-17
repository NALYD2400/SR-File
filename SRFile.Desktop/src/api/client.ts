import {
  AppStatus,
  RpfInfo,
  RpfEntry,
  TextureItem,
  AudioStream,
  ModPackageInfo,
  OivManifestInfo,
  ModInstallQueueItem,
  Gen9ConversionRequest,
  Gen9ConversionStatus,
  JoaatResult,
  DictionaryItem,
  CryptoKeysStatus,
  RpfEncryptionInfo,
  ProjectSummary,
  YmapEntity,
  Archetype,
  ReadFileTextResult,
  ReadFileBytesResult,
  RpfCacheStats,
  BatchExtractResult,
  SystemMetrics,
  Gxt2Entry,
  Gxt2Table,
  Gxt2SearchResult,
  ParseGxt2Request,
  WorldStatus,
} from "../types";

let cachedBaseUrl = "http://127.0.0.1:5890";

export async function getBaseUrl(): Promise<string> {
  try {
    const { invoke } = await import("@tauri-apps/api/core");
    const url = await invoke<string>("get_sidecar_url");
    if (url) {
      cachedBaseUrl = url;
    }
  } catch {
    // In standard browser environment or before Tauri is ready
  }
  return cachedBaseUrl;
}

export async function fetchSidecar<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const base = await getBaseUrl();
  const url = `${base}${endpoint}`;
  const res = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
  });

  if (!res.ok) {
    let msg = `HTTP error ${res.status}`;
    try {
      const err = await res.json();
      if (err.error) msg = err.error;
    } catch {
      // fallback
    }
    throw new Error(msg);
  }

  return res.json();
}

export const api = {
  async getStatus(): Promise<AppStatus> {
    return fetchSidecar<AppStatus>("/api/status");
  },

  async configureGtaFolder(path: string, isGen9 = false, aesKey?: string): Promise<{ success: boolean; status: AppStatus }> {
    return fetchSidecar("/api/config/gta-folder", {
      method: "POST",
      body: JSON.stringify({ path, isGen9, aesKey }),
    });
  },

  async openRpf(filePath: string): Promise<RpfInfo> {
    return fetchSidecar<RpfInfo>("/api/rpf/open", {
      method: "POST",
      body: JSON.stringify({ filePath }),
    });
  },

  async getRpfInfo(rpfPath: string): Promise<RpfInfo> {
    const params = new URLSearchParams({ rpfPath });
    return fetchSidecar<RpfInfo>(`/api/rpf/info?${params.toString()}`);
  },

  async getEntries(rpfPath: string, dirPath = ""): Promise<RpfEntry[]> {
    const params = new URLSearchParams({ rpfPath, dirPath });
    return fetchSidecar<RpfEntry[]>(`/api/rpf/entries?${params.toString()}`);
  },

  async searchEntries(rpfPath: string, query: string, limit = 250): Promise<RpfEntry[]> {
    const params = new URLSearchParams({ rpfPath, q: query, limit: limit.toString() });
    return fetchSidecar<RpfEntry[]>(`/api/rpf/search?${params.toString()}`);
  },

  async getFileText(rpfPath: string, entryPath: string): Promise<{ content: string }> {
    const params = new URLSearchParams({ rpfPath, entryPath });
    return fetchSidecar<{ content: string }>(`/api/rpf/file/text?${params.toString()}`);
  },

  async getTextures(rpfPath: string, entryPath: string): Promise<TextureItem[]> {
    const params = new URLSearchParams({ rpfPath, entryPath });
    return fetchSidecar<TextureItem[]>(`/api/rpf/textures?${params.toString()}`);
  },

  async getAudioStreams(rpfPath: string, entryPath: string): Promise<AudioStream[]> {
    const params = new URLSearchParams({ rpfPath, entryPath });
    return fetchSidecar<AudioStream[]>(`/api/rpf/audio/streams?${params.toString()}`);
  },

  getTexturePngUrl(rpfPath: string, entryPath: string, name: string, mip = 0): string {
    const params = new URLSearchParams({ rpfPath, entryPath, name, mip: mip.toString() });
    return `${cachedBaseUrl}/api/rpf/texture/png?${params.toString()}`;
  },

  getTextureDdsUrl(rpfPath: string, entryPath: string, name: string): string {
    const params = new URLSearchParams({ rpfPath, entryPath, name });
    return `${cachedBaseUrl}/api/rpf/texture/dds?${params.toString()}`;
  },

  getAudioWavUrl(rpfPath: string, entryPath: string, streamIndex = 0): string {
    const params = new URLSearchParams({ rpfPath, entryPath, stream: streamIndex.toString() });
    return `${cachedBaseUrl}/api/rpf/audio/wav?${params.toString()}`;
  },

  getRawFileUrl(rpfPath: string, entryPath: string): string {
    const params = new URLSearchParams({ rpfPath, entryPath });
    return `${cachedBaseUrl}/api/rpf/file?${params.toString()}`;
  },

  // 1. Mod Manager API
  async getMods(gtaFolder?: string): Promise<ModPackageInfo[]> {
    const params = gtaFolder ? `?gtaFolder=${encodeURIComponent(gtaFolder)}` : "";
    return fetchSidecar<ModPackageInfo[]>(`/api/mods${params}`);
  },

  async toggleMod(modId: string, enabled: boolean, gtaFolder?: string): Promise<{ success: boolean }> {
    return fetchSidecar("/api/mods/toggle", {
      method: "POST",
      body: JSON.stringify({ modId, enabled, gtaFolder }),
    });
  },

  async inspectOiv(filePath: string): Promise<OivManifestInfo> {
    return fetchSidecar<OivManifestInfo>("/api/mods/inspect-oiv", {
      method: "POST",
      body: JSON.stringify({ filePath }),
    });
  },

  async installMod(filePath: string, enableAfterInstall = true, gtaFolder?: string): Promise<ModInstallQueueItem> {
    return fetchSidecar<ModInstallQueueItem>("/api/mods/install", {
      method: "POST",
      body: JSON.stringify({ filePath, enableAfterInstall, gtaFolder }),
    });
  },

  async getModQueue(): Promise<ModInstallQueueItem[]> {
    return fetchSidecar<ModInstallQueueItem[]>("/api/mods/queue");
  },

  async uninstallMod(modId: string): Promise<{ success: boolean }> {
    return fetchSidecar(`/api/mods/${encodeURIComponent(modId)}`, {
      method: "DELETE",
    });
  },

  // 2. Gen9 Converter API
  async startGen9Conversion(req: Gen9ConversionRequest): Promise<Gen9ConversionStatus> {
    return fetchSidecar<Gen9ConversionStatus>("/api/gen9/convert", {
      method: "POST",
      body: JSON.stringify(req),
    });
  },

  async getGen9Status(): Promise<Gen9ConversionStatus> {
    return fetchSidecar<Gen9ConversionStatus>("/api/gen9/status");
  },

  async stopGen9Conversion(): Promise<{ message: string }> {
    return fetchSidecar("/api/gen9/stop", {
      method: "POST",
    });
  },

  async clearGen9Logs(): Promise<{ message: string }> {
    return fetchSidecar("/api/gen9/clear-logs", {
      method: "POST",
    });
  },

  // 3. Crypto & Jenkins API
  async computeJoaat(text: string, encoding = "utf8"): Promise<JoaatResult> {
    return fetchSidecar<JoaatResult>("/api/crypto/joaat", {
      method: "POST",
      body: JSON.stringify({ text, encoding }),
    });
  },

  async batchJoaat(texts: string[], encoding = "utf8"): Promise<JoaatResult[]> {
    return fetchSidecar<JoaatResult[]>("/api/crypto/joaat/batch", {
      method: "POST",
      body: JSON.stringify({ texts, encoding }),
    });
  },

  async lookupJoaat(hash: number): Promise<{ hash: number; text: string }> {
    return fetchSidecar<{ hash: number; text: string }>(`/api/crypto/joaat/lookup?hash=${hash}`);
  },

  async searchDictionary(query: string, limit = 50): Promise<DictionaryItem[]> {
    const params = new URLSearchParams({ q: query, limit: limit.toString() });
    return fetchSidecar<DictionaryItem[]>(`/api/crypto/dictionary/search?${params.toString()}`);
  },

  async getCryptoKeys(): Promise<CryptoKeysStatus> {
    return fetchSidecar<CryptoKeysStatus>("/api/crypto/keys");
  },

  async setAesKey(key: string): Promise<{ success: boolean; status: CryptoKeysStatus }> {
    return fetchSidecar("/api/crypto/set-aes", {
      method: "POST",
      body: JSON.stringify({ key }),
    });
  },

  async inspectRpfEncryption(filePath: string): Promise<RpfEncryptionInfo> {
    return fetchSidecar<RpfEncryptionInfo>("/api/crypto/inspect-rpf", {
      method: "POST",
      body: JSON.stringify({ filePath }),
    });
  },

  // 4. Map & Project Editor API
  async getCurrentProject(): Promise<ProjectSummary> {
    return fetchSidecar<ProjectSummary>("/api/project/current");
  },

  async createProject(name: string): Promise<ProjectSummary> {
    return fetchSidecar<ProjectSummary>("/api/project/new", {
      method: "POST",
      body: JSON.stringify({ filePath: name }),
    });
  },

  async openProject(filePath: string): Promise<ProjectSummary> {
    return fetchSidecar<ProjectSummary>("/api/project/open", {
      method: "POST",
      body: JSON.stringify({ filePath }),
    });
  },

  async saveProject(req: {
    filePath: string;
    name: string;
    version: number;
    ymapFiles: string[];
    ytypFiles: string[];
    ybnFiles: string[];
  }): Promise<{ success: boolean }> {
    return fetchSidecar("/api/project/save", {
      method: "POST",
      body: JSON.stringify(req),
    });
  },

  async getEntities(): Promise<YmapEntity[]> {
    return fetchSidecar<YmapEntity[]>("/api/project/entities");
  },

  async getArchetypes(): Promise<Archetype[]> {
    return fetchSidecar<Archetype[]>("/api/project/archetypes");
  },

  async updateEntity(index: number, entity: YmapEntity): Promise<{ success: boolean; entities: YmapEntity[] }> {
    return fetchSidecar("/api/project/entity?index=" + index, {
      method: "POST",
      body: JSON.stringify(entity),
    });
  },

  async deleteEntity(index: number): Promise<{ success: boolean; entities: YmapEntity[] }> {
    return fetchSidecar(`/api/project/entity/${index}`, {
      method: "DELETE",
    });
  },

  async exportYmapXml(ymapName: string, entities: YmapEntity[]): Promise<{ xml: string }> {
    return fetchSidecar<{ xml: string }>("/api/project/ymap/export-xml", {
      method: "POST",
      body: JSON.stringify({ ymapName, entities }),
    });
  },

  async importYmapXml(xmlContent: string): Promise<YmapEntity[]> {
    return fetchSidecar<YmapEntity[]>("/api/project/ymap/import-xml", {
      method: "POST",
      body: JSON.stringify({ xmlContent }),
    });
  },

  // 5. Loose Files API (Code Editor & Hex Viewer)
  async readFileText(filePath: string): Promise<ReadFileTextResult> {
    const params = new URLSearchParams({ filePath });
    return fetchSidecar<ReadFileTextResult>(`/api/file/read-text?${params.toString()}`);
  },

  async saveFileText(filePath: string, content: string): Promise<{ success: boolean; filePath: string; size: number }> {
    return fetchSidecar("/api/file/save-text", {
      method: "POST",
      body: JSON.stringify({ filePath, content }),
    });
  },

  async readFileBytes(filePath: string, offset = 0, length = 65536): Promise<ReadFileBytesResult> {
    const params = new URLSearchParams({ filePath, offset: offset.toString(), length: length.toString() });
    return fetchSidecar<ReadFileBytesResult>(`/api/file/read-bytes?${params.toString()}`);
  },

  // 6. Cache & Batch Extraction API
  async getCacheStats(): Promise<RpfCacheStats> {
    return fetchSidecar<RpfCacheStats>("/api/rpf/cache/stats");
  },

  async clearCache(): Promise<{ success: boolean; evictedCount: number }> {
    return fetchSidecar("/api/rpf/cache/clear", { method: "POST" });
  },

  async closeRpf(filePath: string): Promise<{ success: boolean; filePath: string }> {
    return fetchSidecar("/api/rpf/close", {
      method: "POST",
      body: JSON.stringify({ filePath }),
    });
  },

  async extractFolder(
    rpfPath: string,
    folderPath: string,
    outputDirectory?: string,
    asZip = false,
    recursive = true
  ): Promise<BatchExtractResult> {
    return fetchSidecar<BatchExtractResult>("/api/rpf/extract-folder", {
      method: "POST",
      body: JSON.stringify({ rpfPath, folderPath, outputDirectory, asZip, recursive }),
    });
  },

  async extractFolderZip(
    rpfPath: string,
    folderPath: string,
    recursive = true
  ): Promise<Blob> {
    const base = await getBaseUrl();
    const res = await fetch(`${base}/api/rpf/extract-folder`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ rpfPath, folderPath, asZip: true, recursive }),
    });
    if (!res.ok) {
      const errJson = await res.json().catch(() => ({}));
      throw new Error(errJson.error || "Échec de l'extraction ZIP du dossier");
    }
    return res.blob();
  },

  async extractBatch(
    rpfPath: string,
    entryPaths: string[],
    outputDirectory?: string,
    asZip = false
  ): Promise<BatchExtractResult> {
    return fetchSidecar<BatchExtractResult>("/api/rpf/extract-batch", {
      method: "POST",
      body: JSON.stringify({ rpfPath, entryPaths, outputDirectory, asZip }),
    });
  },

  async extractBatchZip(
    rpfPath: string,
    entryPaths: string[]
  ): Promise<Blob> {
    const base = await getBaseUrl();
    const res = await fetch(`${base}/api/rpf/extract-batch`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ rpfPath, entryPaths, asZip: true }),
    });
    if (!res.ok) {
      const errJson = await res.json().catch(() => ({}));
      throw new Error(errJson.error || "Échec du téléchargement groupé ZIP");
    }
    return res.blob();
  },

  async downloadFile(rpfPath: string, entryPath: string): Promise<Blob> {
    const base = await getBaseUrl();
    const params = new URLSearchParams({ rpfPath, entryPath });
    const res = await fetch(`${base}/api/rpf/file?${params.toString()}`);
    if (!res.ok) throw new Error("Échec du téléchargement du fichier");
    return res.blob();
  },

  // 7. Diagnostics & System Metrics API
  async getSystemMetrics(): Promise<SystemMetrics> {
    return fetchSidecar<SystemMetrics>("/api/system/metrics");
  },

  async clearSystemCache(): Promise<{ success: boolean; evictedCount: number }> {
    return fetchSidecar("/api/system/clear-cache", { method: "POST" });
  },

  // 8. Text & GXT2 API
  async getGxt2(rpfPath: string, entryPath: string): Promise<Gxt2Table> {
    const params = new URLSearchParams({ rpfPath, entryPath });
    return fetchSidecar<Gxt2Table>(`/api/text/gxt2?${params.toString()}`);
  },

  async searchGxt2(rpfPath: string, entryPath: string, q: string, limit = 100): Promise<Gxt2Entry[]> {
    const params = new URLSearchParams({ rpfPath, entryPath, q, limit: limit.toString() });
    return fetchSidecar<Gxt2Entry[]>(`/api/text/gxt2/search?${params.toString()}`);
  },

  async searchText(q: string, limit = 100, rpfPath?: string, entryPath?: string): Promise<Gxt2SearchResult[]> {
    const params = new URLSearchParams({ q, limit: limit.toString() });
    if (rpfPath) params.set("rpfPath", rpfPath);
    if (entryPath) params.set("entryPath", entryPath);
    return fetchSidecar<Gxt2SearchResult[]>(`/api/text/search?${params.toString()}`);
  },

  async searchRpfGxt2(rpfPath: string, q: string, limit = 250): Promise<Gxt2SearchResult[]> {
    const params = new URLSearchParams({ rpfPath, q, limit: limit.toString() });
    return fetchSidecar<Gxt2SearchResult[]>(`/api/text/search-rpf?${params.toString()}`);
  },

  async parseGxt2(req: ParseGxt2Request): Promise<Gxt2Table> {
    return fetchSidecar<Gxt2Table>("/api/text/parse-gxt2", {
      method: "POST",
      body: JSON.stringify(req),
    });
  },

  async exportGxt2Text(entries: Gxt2Entry[], fileName?: string): Promise<{ text: string }> {
    return fetchSidecar("/api/text/gxt2/export-text", {
      method: "POST",
      body: JSON.stringify({ entries, fileName }),
    });
  },

  async buildGxt2(textContent: string, entryName = "text.gxt2"): Promise<Blob> {
    const base = await getBaseUrl();
    const res = await fetch(`${base}/api/text/build-gxt2`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ textContent, entryName }),
    });
    if (!res.ok) throw new Error("Failed to build GXT2 binary");
    return res.blob();
  },

  async buildGxt2Binary(textContent: string, entryName = "text.gxt2"): Promise<Blob> {
    return this.buildGxt2(textContent, entryName);
  },

  // World 3D
  async getWorldStatus(): Promise<WorldStatus> {
    return fetchSidecar<WorldStatus>("/api/world/status");
  },

  async launchWorld3D(startPosition?: string, mode?: string): Promise<{ success: boolean; pid?: number; message: string }> {
    return fetchSidecar("/api/world/launch", {
      method: "POST",
      body: JSON.stringify({ startPosition, mode }),
    });
  },

  // Native Tauri Dialogs
  async selectFileNative(title: string, filterName: string, extensions: string[]): Promise<string | null> {
    try {
      const { invoke } = await import("@tauri-apps/api/core");
      return await invoke<string | null>("select_file", {
        title,
        filterName,
        extensions,
      });
    } catch {
      return null;
    }
  },

  async selectFolderNative(title: string): Promise<string | null> {
    try {
      const { invoke } = await import("@tauri-apps/api/core");
      return await invoke<string | null>("select_folder", { title });
    } catch {
      return null;
    }
  },
};

export function triggerFileDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
