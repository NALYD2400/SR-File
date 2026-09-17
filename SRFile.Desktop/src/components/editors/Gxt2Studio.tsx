import React, { useState, useMemo, useRef } from "react";
import { RpfInfo, Gxt2Entry, Gxt2SearchResult } from "../../types";
import { api, triggerFileDownload } from "../../api/client";
import { useToast } from "../common/Toast";
import {
  Type,
  Search,
  Plus,
  Trash2,
  Download,
  Upload,
  FileCode,
  Copy,
  Sparkles,
  RefreshCw,
  FileText
} from "lucide-react";
import { cn } from "../../lib/utils";

interface Gxt2StudioProps {
  currentRpf?: RpfInfo | null;
  onNavigateToExplorer?: () => void;
}

// GTA V In-Game Color Token Parser & Live Preview Component
export const GtaColorPreview: React.FC<{ text: string; className?: string }> = ({ text, className }) => {
  const elements = useMemo(() => {
    if (!text) return [<span key="empty" className="text-slate-500 italic">Texte vide</span>];

    const tokens = text.split(/(~[A-Za-z0-9_]+~)/g);
    let currentColor = "text-white";
    let isBold = false;

    return tokens.map((part, index) => {
      if (!part) return null;

      if (part.startsWith("~") && part.endsWith("~")) {
        const code = part.slice(1, -1).toUpperCase();
        switch (code) {
          case "R":
          case "HUD_COLOUR_RED":
            currentColor = "text-rose-400";
            return null;
          case "G":
          case "HUD_COLOUR_GREEN":
            currentColor = "text-emerald-400";
            return null;
          case "B":
          case "HUD_COLOUR_BLUE":
            currentColor = "text-sky-400";
            return null;
          case "Y":
          case "HUD_COLOUR_YELLOW":
            currentColor = "text-yellow-300";
            return null;
          case "P":
          case "HUD_COLOUR_PURPLE":
            currentColor = "text-purple-400";
            return null;
          case "O":
          case "HUD_COLOUR_ORANGE":
            currentColor = "text-orange-400";
            return null;
          case "C":
            currentColor = "text-slate-400";
            return null;
          case "M":
            currentColor = "text-slate-600";
            return null;
          case "U":
            currentColor = "text-zinc-900";
            return null;
          case "W":
          case "S":
          case "HUD_COLOUR_WHITE":
            currentColor = "text-white";
            return null;
          case "H":
            isBold = !isBold;
            return null;
          case "N":
            return <br key={index} />;
          default:
            return (
              <span key={index} className="text-[#FF7A29]/70 text-[10px] font-mono px-0.5">
                {part}
              </span>
            );
        }
      }

      return (
        <span
          key={index}
          className={cn(currentColor, isBold ? "font-bold" : "font-normal", "transition-colors")}
        >
          {part}
        </span>
      );
    });
  }, [text]);

  return (
    <div className={cn("leading-relaxed font-sans select-text break-words", className)}>
      {elements}
    </div>
  );
};

const DEFAULT_SAMPLE_ENTRIES: Gxt2Entry[] = [
  {
    hash: 0x867b4512,
    hexHash: "0x867B4512",
    resolvedKey: "VEH_TURISMO_R",
    text: "~r~Grotti ~s~Turismo R ~g~(Sportive Custom)"
  },
  {
    hash: 0xa91c243f,
    hexHash: "0xA91C243F",
    resolvedKey: "MISSION_ALERT",
    text: "~HUD_COLOUR_YELLOW~ALERTE : ~s~Cible repérée près de ~HUD_COLOUR_ORANGE~Legion Square~s~ !"
  },
  {
    hash: 0x5142abcd,
    hexHash: "0x5142ABCD",
    resolvedKey: "WEAPON_HEAVY_RIFLE",
    text: "Fusil lourd ~h~MK II~h~ calibré pour ~r~munitions perforantes~s~."
  }
];

