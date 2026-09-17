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
  | "explorer"
  | "textures"
  | "audio"
  | "mods"
  | "gen9"
  | "code_editor"
  | "hex_viewer"
  | "jenkins"
  | "crypto"
  | "project_editor"
  | "settings";
