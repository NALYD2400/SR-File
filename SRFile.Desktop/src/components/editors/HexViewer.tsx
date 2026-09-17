import React, { useState, useMemo } from "react";
import { api } from "../../api/client";
import { cn, formatBytes } from "../../lib/utils";
import {
  Binary,
  FolderOpen,
  Hash,
  Navigation,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

function base64ToUint8Array(base64: string): Uint8Array {
  const binaryString = atob(base64);
  const bytes = new Uint8Array(binaryString.length);
  for (let i = 0; i < binaryString.length; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return bytes;
}

export const HexViewer: React.FC = () => {
  const CHUNK_SIZE = 4096;

  // Sample GTA V RPF header bytes (RPF7 magic: 52 50 46 37, entries: 0x00000020, namesLen: 0x00000400, enc: 4E 45 50 4F)
  const defaultBytes = useMemo(() => {
    const arr = new Uint8Array(512);
    // RPF7 Header
    arr[0] = 0x52; arr[1] = 0x50; arr[2] = 0x46; arr[3] = 0x37; // "RPF7"
    arr[4] = 0x24; arr[5] = 0x01; arr[6] = 0x00; arr[7] = 0x00; // EntryCount = 292
    arr[8] = 0x80; arr[9] = 0x0C; arr[10] = 0x00; arr[11] = 0x00; // NamesLength = 3200
    arr[12] = 0x4F; arr[13] = 0x50; arr[14] = 0x45; arr[15] = 0x4E; // "OPEN" (Little-Endian: 0x4E45504F)

    // Fill some sample directory entries and strings
    const sampleText = "common.rpf\\data\\levels\\gta5\\props.rpf\\vehicles.meta\\carvariations.meta\\handling.meta";
    for (let i = 0; i < sampleText.length && i + 16 < 512; i++) {
      arr[16 + i] = sampleText.charCodeAt(i);
    }
    return arr;
  }, []);

  const [buffer, setBuffer] = useState<Uint8Array>(defaultBytes);
  const [fileName, setFileName] = useState<string>("header_dump.bin");
  const [filePath, setFilePath] = useState<string | null>(null);
  const [totalFileSize, setTotalFileSize] = useState<number>(defaultBytes.length);
  const [currentChunkOffset, setCurrentChunkOffset] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [selectedOffset, setSelectedOffset] = useState<number>(0);
  const [jumpOffsetInput, setJumpOffsetInput] = useState<string>("");

  // Parse bytes into 16-byte rows
  const rowsCount = Math.ceil(buffer.length / 16);

  const loadChunk = async (path: string, offset: number, targetRelativeOffset = 0) => {
    setIsLoading(true);
    try {
      const res = await api.readFileBytes(path, offset, CHUNK_SIZE);
      const bytes = base64ToUint8Array(res.base64Data);
      setBuffer(bytes);
      setCurrentChunkOffset(res.offset);
      setTotalFileSize(res.totalSize);
      setSelectedOffset(Math.min(targetRelativeOffset, Math.max(0, bytes.length - 1)));
    } catch (err: any) {
      alert(`Erreur de lecture binaire: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  // Jump to offset
  const handleJumpToOffset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!jumpOffsetInput.trim()) return;

    let target = 0;
    const clean = jumpOffsetInput.trim().toLowerCase();
    if (clean.startsWith("0x")) {
      target = parseInt(clean.substring(2), 16);
    } else {
      target = parseInt(clean, 10);
    }

    if (isNaN(target) || target < 0) return;

    if (filePath) {
      if (target >= totalFileSize) return;
      const targetChunk = Math.floor(target / CHUNK_SIZE) * CHUNK_SIZE;
      const targetRel = target - targetChunk;
      await loadChunk(filePath, targetChunk, targetRel);
    } else {
      if (target < buffer.length) {
        setSelectedOffset(target);
      }
    }
  };

  // Open native binary file
  const handleOpenFile = async () => {
    try {
      const selected = await api.selectFileNative(
        "Ouvrir un fichier binaire",
        "Tous les fichiers (*.*)",
        ["*"]
      );
      if (selected) {
        setFileName(selected.split(/[\\/]/).pop() || "fichier.bin");
        setFilePath(selected);
        await loadChunk(selected, 0, 0);
      }
    } catch { }
  };

  const handlePrevChunk = () => {
    if (!filePath || currentChunkOffset <= 0) return;
    const newOffset = Math.max(0, currentChunkOffset - CHUNK_SIZE);
    loadChunk(filePath, newOffset, 0);
  };

  const handleNextChunk = () => {
    if (!filePath || currentChunkOffset + CHUNK_SIZE >= totalFileSize) return;
    const newOffset = currentChunkOffset + CHUNK_SIZE;
    loadChunk(filePath, newOffset, 0);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      setFileName(file.name);
      setFilePath(null);
      setTotalFileSize(file.size);
      setCurrentChunkOffset(0);

      const reader = new FileReader();
      reader.onload = (ev) => {
        if (ev.target?.result) {
          const arr = new Uint8Array(ev.target.result as ArrayBuffer);
          setBuffer(arr);
          setSelectedOffset(0);
        }
      };
      // Read first 64KB for dropped file
      const slice = file.slice(0, 65536);
      reader.readAsArrayBuffer(slice);
    }
  };

  // Byte inspector calculations
  const inspectionData = useMemo(() => {
    const offset = selectedOffset;
    if (offset < 0 || offset >= buffer.length) return null;

    const absoluteOffset = currentChunkOffset + offset;
    const view = new DataView(buffer.buffer, buffer.byteOffset, buffer.byteLength);

    // Int8 / UInt8
    const u8 = buffer[offset];
    const s8 = view.getInt8(offset);

    // Int16 / UInt16 (LE & BE)
    let u16LE = 0, s16LE = 0, u16BE = 0, s16BE = 0;
    if (offset + 2 <= buffer.length) {
      u16LE = view.getUint16(offset, true);
      s16LE = view.getInt16(offset, true);
      u16BE = view.getUint16(offset, false);
      s16BE = view.getInt16(offset, false);
    }

    // Int32 / UInt32 (LE & BE)
    let u32LE = 0, s32LE = 0, u32BE = 0, s32BE = 0;
    let f32LE = 0, f32BE = 0;
    if (offset + 4 <= buffer.length) {
      u32LE = view.getUint32(offset, true);
      s32LE = view.getInt32(offset, true);
      u32BE = view.getUint32(offset, false);
      s32BE = view.getInt32(offset, false);
      f32LE = view.getFloat32(offset, true);
      f32BE = view.getFloat32(offset, false);
    }

    // Float64 (Double)
    let f64LE = 0;
    if (offset + 8 <= buffer.length) {
      f64LE = view.getFloat64(offset, true);
    }

    // Binary 8-bit
    const binStr = u8.toString(2).padStart(8, "0");

    // ASCII char
    const asciiChar = u8 >= 32 && u8 <= 126 ? String.fromCharCode(u8) : ".";

    return {
      offset: absoluteOffset,
      offsetHex: "0x" + absoluteOffset.toString(16).toUpperCase().padStart(8, "0"),
      u8,
      s8,
      u16LE,
      s16LE,
      u16BE,
      s16BE,
      u32LE,
      s32LE,
      u32BE,
      s32BE,
      f32LE,
      f32BE,
      f64LE,
      binStr,
      asciiChar,
    };
  }, [buffer, selectedOffset, currentChunkOffset]);

  return (
    <div
      className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden"
      onDragOver={(e) => e.preventDefault()}
      onDrop={handleDrop}
    >
      {/* Header Toolbar */}
      <div className="p-4 border-b border-[#222D42] bg-[#121824] flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-xl bg-[#FF7A29]/15 text-[#FF7A29]">
            <Binary className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm font-bold text-white font-mono">{fileName}</h1>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[#222D42] text-[#94A3B8]">
                {formatBytes(totalFileSize)}
              </span>
              {isLoading && (
                <span className="text-[10px] font-mono text-[#FF7A29] animate-pulse">
                  Chargement du bloc...
                </span>
              )}
            </div>
            <p className="text-xs text-[#94A3B8]">
              Éditeur hexadécimal et inspecteur de types binaires multi-endian.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* Chunk Navigation if File loaded */}
          {filePath && (
            <div className="flex items-center gap-1.5 bg-[#0A0E16] px-2 py-1 rounded-xl border border-[#222D42] text-xs font-mono">
              <button
                type="button"
                onClick={handlePrevChunk}
                disabled={currentChunkOffset <= 0 || isLoading}
                className="p-1 rounded text-[#94A3B8] hover:text-white disabled:opacity-30 cursor-pointer"
                title="Bloc précédent"
              >
                <ChevronLeft className="w-3.5 h-3.5" />
              </button>
              <span className="text-[11px] text-[#94A3B8]">
                0x{currentChunkOffset.toString(16).toUpperCase()} - 0x{(currentChunkOffset + buffer.length - 1).toString(16).toUpperCase()}
              </span>
              <button
                type="button"
                onClick={handleNextChunk}
                disabled={currentChunkOffset + CHUNK_SIZE >= totalFileSize || isLoading}
                className="p-1 rounded text-[#94A3B8] hover:text-white disabled:opacity-30 cursor-pointer"
                title="Bloc suivant"
              >
                <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          )}

          {/* Jump to offset form */}
          <form onSubmit={handleJumpToOffset} className="flex items-center gap-1.5">
            <div className="relative">
              <Navigation className="w-3.5 h-3.5 text-[#64748B] absolute left-2.5 top-2.5" />
              <input
                type="text"
                value={jumpOffsetInput}
                onChange={(e) => setJumpOffsetInput(e.target.value)}
                placeholder="Offset (0x0000)..."
                className="bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl pl-8 pr-3 py-1.5 text-xs text-white font-mono w-36"
              />
            </div>
            <button
              type="submit"
              className="px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer"
            >
              Aller à
            </button>
          </form>

          <button
            onClick={handleOpenFile}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-all shadow-[0_0_15px_rgba(255,122,41,0.25)] cursor-pointer"
          >
            <FolderOpen className="w-3.5 h-3.5" />
            <span>Ouvrir un binaire</span>
          </button>
        </div>
      </div>

      {/* Main Split: Hex View (Left) & Inspector Sidebar (Right) */}
      <div className="flex-1 flex overflow-hidden">
        {/* Hex Rows Container */}
        <div className="flex-1 overflow-y-auto p-6 font-mono text-xs select-none custom-scrollbar">
          <div className="min-w-[620px] space-y-1">
            {/* Hex Column Headers */}
            <div className="flex items-center text-[#64748B] text-[11px] pb-2 border-b border-[#222D42]/60 select-none">
              <span className="w-24 shrink-0">OFFSET</span>
              <div className="flex-1 flex gap-2">
                <span className="w-[184px]">00 01 02 03 04 05 06 07</span>
                <span className="w-2 text-center text-[#334360]">|</span>
                <span className="w-[184px]">08 09 0A 0B 0C 0D 0E 0F</span>
              </div>
              <span className="w-36 shrink-0 pl-4">DÉCODAGE ASCII</span>
            </div>

            {/* Rows */}
            {Array.from({ length: rowsCount }).map((_, rowIndex) => {
              const rowOffset = currentChunkOffset + rowIndex * 16;
              const rowOffsetHex = rowOffset.toString(16).toUpperCase().padStart(8, "0");

              return (
                <div
                  key={rowIndex}
                  className="flex items-center hover:bg-[#121824]/60 rounded px-1 py-0.5 leading-6 transition-colors"
                >
                  {/* Offset Header */}
                  <span className="w-24 text-[#64748B] shrink-0 font-bold select-none">
                    {rowOffsetHex}
                  </span>

                  {/* Hex Bytes (16 bytes split in 2 blocks of 8) */}
                  <div className="flex-1 flex gap-2">
                    {/* First 8 bytes */}
                    <div className="flex gap-1.5 w-[184px]">
                      {Array.from({ length: 8 }).map((_, byteIdx) => {
                        const currentByteOffset = rowOffset + byteIdx;
                        if (currentByteOffset >= buffer.length) {
                          return <span key={byteIdx} className="w-5 text-center text-transparent">..</span>;
                        }
                        const b = buffer[currentByteOffset];
                        const isSelected = selectedOffset === currentByteOffset;

                        return (
                          <span
                            key={byteIdx}
                            onClick={() => setSelectedOffset(currentByteOffset)}
                            className={cn(
                              "w-5 text-center cursor-pointer rounded transition-colors",
                              isSelected
                                ? "bg-[#FF7A29] text-white font-bold shadow-[0_0_8px_rgba(255,122,41,0.5)]"
                                : b === 0
                                ? "text-[#475569]"
                                : "text-[#F1F5F9] hover:bg-[#1E283D] hover:text-[#FF7A29]"
                            )}
                          >
                            {b.toString(16).toUpperCase().padStart(2, "0")}
                          </span>
                        );
                      })}
                    </div>

                    <span className="w-2 text-center text-[#222D42] select-none">|</span>

                    {/* Second 8 bytes */}
                    <div className="flex gap-1.5 w-[184px]">
                      {Array.from({ length: 8 }).map((_, byteIdx) => {
                        const currentByteOffset = rowOffset + 8 + byteIdx;
                        if (currentByteOffset >= buffer.length) {
                          return <span key={byteIdx} className="w-5 text-center text-transparent">..</span>;
                        }
                        const b = buffer[currentByteOffset];
                        const isSelected = selectedOffset === currentByteOffset;

                        return (
                          <span
                            key={byteIdx}
                            onClick={() => setSelectedOffset(currentByteOffset)}
                            className={cn(
                              "w-5 text-center cursor-pointer rounded transition-colors",
                              isSelected
                                ? "bg-[#FF7A29] text-white font-bold shadow-[0_0_8px_rgba(255,122,41,0.5)]"
                                : b === 0
                                ? "text-[#475569]"
                                : "text-[#F1F5F9] hover:bg-[#1E283D] hover:text-[#FF7A29]"
                            )}
                          >
                            {b.toString(16).toUpperCase().padStart(2, "0")}
                          </span>
                        );
                      })}
                    </div>
                  </div>

                  {/* ASCII Representation on Right */}
                  <div className="w-36 shrink-0 pl-4 flex select-none text-[11px]">
                    {Array.from({ length: 16 }).map((_, byteIdx) => {
                      const currentByteOffset = rowOffset + byteIdx;
                      if (currentByteOffset >= buffer.length) return null;
                      const b = buffer[currentByteOffset];
                      const isSelected = selectedOffset === currentByteOffset;
                      const char = b >= 32 && b <= 126 ? String.fromCharCode(b) : ".";

                      return (
                        <span
                          key={byteIdx}
                          onClick={() => setSelectedOffset(currentByteOffset)}
                          className={cn(
                            "w-2 text-center cursor-pointer transition-colors",
                            isSelected
                              ? "text-[#FF7A29] font-bold bg-[#FF7A29]/20 rounded"
                              : b === 0
                              ? "text-[#334360]"
                              : "text-[#94A3B8] hover:text-white"
                          )}
                        >
                          {char}
                        </span>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Right Inspector Panel */}
        <div className="w-80 border-l border-[#222D42] bg-[#121824] p-5 flex flex-col space-y-4 overflow-y-auto shrink-0 custom-scrollbar">
          <div className="flex items-center justify-between pb-3 border-b border-[#222D42]">
            <h2 className="text-xs font-bold text-white font-mono uppercase tracking-wider flex items-center gap-2">
              <Hash className="w-4 h-4 text-[#FF7A29]" />
              <span>Inspecteur d'Octets</span>
            </h2>
            <span className="text-[11px] font-mono text-[#FF7A29] font-bold">
              {inspectionData?.offsetHex}
            </span>
          </div>

          {inspectionData ? (
            <div className="space-y-3 text-xs font-mono">
              {/* Offset Info */}
              <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                <div className="text-[10px] text-[#64748B]">Position dans le fichier</div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Hexadécimal :</span>
                  <span className="text-white font-bold">{inspectionData.offsetHex}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Décimal :</span>
                  <span className="text-white font-bold">{inspectionData.offset}</span>
                </div>
              </div>

              {/* 8-bit Types */}
              <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-2">
                <div className="text-[10px] text-[#64748B] uppercase">Valeurs 8-bit (1 Octet)</div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Binaire :</span>
                  <span className="text-amber-400 font-bold">{inspectionData.binStr}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">UInt8 (Octet) :</span>
                  <span className="text-white font-bold">{inspectionData.u8}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Int8 (Signé) :</span>
                  <span className="text-white font-bold">{inspectionData.s8}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Caractère ASCII :</span>
                  <span className="text-emerald-400 font-bold">'{inspectionData.asciiChar}'</span>
                </div>
              </div>

              {/* 16-bit Types */}
              <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-2">
                <div className="text-[10px] text-[#64748B] uppercase">Valeurs 16-bit (2 Octets)</div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">UInt16 (Little-Endian) :</span>
                  <span className="text-white font-bold">{inspectionData.u16LE}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Int16 (Little-Endian) :</span>
                  <span className="text-white font-bold">{inspectionData.s16LE}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">UInt16 (Big-Endian) :</span>
                  <span className="text-slate-400 font-bold">{inspectionData.u16BE}</span>
                </div>
              </div>

              {/* 32-bit & Floating Point Types */}
              <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-2">
                <div className="text-[10px] text-[#64748B] uppercase">Valeurs 32-bit (4 Octets)</div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">UInt32 (LE) :</span>
                  <span className="text-white font-bold">{inspectionData.u32LE}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Int32 (LE) :</span>
                  <span className="text-white font-bold">{inspectionData.s32LE}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Float32 (Single LE) :</span>
                  <span className="text-sky-400 font-bold">{inspectionData.f32LE.toFixed(4)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-[#94A3B8]">Float64 (Double) :</span>
                  <span className="text-purple-400 font-bold">{inspectionData.f64LE.toExponential(4)}</span>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center text-[#64748B] text-xs py-8">
              Cliquez sur un octet dans la grille pour inspecter sa valeur.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
