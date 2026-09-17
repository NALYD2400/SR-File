export interface AppStatus {
  running: boolean;
  version: string;
  gtaFolder: string | null;
  isGen9: boolean;
  keysLoaded: boolean;
  loadedRpfsCount: number;
  uptimeSeconds: number;
}

export interface RpfInfo {
  name: string;
  filePath: string;
  fileSize: number;
  version: number;
  entryCount: number;
  totalFileCount: number;
  totalFolderCount: number;
  totalResourceCount: number;
  encryption: string;
}

export interface RpfEntry {
  name: string;
  path: string;
  isDirectory: boolean;
  isResource: boolean;
  size: number;
  compressedSize: number;
  resourceType: string;
  encryption: string;
}

export interface TextureItem {
  name: string;
  width: number;
  height: number;
  format: string;
  mipCount: number;
}

export interface AudioStream {
  index: number;
  name: string;
  length: number;
  sampleRate: number;
  channels: number;
  type: string;
  byteLength: number;
}

// 1. Mod Manager Types
export interface ModPackageInfo {
  id: string;
  name: string;
  type: "OIV" | "DLC" | "ASI" | "Loose";
  isEnabled: boolean;
  version: string;
  author: string;
  description: string;
  sourcePath: string;
  installPath: string;
  size: number;
  fileCount: number;
  files: string[];
  conflicts: string[];
}

export interface OivManifestInfo {
  name: string;
  author: string;
  version: string;
  description: string;
  targetComponents: string[];
  actionCount: number;
  rawXml: string;
}

export interface ModInstallQueueItem {
  id: string;
  packagePath: string;
  packageName: string;
  status: "Queued" | "Installing" | "Completed" | "Failed";
  progress: number;
  message: string;
}

// 2. Gen9 Converter Types
export interface Gen9ConversionRequest {
  inputPath: string;
  outputPath: string;
  preset: "gen9_to_pc" | "pc_to_gen9" | "textures_only";
  processSubfolders: boolean;
  overwriteExisting: boolean;
  copyUnconverted: boolean;
}

export interface Gen9ConversionStatus {
  isConverting: boolean;
  progress: number;
  currentFile: string;
  totalFiles: number;
  processedFiles: number;
  logs: string[];
  error: string | null;
}

// 3. Crypto & Jenkins Types
export interface JoaatResult {
  text: string;
  hashUint: number;
  hashInt: number;
  hashHex: string;
}

export interface DictionaryItem {
  hashUint: number;
  hashHex: string;
  text: string;
  score: number;
}

export interface CryptoKeysStatus {
  aesKeyLoaded: boolean;
  aesKeyHex: string | null;
  aesKeyBase64: string | null;
  ngKeysCount: number;
  decryptTablesCount: number;
  awcKeyLoaded: boolean;
}

export interface RpfEncryptionInfo {
  filePath: string;
  encryption: string;
  isValidHeader: boolean;
  version: number;
}

// 4. Map & Project Editor Types
export interface Vector3 {
  x: number;
  y: number;
  z: number;
}

export interface Vector4 {
  x: number;
  y: number;
  z: number;
  w: number;
}

export interface YmapEntity {
  name: string;
  archetypeName: string;
  position: Vector3;
  rotation: Vector4;
  eulerRotation: Vector3;
  lodDist: number;
  childLodDist: number;
  flags: number;
  guid: number;
}

export interface Archetype {
  name: string;
  textureDictionary: string;
  physicsDictionary: string;
  lodDist: number;
  hdTextureDist: number;
  bbMin: Vector3;
  bbMax: Vector3;
  bsCentre: Vector3;
  bsRadius: number;
}

export interface ReadFileTextResult {
  filePath: string;
  content: string;
  size: number;
}

export interface ReadFileBytesResult {
  filePath: string;
  totalSize: number;
  offset: number;
  length: number;
  base64Data: string;
}

export interface ProjectSummary {
  name: string;
  version: number;
  filepath: string | null;
  ymapFiles: string[];
  ytypFiles: string[];
  ybnFiles: string[];
}

export type ActiveView =
  | "dashboard"
  | "world_3d"
  | "explorer"
  | "textures"
  | "audio"
  | "mods"
  | "gen9"
  | "gxt2_studio"
  | "code_editor"
  | "hex_viewer"
  | "jenkins"
  | "crypto"
  | "project_editor"
  | "settings";

export interface WorldStatus {
  isRunning: boolean;
  exeFound: boolean;
  exePath: string | null;
}

export interface ParseGxt2Request {
  filePath?: string;
  base64Data?: string;
  fileName?: string;
}

export interface RpfCacheItem {
  name: string;
  filePath: string;
  fileSize: number;
  entryCount: number;
}

export interface RpfCacheStats {
  loadedRpfsCount: number;
  totalIndexedEntries: number;
  cacheHits: number;
  cacheMisses: number;
  hitRatePercent: number;
  openArchives: RpfCacheItem[];
}

export interface BatchExtractResult {
  success: boolean;
  extractedCount: number;
  errorCount: number;
  totalBytes: number;
  durationMs: number;
  outputDirectory: string | null;
  errors: string[];
}

export interface SystemMetrics {
  processWorkingSetBytes: number;
  processWorkingSetMB: number;
  processPrivateMemoryBytes: number;
  processPrivateMemoryMB: number;
  processVirtualMemoryBytes: number;
  gcTotalMemoryBytes: number;
  gcTotalMemoryMB: number;
  gcGen0Collections: number;
  gcGen1Collections: number;
  gcGen2Collections: number;
  heapSizeBytes: number;
  processId: number;
  threadCount: number;
  handleCount: number;
  uptimeSeconds: number;
  startTime: string;
  processorCount: number;
  osPlatform: string;
  osArchitecture: string;
  processArchitecture: string;
  frameworkDescription: string;
  openArchivesCount: number;
  openArchives: RpfCacheItem[];
  totalIndexedEntries: number;
  cacheHits: number;
  cacheMisses: number;
}

export interface Gxt2Entry {
  hash: number;
  hexHash: string;
  text: string;
  resolvedKey: string | null;
}

export interface Gxt2Table {
  fileName: string;
  entryCount: number;
  entries: Gxt2Entry[];
}

export interface Gxt2SearchResult {
  rpfPath: string;
  entryPath: string;
  hash: number;
  hexHash: string;
  text: string;
  resolvedKey: string | null;
}