export const Gxt2Studio: React.FC<Gxt2StudioProps> = ({ currentRpf, onNavigateToExplorer }) => {
  const toast = useToast();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // Table State
  const [currentFileName, setCurrentFileName] = useState<string>("custom_text.gxt2");
  const [entries, setEntries] = useState<Gxt2Entry[]>(DEFAULT_SAMPLE_ENTRIES);
  const [selectedEntryIndex, setSelectedEntryIndex] = useState<number>(0);

  // Search State
  const [searchQuery, setSearchQuery] = useState<string>("");
  const [searchScope, setSearchScope] = useState<"table" | "global" | "rpf">("table");
  const [searchResults, setSearchResults] = useState<Gxt2SearchResult[]>([]);
  const [isSearching, setIsSearching] = useState<boolean>(false);

  // Import Modal State
  const [isImportModalOpen, setIsImportModalOpen] = useState<boolean>(false);
  const [importRawText, setImportRawText] = useState<string>("");

  // Filtered entries in current table
  const filteredTableEntries = useMemo(() => {
    if (searchScope !== "table" || !searchQuery.trim()) return entries;
    const q = searchQuery.toLowerCase();
    return entries.filter(
      (e) =>
        e.text.toLowerCase().includes(q) ||
        e.hexHash.toLowerCase().includes(q) ||
        (e.resolvedKey && e.resolvedKey.toLowerCase().includes(q))
    );
  }, [entries, searchQuery, searchScope]);

  // Selected Entry
  const selectedEntry = entries[selectedEntryIndex] ?? null;

  // Real-time backend search for global or RPF
  const handlePerformSearch = async () => {
    if (!searchQuery.trim()) {
      setSearchResults([]);
      return;
    }

    if (searchScope === "table") return;

    setIsSearching(true);
    try {
      if (searchScope === "global") {
        const results = await api.searchText(searchQuery.trim(), 100);
        setSearchResults(results);
      } else if (searchScope === "rpf" && currentRpf) {
        const results = await api.searchText(searchQuery.trim(), 100, currentRpf.filePath);
        setSearchResults(results);
      }
    } catch (err: any) {
      toast.error("Erreur de recherche", err.message);
    } finally {
      setIsSearching(false);
    }
  };

  // Add new blank entry
  const handleAddNewEntry = () => {
    const randomHash = Math.floor(Math.random() * 0xffffffff);
    const hex = "0x" + randomHash.toString(16).toUpperCase().padStart(8, "0");
    const newEntry: Gxt2Entry = {
      hash: randomHash,
      hexHash: hex,
      resolvedKey: `NEW_STRING_${entries.length + 1}`,
      text: "Nouveau texte de sous-titre GTA V"
    };
    setEntries((prev) => [newEntry, ...prev]);
    setSelectedEntryIndex(0);
    toast.info("Entrée créée", `Ajoutée avec le hash ${hex}`);
  };

  // Delete current entry
  const handleDeleteSelected = () => {
    if (entries.length === 0) return;
    setEntries((prev) => prev.filter((_, idx) => idx !== selectedEntryIndex));
    setSelectedEntryIndex((prev) => Math.max(0, prev - 1));
    toast.warning("Entrée supprimée");
  };

  // Update entry text or key
  const handleUpdateEntry = (field: "text" | "resolvedKey" | "hexHash", value: string) => {
    if (!selectedEntry) return;

    setEntries((prev) => {
      const copy = [...prev];
      const target = { ...copy[selectedEntryIndex] };

      if (field === "text") {
        target.text = value;
      } else if (field === "resolvedKey") {
        target.resolvedKey = value.toUpperCase().replace(/[^A-Z0-9_]/g, "");
      } else if (field === "hexHash") {
        let cleanHex = value.trim();
        if (!cleanHex.startsWith("0x") && !cleanHex.startsWith("0X")) {
          cleanHex = "0x" + cleanHex;
        }
        target.hexHash = cleanHex;
        const parsed = parseInt(cleanHex, 16);
        if (!isNaN(parsed)) {
          target.hash = parsed;
        }
      }

      copy[selectedEntryIndex] = target;
      return copy;
    });
  };

  // Insert color code into active entry text
  const insertToken = (token: string) => {
    if (!selectedEntry) return;
    handleUpdateEntry("text", selectedEntry.text + token);
  };

  // Add from search results into working table
  const handleAddSearchResultToTable = (item: Gxt2SearchResult) => {
    const existing = entries.find((e) => e.hash === item.hash);
    if (existing) {
      toast.warning("Déjà présent", `L'entrée ${item.hexHash} existe déjà dans votre table.`);
      return;
    }

    const newEntry: Gxt2Entry = {
      hash: item.hash,
      hexHash: item.hexHash,
      resolvedKey: item.resolvedKey,
      text: item.text
    };

    setEntries((prev) => [newEntry, ...prev]);
    setSelectedEntryIndex(0);
    toast.success("Chaîne importée", `Ajout de ${item.hexHash} à la table.`);
  };

  // Export to compiled .gxt2 binary
  const handleExportBinary = async () => {
    try {
      const textRepresentation = entries.map((e) => `${e.hexHash} = ${e.text}`).join("\n");
      const blob = await api.buildGxt2Binary(textRepresentation, currentFileName);
      triggerFileDownload(blob, currentFileName.endsWith(".gxt2") ? currentFileName : `${currentFileName}.gxt2`);
      toast.success("Compilation réussie", `${entries.length} chaînes compilées en binaire GXT2.`);
    } catch (err: any) {
      toast.error("Échec de la compilation", err.message);
    }
  };

  // Export to text
  const handleExportText = () => {
    const txt = entries.map((e) => `${e.hexHash} = ${e.text}`).join("\n");
    const blob = new Blob([txt], { type: "text/plain;charset=utf-8" });
    triggerFileDownload(blob, `${currentFileName.replace(/\.gxt2$/i, "")}.txt`);
    toast.success("Export Texte terminé", "Fichier .txt téléchargé.");
  };

  // Export to JSON
  const handleExportJson = () => {
    const json = JSON.stringify(entries, null, 2);
    const blob = new Blob([json], { type: "application/json;charset=utf-8" });
    triggerFileDownload(blob, `${currentFileName.replace(/\.gxt2$/i, "")}.json`);
    toast.success("Export JSON terminé", "Fichier .json téléchargé.");
  };

  // Import from GXT2 file upload
  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      const reader = new FileReader();
      reader.onload = async () => {
        try {
          const base64 = (reader.result as string).split(",")[1];
          const table = await api.parseGxt2({ base64Data: base64, fileName: file.name });
          setEntries(table.entries);
          setCurrentFileName(file.name);
          setSelectedEntryIndex(0);
          toast.success("Archive GXT2 chargée", `${table.entryCount} chaînes analysées avec succès.`);
        } catch (innerErr: any) {
          toast.error("Erreur d'analyse GXT2", innerErr.message);
        }
      };
      reader.readAsDataURL(file);
    } catch (err: any) {
      toast.error("Erreur de lecture", err.message);
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  // Parse raw text or JSON input
  const handleApplyImportText = () => {
    if (!importRawText.trim()) return;

    try {
      // 1. Try parsing JSON
      if (importRawText.trim().startsWith("[") || importRawText.trim().startsWith("{")) {
        const parsed = JSON.parse(importRawText);
        const list: Gxt2Entry[] = Array.isArray(parsed) ? parsed : [parsed];
        const sanitized = list.map((item, idx) => ({
          hash: item.hash || idx,
          hexHash: item.hexHash || `0x${(item.hash || idx).toString(16).toUpperCase()}`,
          resolvedKey: item.resolvedKey || null,
          text: item.text || String(item)
        }));
        setEntries(sanitized);
        setIsImportModalOpen(false);
        toast.success("Importation JSON réussie", `${sanitized.length} entrées appliquées.`);
        return;
      }

      // 2. Parse KEY = VALUE or 0xHASH = VALUE lines
      const lines = importRawText.split(/\r?\n/);
      const newItems: Gxt2Entry[] = [];

      lines.forEach((line, idx) => {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith("//") || trimmed.startsWith("#")) return;

        const sepIdx = trimmed.indexOf("=") > 0 ? trimmed.indexOf("=") : trimmed.indexOf(":");
        if (sepIdx > 0) {
          const keyPart = trimmed.slice(0, sepIdx).trim();
          const valPart = trimmed.slice(sepIdx + 1).trim();
          let hash = idx;
          let hex = "0x" + idx.toString(16).padStart(8, "0");

          if (keyPart.startsWith("0x") || keyPart.startsWith("0X")) {
            hex = keyPart;
            hash = parseInt(keyPart, 16) || idx;
          }

          newItems.push({
            hash,
            hexHash: hex,
            resolvedKey: keyPart.startsWith("0x") ? null : keyPart,
            text: valPart
          });
        }
      });

      if (newItems.length > 0) {
        setEntries(newItems);
        setSelectedEntryIndex(0);
        setIsImportModalOpen(false);
        toast.success("Importation Texte réussie", `${newItems.length} entrées ajoutées.`);
      } else {
        toast.warning("Aucune entrée détectée", "Format attendu : CLÉ = VALEUR ou 0x12345678 = VALEUR");
      }
    } catch (err: any) {
      toast.error("Erreur de parsing", err.message);
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden select-none">
      {/* Hidden File Input */}
      <input
        ref={fileInputRef}
        type="file"
        accept=".gxt2,.bin"
        className="hidden"
        onChange={handleFileUpload}
      />

      {/* Top Header Bar */}
      <header className="p-4 border-b border-[#222D42] bg-[#121824]/90 backdrop-blur-md flex flex-wrap items-center justify-between gap-4 shrink-0">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
            <Type className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-bold text-white tracking-wide">
                GXT2 & In-Game Text Studio
              </h1>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                {entries.length} entrées
              </span>
            </div>
            <p className="text-xs text-[#94A3B8] font-mono">
              Fichier actif : <span className="text-white font-semibold">{currentFileName}</span>
            </p>
          </div>
        </div>

        {/* Global Toolbar Actions */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => fileInputRef.current?.click()}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-[#94A3B8] hover:text-white transition-all cursor-pointer"
          >
            <Upload className="w-3.5 h-3.5" />
            <span>Ouvrir .gxt2</span>
          </button>

          <button
            onClick={() => setIsImportModalOpen(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-[#94A3B8] hover:text-white transition-all cursor-pointer"
          >
            <FileCode className="w-3.5 h-3.5" />
            <span>Importer Texte / JSON</span>
          </button>

          <button
            onClick={handleExportText}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-[#94A3B8] hover:text-white transition-all cursor-pointer"
            title="Exporter la liste au format texte brut (Clé = Valeur)"
          >
            <FileText className="w-3.5 h-3.5" />
            <span>Export TXT</span>
          </button>

          <button
            onClick={handleExportJson}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-[#94A3B8] hover:text-white transition-all cursor-pointer"
            title="Exporter au format JSON"
          >
            <Copy className="w-3.5 h-3.5" />
            <span>Export JSON</span>
          </button>

          <button
            onClick={handleExportBinary}
            className="flex items-center gap-1.5 px-4 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] active:scale-[0.98] text-white font-bold text-xs shadow-[0_0_15px_rgba(255,122,41,0.25)] transition-all cursor-pointer"
          >
            <Download className="w-3.5 h-3.5" />
            <span>Compiler en .GXT2</span>
          </button>
        </div>
      </header>

      {/* Main Split Layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left Column: List & Search Panel */}
        <aside className="w-80 sm:w-96 border-r border-[#222D42] bg-[#0E131D] flex flex-col overflow-hidden shrink-0">
          {/* Scope Selector & Search */}
          <div className="p-3 border-b border-[#222D42] space-y-2 bg-[#121824]/50">
            {/* Scope tabs */}
            <div className="grid grid-cols-3 gap-1 p-1 rounded-xl bg-[#0A0E16] border border-[#222D42]">
              <button
                onClick={() => setSearchScope("table")}
                className={cn(
                  "py-1 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer",
                  searchScope === "table"
                    ? "bg-[#FF7A29] text-white"
                    : "text-[#94A3B8] hover:text-white"
                )}
              >
                Table active
              </button>
              <button
                onClick={() => setSearchScope("global")}
                className={cn(
                  "py-1 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer",
                  searchScope === "global"
                    ? "bg-[#FF7A29] text-white"
                    : "text-[#94A3B8] hover:text-white"
                )}
              >
                Global GTA V
              </button>
              <button
                onClick={() => {
                  if (currentRpf) {
                    setSearchScope("rpf");
                  } else if (onNavigateToExplorer) {
                    onNavigateToExplorer();
                  }
                }}
                className={cn(
                  "py-1 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer",
                  searchScope === "rpf"
                    ? "bg-[#FF7A29] text-white"
                    : "text-[#94A3B8] hover:text-white"
                )}
                title={!currentRpf ? "Cliquez pour ouvrir une archive RPF dans l'Explorer" : undefined}
              >
                {currentRpf ? "Archive RPF" : "Lier RPF"}
              </button>
            </div>

            {/* Search Input */}
            <div className="flex gap-1.5">
              <div className="relative flex-1">
                <Search className="w-3.5 h-3.5 text-[#64748B] absolute left-3 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handlePerformSearch()}
                  placeholder={
                    searchScope === "table"
                      ? "Filtrer dans la table..."
                      : "Recherche texte ou 0xHash..."
                  }
                  className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl pl-8 pr-3 py-1.5 text-xs text-white placeholder-[#64748B] font-mono"
                />
              </div>
              {searchScope !== "table" && (
                <button
                  onClick={handlePerformSearch}
                  disabled={isSearching}
                  className="px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs font-semibold text-white transition-colors cursor-pointer disabled:opacity-50"
                >
                  {isSearching ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : "Chercher"}
                </button>
              )}
            </div>

            {searchScope === "table" && (
              <button
                onClick={handleAddNewEntry}
                className="w-full flex items-center justify-center gap-1.5 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] hover:border-[#FF7A29]/50 text-xs font-semibold text-white transition-colors cursor-pointer"
              >
                <Plus className="w-3.5 h-3.5 text-[#FF7A29]" />
                <span>Ajouter une nouvelle entrée</span>
              </button>
            )}
          </div>

          {/* List of Entries or Search Results */}
          <div className="flex-1 overflow-y-auto custom-scrollbar divide-y divide-[#222D42]/40">
            {searchScope === "table" ? (
              filteredTableEntries.length === 0 ? (
                <div className="p-8 text-center text-xs text-[#64748B] space-y-2">
                  <Type className="w-8 h-8 mx-auto opacity-40 text-[#FF7A29]" />
                  <p>Aucune entrée dans la table.</p>
                </div>
              ) : (
                filteredTableEntries.map((entry, idx) => {
                  const isSelected = selectedEntryIndex === idx;
                  return (
                    <div
                      key={idx}
                      onClick={() => setSelectedEntryIndex(idx)}
                      className={cn(
                        "p-3 transition-colors cursor-pointer space-y-1 select-none",
                        isSelected
                          ? "bg-[#FF7A29]/15 border-l-2 border-[#FF7A29]"
                          : "hover:bg-[#121824]/80 text-[#94A3B8]"
                      )}
                    >
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-mono text-[10px] text-[#FF7A29] font-bold">
                          {entry.hexHash}
                        </span>
                        {entry.resolvedKey && (
                          <span className="font-mono text-[10px] text-sky-400 truncate max-w-[120px]">
                            {entry.resolvedKey}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-white line-clamp-2 leading-relaxed">
                        {entry.text || <span className="text-[#64748B] italic">Sans texte</span>}
                      </p>
                    </div>
                  );
                })
              )
            ) : isSearching ? (
              <div className="p-8 text-center text-xs text-[#64748B] space-y-2">
                <RefreshCw className="w-6 h-6 animate-spin mx-auto text-[#FF7A29]" />
                <p>Recherche des chaînes GTA V...</p>
              </div>
            ) : searchResults.length === 0 ? (
              <div className="p-8 text-center text-xs text-[#64748B] space-y-2">
                <Search className="w-8 h-8 mx-auto opacity-40" />
                <p>Aucun résultat. Lancez une recherche ci-dessus.</p>
              </div>
            ) : (
              searchResults.map((item, idx) => (
                <div
                  key={idx}
                  className="p-3 hover:bg-[#121824] transition-colors space-y-1.5 group"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-mono text-[10px] text-[#FF7A29] font-bold">
                      {item.hexHash}
                    </span>
                    <button
                      onClick={() => handleAddSearchResultToTable(item)}
                      className="px-2 py-0.5 rounded bg-[#FF7A29]/20 hover:bg-[#FF7A29] text-[#FF7A29] hover:text-white font-mono text-[10px] font-bold transition-colors cursor-pointer flex items-center gap-1"
                    >
                      <Plus className="w-2.5 h-2.5" />
                      <span>Ajouter</span>
                    </button>
                  </div>
                  {item.resolvedKey && (
                    <div className="text-[10px] font-mono text-sky-400">{item.resolvedKey}</div>
                  )}
                  <p className="text-xs text-white leading-relaxed line-clamp-3">{item.text}</p>
                  <div className="text-[9px] font-mono text-[#64748B] truncate">
                    {item.entryPath}
                  </div>
                </div>
              ))
            )}
          </div>
        </aside>

        {/* Right Column: Active Entry Editor & In-Game Simulation */}
        <main className="flex-1 flex flex-col overflow-y-auto p-6 space-y-6 custom-scrollbar bg-[#0A0E16]">
          {selectedEntry ? (
            <div className="space-y-6 max-w-4xl">
              {/* Top metadata & controls */}
              <div className="flex flex-wrap items-center justify-between gap-4 p-4 rounded-2xl bg-[#121824] border border-[#222D42]">
                <div className="flex items-center gap-4">
                  <div>
                    <label className="text-[10px] font-mono text-[#64748B] uppercase block">
                      Hachage Hexadécimal
                    </label>
                    <input
                      type="text"
                      value={selectedEntry.hexHash}
                      onChange={(e) => handleUpdateEntry("hexHash", e.target.value)}
                      className="bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg px-3 py-1 font-mono text-xs text-[#FF7A29] font-bold mt-1"
                    />
                  </div>

                  <div>
                    <label className="text-[10px] font-mono text-[#64748B] uppercase block">
                      Clé / Label GTA V
                    </label>
                    <input
                      type="text"
                      value={selectedEntry.resolvedKey ?? ""}
                      placeholder="VEH_LABEL, MISSION_TITLE..."
                      onChange={(e) => handleUpdateEntry("resolvedKey", e.target.value)}
                      className="bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg px-3 py-1 font-mono text-xs text-sky-400 mt-1"
                    />
                  </div>

                  <div>
                    <label className="text-[10px] font-mono text-[#64748B] uppercase block">
                      Valeur Uint (32-bit)
                    </label>
                    <span className="font-mono text-xs text-white block mt-2">
                      {selectedEntry.hash}
                    </span>
                  </div>
                </div>

                <button
                  onClick={handleDeleteSelected}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-rose-950/40 hover:bg-rose-900/60 border border-rose-800/50 text-rose-300 text-xs font-semibold transition-colors cursor-pointer"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Supprimer l'entrée</span>
                </button>
              </div>

              {/* Text Entry & Quick Palette */}
              <div className="space-y-3 p-5 rounded-2xl bg-[#121824] border border-[#222D42]">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold text-white uppercase tracking-wider font-mono">
                    Contenu Textuel (avec Balises Rockstar Games)
                  </label>
                  <span className="text-[10px] font-mono text-[#64748B]">
                    {selectedEntry.text.length} caractères
                  </span>
                </div>

                {/* Quick Color Palette Tokens */}
                <div className="flex flex-wrap items-center gap-1.5 pt-1">
                  <span className="text-[10px] font-mono text-[#64748B] mr-1">Balises :</span>
                  <button
                    onClick={() => insertToken("~r~")}
                    className="px-2 py-0.5 rounded-md bg-rose-500/15 hover:bg-rose-500/30 text-rose-400 text-[10px] font-mono font-bold border border-rose-500/20 cursor-pointer"
                  >
                    ~r~ Rouge
                  </button>
                  <button
                    onClick={() => insertToken("~g~")}
                    className="px-2 py-0.5 rounded-md bg-emerald-500/15 hover:bg-emerald-500/30 text-emerald-400 text-[10px] font-mono font-bold border border-emerald-500/20 cursor-pointer"
                  >
                    ~g~ Vert
                  </button>
                  <button
                    onClick={() => insertToken("~b~")}
                    className="px-2 py-0.5 rounded-md bg-sky-500/15 hover:bg-sky-500/30 text-sky-400 text-[10px] font-mono font-bold border border-sky-500/20 cursor-pointer"
                  >
                    ~b~ Bleu
                  </button>
                  <button
                    onClick={() => insertToken("~y~")}
                    className="px-2 py-0.5 rounded-md bg-yellow-500/15 hover:bg-yellow-500/30 text-yellow-300 text-[10px] font-mono font-bold border border-yellow-500/20 cursor-pointer"
                  >
                    ~y~ Jaune
                  </button>
                  <button
                    onClick={() => insertToken("~o~")}
                    className="px-2 py-0.5 rounded-md bg-orange-500/15 hover:bg-orange-500/30 text-orange-400 text-[10px] font-mono font-bold border border-orange-500/20 cursor-pointer"
                  >
                    ~o~ Orange
                  </button>
                  <button
                    onClick={() => insertToken("~p~")}
                    className="px-2 py-0.5 rounded-md bg-purple-500/15 hover:bg-purple-500/30 text-purple-400 text-[10px] font-mono font-bold border border-purple-500/20 cursor-pointer"
                  >
                    ~p~ Violet
                  </button>
                  <button
                    onClick={() => insertToken("~w~")}
                    className="px-2 py-0.5 rounded-md bg-white/10 hover:bg-white/20 text-white text-[10px] font-mono font-bold border border-white/20 cursor-pointer"
                  >
                    ~w~ Blanc
                  </button>
                  <button
                    onClick={() => insertToken("~s~")}
                    className="px-2 py-0.5 rounded-md bg-slate-500/15 hover:bg-slate-500/30 text-slate-300 text-[10px] font-mono font-bold border border-slate-500/20 cursor-pointer"
                  >
                    ~s~ Reset
                  </button>
                  <button
                    onClick={() => insertToken("~h~")}
                    className="px-2 py-0.5 rounded-md bg-amber-500/15 hover:bg-amber-500/30 text-amber-300 text-[10px] font-mono font-bold border border-amber-500/20 cursor-pointer"
                  >
                    ~h~ Gras
                  </button>
                  <button
                    onClick={() => insertToken("~n~")}
                    className="px-2 py-0.5 rounded-md bg-[#FF7A29]/15 hover:bg-[#FF7A29]/30 text-[#FF7A29] text-[10px] font-mono font-bold border border-[#FF7A29]/20 cursor-pointer"
                  >
                    ~n~ Ligne
                  </button>
                </div>

                <textarea
                  rows={4}
                  value={selectedEntry.text}
                  onChange={(e) => handleUpdateEntry("text", e.target.value)}
                  placeholder="Tapez le texte du jeu ici..."
                  className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl p-3 font-mono text-sm text-white placeholder-[#64748B] transition-colors leading-relaxed"
                />
              </div>

              {/* In-Game Live HUD Simulation Box */}
              <div className="space-y-3 p-5 rounded-2xl bg-gradient-to-b from-[#121824] to-[#0A0E16] border border-[#FF7A29]/30 shadow-xl relative overflow-hidden">
                <div className="absolute top-0 right-0 w-48 h-48 bg-[#FF7A29]/10 rounded-full blur-2xl pointer-events-none -mr-10 -mt-10" />

                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Sparkles className="w-4 h-4 text-[#FF7A29]" />
                    <h3 className="text-xs font-bold text-white uppercase tracking-wider font-mono">
                      Simulation du Rendu En Jeu (HUD & Sous-Titres)
                    </h3>
                  </div>
                  <span className="text-[10px] font-mono text-emerald-400 font-semibold">
                    100% Fidèle GTA V
                  </span>
                </div>

                {/* Subtitle Bar Simulation */}
                <div className="p-6 rounded-xl bg-black/80 border border-[#222D42]/80 backdrop-blur-md text-center shadow-inner min-h-[90px] flex items-center justify-center">
                  <GtaColorPreview
                    text={selectedEntry.text}
                    className="text-sm font-semibold tracking-wide text-shadow drop-shadow-[0_2px_4px_rgba(0,0,0,0.9)]"
                  />
                </div>

                {/* In-Game Help Notification Style */}
                <div className="p-4 rounded-xl bg-black/90 border-l-4 border-[#FF7A29] shadow-lg max-w-md">
                  <div className="text-[10px] font-mono text-[#FF7A29] uppercase font-bold mb-1">
                    Notification Radar / Notification Jeu
                  </div>
                  <GtaColorPreview text={selectedEntry.text} className="text-xs" />
                </div>
              </div>
            </div>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center text-center p-8 text-[#64748B] space-y-3">
              <Type className="w-12 h-12 text-[#FF7A29] opacity-40" />
              <h3 className="text-base font-bold text-white">Aucune entrée sélectionnée</h3>
              <p className="text-xs max-w-sm">
                Sélectionnez une entrée dans le panneau latéral ou créez-en une nouvelle pour commencer l'édition.
              </p>
              <button
                onClick={handleAddNewEntry}
                className="px-4 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold text-xs shadow-lg transition-colors cursor-pointer"
              >
                Créer une entrée
              </button>
            </div>
          )}
        </main>
      </div>

      {/* Raw Text / JSON Import Modal */}
      {isImportModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4 animate-in fade-in">
          <div className="w-full max-w-2xl rounded-2xl bg-[#121824] border border-[#222D42] shadow-2xl p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FileCode className="w-5 h-5 text-[#FF7A29]" />
                <h3 className="text-base font-bold text-white">Importer du Texte ou JSON</h3>
              </div>
              <button
                onClick={() => setIsImportModalOpen(false)}
                className="text-[#64748B] hover:text-white p-1"
              >
                ✕
              </button>
            </div>

            <p className="text-xs text-[#94A3B8] leading-relaxed font-mono">
              Collez vos chaînes au format classique GTA V (<code className="text-[#FF7A29]">0x12345678 = Mon Texte</code> ou <code className="text-[#FF7A29]">MON_LABEL = Mon Texte</code>) ou au format JSON.
            </p>

            <textarea
              rows={10}
              value={importRawText}
              onChange={(e) => setImportRawText(e.target.value)}
              placeholder="0x1234ABCD = Première ligne de texte&#10;0x87654321 = Deuxième ligne avec ~r~couleur~s~"
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl p-3 font-mono text-xs text-white placeholder-[#64748B] custom-scrollbar leading-relaxed"
            />

            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                onClick={() => setIsImportModalOpen(false)}
                className="px-4 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] text-xs font-semibold text-[#94A3B8] hover:text-white transition-colors cursor-pointer"
              >
                Annuler
              </button>
              <button
                onClick={handleApplyImportText}
                className="px-5 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold shadow-lg transition-colors cursor-pointer"
              >
                Appliquer l'importation
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
