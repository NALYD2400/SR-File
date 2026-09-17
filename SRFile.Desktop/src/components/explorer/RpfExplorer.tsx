import React, { useState, useMemo, useRef } from "react";
import { RpfInfo, RpfEntry, TextureItem, AudioStream } from "../../types";
import { api } from "../../api/client";
import { useVirtualizer } from "@tanstack/react-virtual";
import {
  Folder,
  File,
  Image as ImageIcon,
  Music,
  FileCode,
  Box,
  Layers,
  ChevronRight,
  ArrowUp,
  Search,
  Download,
  X,
  ZoomIn,
  ZoomOut,
  Copy,
  Check,
  HardDrive,
  RefreshCw,
} from "lucide-react";
import { formatBytes, cn } from "../../lib/utils";

interface RpfExplorerProps {
  currentRpf: RpfInfo | null;
  onOpenAnotherRpf: () => void;
}

export const RpfExplorer: React.FC<RpfExplorerProps> = ({ currentRpf, onOpenAnotherRpf }) => {
  const [currentPath, setCurrentPath] = useState<string>("");
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [selectedEntry, setSelectedEntry] = useState<RpfEntry | null>(null);
  
  // Data queries
  const [entries, setEntries] = useState<RpfEntry[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  // Preview states
  const [previewTab, setPreviewTab] = useState<"info" | "content">("content");
  const [textContent, setTextContent] = useState<string | null>(null);
  const [textures, setTextures] = useState<TextureItem[]>([]);
  const [selectedTexture, setSelectedTexture] = useState<TextureItem | null>(null);
  const [textureZoom, setTextureZoom] = useState<number>(1);
  const [textureMip, setTextureMip] = useState<number>(0);
  const [audioStreams, setAudioStreams] = useState<AudioStream[]>([]);
  const [activeAudioIdx, setActiveAudioIdx] = useState<number>(0);
  const [isPlayingAudio, setIsPlayingAudio] = useState<boolean>(false);
  const [copiedText, setCopiedText] = useState<boolean>(false);
  const [isPreviewLoading, setIsPreviewLoading] = useState<boolean>(false);

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const tableContainerRef = useRef<HTMLDivElement | null>(null);

  // Load entries when RPF or currentPath changes
  const loadDirectory = async (path: string) => {
    if (!currentRpf) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await api.getEntries(currentRpf.filePath, path);
      setEntries(data);
      setCurrentPath(path);
      setSelectedEntry(null);
      setTextContent(null);
      setTextures([]);
      setAudioStreams([]);
    } catch (err: any) {
      setError(err.message || "Impossible de charger le dossier");
    } finally {
      setIsLoading(false);
    }
  };

  // Initial load
  React.useEffect(() => {
    if (currentRpf) {
      loadDirectory("");
    }
  }, [currentRpf]);

  // Filtered entries
  const filteredEntries = useMemo(() => {
    if (!searchQuery.trim()) return entries;
    const q = searchQuery.toLowerCase();
    return entries.filter((e) => e.name.toLowerCase().includes(q));
  }, [entries, searchQuery]);

  // Virtualizer for 60fps scrolling
  const rowVirtualizer = useVirtualizer({
    count: filteredEntries.length,
    getScrollElement: () => tableContainerRef.current,
    estimateSize: () => 40,
    overscan: 15,
  });

  // Breadcrumbs (avoid duplicating root RPF name)
  const breadcrumbParts = useMemo(() => {
    if (!currentPath) return [];
    const parts = currentPath.split(/[\\/]/).filter(Boolean);
    if (parts.length > 0 && currentRpf && parts[0].toLowerCase() === currentRpf.name.toLowerCase()) {
      return parts.slice(1);
    }
    return parts;
  }, [currentPath, currentRpf]);

  const navigateToBreadcrumb = (index: number) => {
    if (index === -1) {
      loadDirectory("");
      return;
    }
    if (!currentRpf) return;
    const subParts = breadcrumbParts.slice(0, index + 1);
    const newPath = [currentRpf.name, ...subParts].join("\\");
    loadDirectory(newPath);
  };

  const navigateUp = () => {
    if (breadcrumbParts.length <= 1) {
      loadDirectory("");
    } else {
      navigateToBreadcrumb(breadcrumbParts.length - 2);
    }
  };

  // Handle entry click / inspect
  const handleSelectEntry = async (entry: RpfEntry) => {
    setSelectedEntry(entry);
    setPreviewTab("content");
    setTextContent(null);
    setTextures([]);
    setSelectedTexture(null);
    setTextureMip(0);
    setAudioStreams([]);
    setIsPreviewLoading(true);

    if (entry.isDirectory) {
      setIsPreviewLoading(false);
      return;
    }

    if (!currentRpf) return;

    try {
      const ext = entry.resourceType.toLowerCase();
      if (ext === "ytd") {
        const texList = await api.getTextures(currentRpf.filePath, entry.path);
        setTextures(texList);
        if (texList.length > 0) {
          setSelectedTexture(texList[0]);
        }
      } else if (ext === "awc") {
        const streams = await api.getAudioStreams(currentRpf.filePath, entry.path);
        setAudioStreams(streams);
        setActiveAudioIdx(0);
      } else if (["xml", "meta", "dat", "txt", "cfg", "ini", "json", "ytyp", "ymap"].includes(ext)) {
        const { content } = await api.getFileText(currentRpf.filePath, entry.path);
        setTextContent(content);
      }
    } catch (err: any) {
      console.error("Preview load error:", err);
    } finally {
      setIsPreviewLoading(false);
    }
  };

  const handleDoubleClickEntry = (entry: RpfEntry) => {
    if (entry.isDirectory || entry.name.toLowerCase().endsWith(".rpf") || entry.resourceType.toLowerCase() === "rpf") {
      loadDirectory(entry.path);
    } else {
      handleSelectEntry(entry);
    }
  };

  const getFileIcon = (entry: RpfEntry) => {
    if (entry.isDirectory) return <Folder className="w-4 h-4 text-[#FF7A29]" />;
    const ext = entry.resourceType.toLowerCase();
    switch (ext) {
      case "ytd":
        return <ImageIcon className="w-4 h-4 text-sky-400" />;
      case "awc":
        return <Music className="w-4 h-4 text-purple-400" />;
      case "ymap":
      case "ybn":
        return <Layers className="w-4 h-4 text-emerald-400" />;
      case "ydr":
      case "ydd":
      case "yft":
        return <Box className="w-4 h-4 text-amber-400" />;
      case "xml":
      case "meta":
        return <FileCode className="w-4 h-4 text-cyan-400" />;
      default:
        return <File className="w-4 h-4 text-[#64748B]" />;
    }
  };

  const handleCopyText = () => {
    if (textContent) {
      navigator.clipboard.writeText(textContent);
      setCopiedText(true);
      setTimeout(() => setCopiedText(false), 2000);
    }
  };

  if (!currentRpf) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-8 bg-[#0A0E16] text-[#94A3B8]">
        <div className="p-4 rounded-2xl bg-[#121824] border border-[#222D42] text-center max-w-md space-y-4 shadow-xl">
          <HardDrive className="w-12 h-12 text-[#FF7A29] mx-auto opacity-80" />
          <div>
            <h2 className="text-lg font-bold text-white">Aucune archive RPF chargée</h2>
            <p className="text-xs text-[#64748B] mt-1">
              Sélectionnez une archive .rpf dans le Hub ou cliquez ci-dessous pour démarrer l'exploration.
            </p>
          </div>
          <button
            onClick={onOpenAnotherRpf}
            className="w-full py-2.5 px-4 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold text-xs shadow-lg transition-colors cursor-pointer"
          >
            Choisir une archive RPF
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Top Toolbar & Navigation */}
      <div className="p-3 border-b border-[#222D42] bg-[#121824]/80 backdrop-blur-md flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 shrink-0">
        {/* Breadcrumb Path Bar */}
        <div className="flex items-center gap-1.5 overflow-x-auto custom-scrollbar text-xs font-mono py-1">
          <button
            onClick={navigateUp}
            disabled={breadcrumbParts.length === 0}
            className="p-1 rounded bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] disabled:opacity-30 disabled:cursor-not-allowed transition-colors shrink-0"
            title="Dossier parent"
          >
            <ArrowUp className="w-3.5 h-3.5" />
          </button>

          <button
            onClick={() => navigateToBreadcrumb(-1)}
            className={cn(
              "px-2 py-0.5 rounded transition-colors shrink-0",
              breadcrumbParts.length === 0 ? "bg-[#FF7A29]/20 text-[#FF7A29] font-bold" : "text-[#94A3B8] hover:text-white"
            )}
          >
            {currentRpf.name}
          </button>

          {breadcrumbParts.map((part, idx) => (
            <React.Fragment key={idx}>
              <ChevronRight className="w-3 h-3 text-[#64748B] shrink-0" />
              <button
                onClick={() => navigateToBreadcrumb(idx)}
                className={cn(
                  "px-2 py-0.5 rounded transition-colors shrink-0 truncate max-w-[150px]",
                  idx === breadcrumbParts.length - 1
                    ? "bg-[#FF7A29]/20 text-[#FF7A29] font-bold"
                    : "text-[#94A3B8] hover:text-white"
                )}
                title={part}
              >
                {part}
              </button>
            </React.Fragment>
          ))}
        </div>

        {/* Filter Input & Controls */}
        <div className="flex items-center gap-2 shrink-0">
          <div className="relative w-48 sm:w-64">
            <Search className="w-3.5 h-3.5 text-[#64748B] absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Filtrer ce dossier..."
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg pl-8 pr-3 py-1.5 text-xs text-white placeholder-[#64748B] font-mono"
            />
            {searchQuery && (
              <button
                onClick={() => setSearchQuery("")}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#64748B] hover:text-white"
              >
                <X className="w-3 h-3" />
              </button>
            )}
          </div>

          <button
            onClick={() => loadDirectory(currentPath)}
            className="p-1.5 rounded-lg bg-[#0A0E16] hover:bg-[#172030] border border-[#222D42] text-[#94A3B8] hover:text-white transition-colors cursor-pointer"
            title="Actualiser"
          >
            <RefreshCw className={cn("w-3.5 h-3.5", isLoading && "animate-spin text-[#FF7A29]")} />
          </button>
        </div>
      </div>

      {error && (
        <div className="px-4 py-2 bg-red-950/50 border-b border-red-800 text-red-300 text-xs">
          {error}
        </div>
      )}

      {/* Main Split View: Virtual Table + Preview Drawer */}
      <div className="flex-1 flex overflow-hidden">
        {/* Virtualized Table Container */}
        <div className="flex-1 flex flex-col overflow-hidden bg-[#0A0E16]">
          {/* Table Header */}
          <div className="grid grid-cols-12 gap-2 px-4 py-2 border-b border-[#222D42] bg-[#121824]/50 text-[11px] font-mono uppercase text-[#64748B] font-semibold select-none shrink-0">
            <div className="col-span-6 flex items-center gap-2">Nom</div>
            <div className="col-span-2">Format / Type</div>
            <div className="col-span-2 text-right">Taille</div>
            <div className="col-span-2 text-right">Compressé</div>
          </div>

          {/* Table Body with TanStack Virtualizer */}
          <div ref={tableContainerRef} className="flex-1 overflow-y-auto custom-scrollbar">
            {isLoading ? (
              <div className="flex items-center justify-center h-48 text-xs text-[#64748B] gap-2">
                <RefreshCw className="w-4 h-4 animate-spin text-[#FF7A29]" />
                <span>Chargement de la structure RPF...</span>
              </div>
            ) : filteredEntries.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-48 text-xs text-[#64748B] gap-1">
                <span>Aucune entrée dans ce dossier.</span>
                {searchQuery && <span className="text-[10px] text-[#64748B]">Aucun résultat pour "{searchQuery}"</span>}
              </div>
            ) : (
              <div
                style={{
                  height: `${rowVirtualizer.getTotalSize()}px`,
                  width: "100%",
                  position: "relative",
                }}
              >
                {rowVirtualizer.getVirtualItems().map((virtualRow) => {
                  const entry = filteredEntries[virtualRow.index];
                  const isSelected = selectedEntry?.path === entry.path;
                  return (
                    <div
                      key={virtualRow.index}
                      onClick={() => handleSelectEntry(entry)}
                      onDoubleClick={() => handleDoubleClickEntry(entry)}
                      style={{
                        position: "absolute",
                        top: 0,
                        left: 0,
                        width: "100%",
                        height: `${virtualRow.size}px`,
                        transform: `translateY(${virtualRow.start}px)`,
                      }}
                      className={cn(
                        "grid grid-cols-12 gap-2 px-4 items-center text-xs font-mono select-none cursor-pointer border-b border-[#222D42]/30 transition-colors",
                        isSelected
                          ? "bg-[#FF7A29]/15 text-white border-[#FF7A29]/40"
                          : "hover:bg-[#121824] text-[#94A3B8] hover:text-white"
                      )}
                    >
                      {/* Name + Icon */}
                      <div className="col-span-6 flex items-center gap-2.5 truncate">
                        {getFileIcon(entry)}
                        <span className="truncate font-medium">{entry.name}</span>
                      </div>

                      {/* Type / Resource */}
                      <div className="col-span-2 truncate">
                        <span className={cn(
                          "text-[10px] px-1.5 py-0.2 rounded font-semibold uppercase",
                          entry.isDirectory
                            ? "bg-[#FF7A29]/10 text-[#FF7A29]"
                            : entry.isResource
                            ? "bg-purple-500/10 text-purple-400"
                            : "bg-[#222D42] text-[#94A3B8]"
                        )}>
                          {entry.resourceType}
                        </span>
                      </div>

                      {/* Uncompressed Size */}
                      <div className="col-span-2 text-right font-mono text-[11px] text-[#94A3B8]">
                        {entry.isDirectory ? "-" : formatBytes(entry.size)}
                      </div>

                      {/* Compressed Size */}
                      <div className="col-span-2 text-right font-mono text-[11px] text-[#64748B]">
                        {entry.isDirectory || entry.compressedSize === 0
                          ? "-"
                          : formatBytes(entry.compressedSize)}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Table Footer Status */}
          <div className="px-4 py-2 border-t border-[#222D42] bg-[#0A0E16] text-[11px] font-mono text-[#64748B] flex items-center justify-between shrink-0">
            <span>
              {filteredEntries.length} élément(s) {searchQuery && `(filtré sur ${entries.length})`}
            </span>
            <span>Archive: {currentRpf.name}</span>
          </div>
        </div>

        {/* Interactive Preview Drawer */}
        {selectedEntry && (
          <aside className="w-96 border-l border-[#222D42] bg-[#121824] flex flex-col h-full overflow-hidden shadow-2xl shrink-0 z-20">
            {/* Drawer Header */}
            <div className="p-3 border-b border-[#222D42] flex items-center justify-between bg-[#151D2D]">
              <div className="flex items-center gap-2 truncate">
                {getFileIcon(selectedEntry)}
                <span className="font-bold text-xs text-white truncate" title={selectedEntry.name}>
                  {selectedEntry.name}
                </span>
              </div>
              <button
                onClick={() => setSelectedEntry(null)}
                className="p-1 rounded text-[#64748B] hover:text-white hover:bg-[#1A2234] transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Drawer Tabs */}
            <div className="flex border-b border-[#222D42] text-xs font-semibold bg-[#0A0E16]/40">
              <button
                onClick={() => setPreviewTab("content")}
                className={cn(
                  "flex-1 py-2 text-center transition-colors cursor-pointer border-b-2",
                  previewTab === "content"
                    ? "border-[#FF7A29] text-white bg-[#121824]"
                    : "border-transparent text-[#64748B] hover:text-white"
                )}
              >
                Aperçu
              </button>
              <button
                onClick={() => setPreviewTab("info")}
                className={cn(
                  "flex-1 py-2 text-center transition-colors cursor-pointer border-b-2",
                  previewTab === "info"
                    ? "border-[#FF7A29] text-white bg-[#121824]"
                    : "border-transparent text-[#64748B] hover:text-white"
                )}
              >
                Métadonnées
              </button>
            </div>

            {/* Drawer Content */}
            <div className="flex-1 overflow-y-auto p-4 custom-scrollbar">
              {isPreviewLoading ? (
                <div className="flex flex-col items-center justify-center h-48 text-xs text-[#64748B] gap-2">
                  <RefreshCw className="w-5 h-5 animate-spin text-[#FF7A29]" />
                  <span>Décompression par le Sidecar...</span>
                </div>
              ) : previewTab === "content" ? (
                <div className="space-y-4">
                  {/* Texture Preview Mode (.ytd) */}
                  {selectedEntry.resourceType.toLowerCase() === "ytd" && (
                    <div className="space-y-3">
                      {textures.length > 0 ? (
                        <>
                          <div className="flex items-center justify-between text-xs">
                            <span className="text-[#94A3B8] font-mono">
                              Textures: {textures.length}
                            </span>
                            <div className="flex items-center gap-1">
                              <button
                                onClick={() => setTextureZoom((z) => Math.max(0.5, z - 0.25))}
                                className="p-1 rounded bg-[#172030] hover:bg-[#1E283D] text-[#94A3B8]"
                                title="Zoom arrière"
                              >
                                <ZoomOut className="w-3.5 h-3.5" />
                              </button>
                              <span className="text-[10px] font-mono text-[#64748B] w-8 text-center">
                                {Math.round(textureZoom * 100)}%
                              </span>
                              <button
                                onClick={() => setTextureZoom((z) => Math.min(3, z + 0.25))}
                                className="p-1 rounded bg-[#172030] hover:bg-[#1E283D] text-[#94A3B8]"
                                title="Zoom avant"
                              >
                                <ZoomIn className="w-3.5 h-3.5" />
                              </button>
                            </div>
                          </div>

                          {/* Texture Selector List */}
                          <div className="flex gap-1.5 overflow-x-auto pb-1 custom-scrollbar">
                            {textures.map((tex) => (
                              <button
                                key={tex.name}
                                onClick={() => setSelectedTexture(tex)}
                                className={cn(
                                  "px-2.5 py-1 rounded text-[11px] font-mono truncate shrink-0 transition-colors cursor-pointer border",
                                  selectedTexture?.name === tex.name
                                    ? "bg-[#FF7A29] text-white border-[#FF7A29]"
                                    : "bg-[#0A0E16] text-[#94A3B8] border-[#222D42] hover:text-white"
                                )}
                              >
                                {tex.name}
                              </button>
                            ))}
                          </div>

                          {/* Preview Canvas / Image */}
                          {selectedTexture && (
                            <div className="space-y-2">
                              <div className="relative rounded-xl border border-[#222D42] bg-[radial-gradient(#222D42_1px,transparent_1px)] [background-size:12px_12px] bg-[#0A0E16] p-4 flex items-center justify-center min-h-[220px] overflow-hidden">
                                <img
                                  src={api.getTexturePngUrl(
                                    currentRpf.filePath,
                                    selectedEntry.path,
                                    selectedTexture.name,
                                    textureMip
                                  )}
                                  alt={selectedTexture.name}
                                  style={{ transform: `scale(${textureZoom})` }}
                                  className="max-h-64 object-contain transition-transform shadow-lg rounded"
                                />
                              </div>

                              {/* Texture specs */}
                              <div className="grid grid-cols-3 gap-2 p-2.5 rounded-lg bg-[#0A0E16] border border-[#222D42] text-[11px] font-mono text-center">
                                <div>
                                  <div className="text-[#64748B] text-[9px]">RÉSOLUTION</div>
                                  <div className="text-white font-bold">
                                    {Math.max(1, selectedTexture.width >> textureMip)}x{Math.max(1, selectedTexture.height >> textureMip)}
                                  </div>
                                </div>
                                <div>
                                  <div className="text-[#64748B] text-[9px]">FORMAT</div>
                                  <div className="text-[#FF7A29] font-bold truncate">
                                    {selectedTexture.format.replace("D3DFMT_", "")}
                                  </div>
                                </div>
                                <div>
                                  <div className="text-[#64748B] text-[9px]">MIPMAPS</div>
                                  <div className="text-white font-bold">{selectedTexture.mipCount}</div>
                                </div>
                              </div>

                              {/* Mipmap selector bar */}
                              {selectedTexture.mipCount > 1 && (
                                <div className="flex items-center gap-2 p-2 rounded-lg bg-[#0A0E16] border border-[#222D42]">
                                  <span className="text-[10px] font-mono text-[#64748B] shrink-0">MIP:</span>
                                  <div className="flex gap-1 overflow-x-auto custom-scrollbar">
                                    {Array.from({ length: Math.min(selectedTexture.mipCount, 8) }, (_, i) => (
                                      <button
                                        key={i}
                                        onClick={() => setTextureMip(i)}
                                        className={cn(
                                          "px-2 py-0.5 rounded text-[10px] font-mono transition-colors cursor-pointer",
                                          textureMip === i
                                            ? "bg-[#FF7A29] text-white font-bold"
                                            : "bg-[#172030] text-[#94A3B8] hover:text-white"
                                        )}
                                      >
                                        {i === 0 ? "Full" : `M${i}`}
                                      </button>
                                    ))}
                                  </div>
                                </div>
                              )}

                              {/* Download actions */}
                              <div className="flex gap-2">
                                <a
                                  href={api.getTexturePngUrl(
                                    currentRpf.filePath,
                                    selectedEntry.path,
                                    selectedTexture.name
                                  )}
                                  download={`${selectedTexture.name}.png`}
                                  className="flex-1 flex items-center justify-center gap-1.5 py-2 px-3 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-white transition-colors"
                                >
                                  <Download className="w-3.5 h-3.5 text-[#FF7A29]" />
                                  <span>Télécharger PNG</span>
                                </a>
                                <a
                                  href={api.getTextureDdsUrl(
                                    currentRpf.filePath,
                                    selectedEntry.path,
                                    selectedTexture.name
                                  )}
                                  download={`${selectedTexture.name}.dds`}
                                  className="flex-1 flex items-center justify-center gap-1.5 py-2 px-3 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-white transition-colors"
                                >
                                  <Download className="w-3.5 h-3.5 text-sky-400" />
                                  <span>Format DDS</span>
                                </a>
                              </div>
                            </div>
                          )}
                        </>
                      ) : (
                        <div className="text-xs text-[#64748B] text-center py-6">
                          Aucune texture trouvée dans ce dictionnaire.
                        </div>
                      )}
                    </div>
                  )}

                  {/* Audio Player Mode (.awc) */}
                  {selectedEntry.resourceType.toLowerCase() === "awc" && (
                    <div className="space-y-4">
                      <div className="text-xs text-[#94A3B8] font-mono">
                        Pistes audio: {audioStreams.length}
                      </div>

                      {/* Track List */}
                      <div className="space-y-1.5 max-h-48 overflow-y-auto custom-scrollbar">
                        {audioStreams.map((s, idx) => (
                          <div
                            key={idx}
                            onClick={() => {
                              setActiveAudioIdx(idx);
                              setIsPlayingAudio(true);
                            }}
                            className={cn(
                              "flex items-center justify-between p-2 rounded-lg text-xs font-mono cursor-pointer transition-colors border",
                              activeAudioIdx === idx
                                ? "bg-purple-500/20 border-purple-500/40 text-white"
                                : "bg-[#0A0E16] border-[#222D42] text-[#94A3B8] hover:text-white"
                            )}
                          >
                            <div className="flex items-center gap-2 truncate">
                              <Music className="w-3.5 h-3.5 text-purple-400 shrink-0" />
                              <span className="truncate">{s.name}</span>
                            </div>
                            <span className="text-[10px] text-[#64748B] shrink-0 font-mono">
                              {s.length.toFixed(1)}s
                            </span>
                          </div>
                        ))}
                      </div>

                      {/* HTML5 Audio Player for selected track */}
                      {audioStreams.length > 0 && (
                        <div className="p-4 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-3">
                          <div className="text-xs font-bold text-white truncate">
                            {audioStreams[activeAudioIdx]?.name}
                          </div>

                          <audio
                            ref={audioRef}
                            controls
                            autoPlay={isPlayingAudio}
                            src={api.getAudioWavUrl(
                              currentRpf.filePath,
                              selectedEntry.path,
                              activeAudioIdx
                            )}
                            className="w-full h-8"
                          />

                          <div className="grid grid-cols-2 gap-2 text-[10px] font-mono text-[#64748B] pt-2 border-t border-[#222D42]">
                            <div>Échantillonnage: {audioStreams[activeAudioIdx]?.sampleRate} Hz</div>
                            <div>Canaux: {audioStreams[activeAudioIdx]?.channels}</div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* Text / XML Preview Mode */}
                  {textContent !== null && (
                    <div className="space-y-2">
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-[#94A3B8] font-mono">Fichier texte / XML</span>
                        <button
                          onClick={handleCopyText}
                          className="flex items-center gap-1 px-2.5 py-1 rounded bg-[#172030] hover:bg-[#1E283D] text-[11px] font-mono text-white transition-colors cursor-pointer"
                        >
                          {copiedText ? (
                            <>
                              <Check className="w-3 h-3 text-emerald-400" />
                              <span className="text-emerald-400">Copié !</span>
                            </>
                          ) : (
                            <>
                              <Copy className="w-3 h-3" />
                              <span>Copier</span>
                            </>
                          )}
                        </button>
                      </div>

                      <pre className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] font-mono text-[11px] text-[#94A3B8] overflow-x-auto max-h-96 custom-scrollbar whitespace-pre-wrap leading-relaxed">
                        {textContent.slice(0, 15000)}
                        {textContent.length > 15000 && (
                          <div className="text-[#FF7A29] text-[10px] mt-2">
                            [... Contenu tronqué à 15,000 caractères pour fluidité ...]
                          </div>
                        )}
                      </pre>
                    </div>
                  )}

                  {/* Generic File Download */}
                  {!selectedEntry.isDirectory && (
                    <a
                      href={api.getRawFileUrl(currentRpf.filePath, selectedEntry.path)}
                      download={selectedEntry.name}
                      className="w-full flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold text-xs transition-colors cursor-pointer shadow-lg"
                    >
                      <Download className="w-4 h-4" />
                      <span>Exporter ce fichier</span>
                    </a>
                  )}
                </div>
              ) : (
                /* Metadata Tab */
                <div className="space-y-3 font-mono text-xs">
                  <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-2">
                    <div>
                      <span className="text-[#64748B] block text-[10px]">NOM DU FICHIER</span>
                      <span className="text-white font-semibold break-all">{selectedEntry.name}</span>
                    </div>
                    <div>
                      <span className="text-[#64748B] block text-[10px]">CHEMIN DANS L'ARCHIVE</span>
                      <span className="text-white break-all text-[11px]">{selectedEntry.path}</span>
                    </div>
                    <div>
                      <span className="text-[#64748B] block text-[10px]">TYPE DE RESSOURCE</span>
                      <span className="text-[#FF7A29] font-bold">{selectedEntry.resourceType}</span>
                    </div>
                    <div>
                      <span className="text-[#64748B] block text-[10px]">TAILLE DÉCOMPRESSÉE</span>
                      <span className="text-white">{formatBytes(selectedEntry.size)} ({selectedEntry.size} octets)</span>
                    </div>
                    {selectedEntry.compressedSize > 0 && (
                      <div>
                        <span className="text-[#64748B] block text-[10px]">TAILLE COMPRESSÉE</span>
                        <span className="text-white">{formatBytes(selectedEntry.compressedSize)}</span>
                      </div>
                    )}
                    <div>
                      <span className="text-[#64748B] block text-[10px]">CHIFFREMENT</span>
                      <span className="text-emerald-400 font-semibold">{selectedEntry.encryption}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </aside>
        )}
      </div>
    </div>
  );
};
