import React, { useState, useMemo } from "react";
import { api } from "../../api/client";
import { cn } from "../../lib/utils";
import {
  FileCode,
  Search,
  Replace,
  Split,
  Save,
  FolderOpen,
  Copy,
  Check,
  Sparkles,
} from "lucide-react";

interface CodeEditorProps {
  initialContent?: string;
  initialFileName?: string;
  onSave?: (content: string) => void;
}

export const CodeEditor: React.FC<CodeEditorProps> = ({
  initialContent,
  initialFileName = "content.xml",
  onSave,
}) => {
  const [content, setContent] = useState<string>(
    initialContent ||
      `<?xml version="1.0" encoding="utf-8"?>\n<CMapData>\n  <name>custom_ambient_lighting</name>\n  <flags value="0"/>\n  <contentFlags value="1"/>\n  <entitiesExtentsMin x="-1500.0" y="-1200.0" z="-50.0"/>\n  <entitiesExtentsMax x="1500.0" y="1200.0" z="400.0"/>\n  <entities>\n    <Item type="CEntityDef">\n      <archetypeName>prop_barrier_work05</archetypeName>\n      <flags value="32"/>\n      <guid value="101"/>\n      <position x="-1034.5" y="-2732.1" z="13.8"/>\n      <rotation x="0.0" y="0.0" z="0.7071" w="0.7071"/>\n      <scaleXY value="1.0"/>\n      <scaleZ value="1.0"/>\n      <lodDist value="150.0"/>\n      <childLodDist value="0.0"/>\n    </Item>\n  </entities>\n</CMapData>`
  );

  const [originalContent, setOriginalContent] = useState<string>(content);
  const [fileName, setFileName] = useState<string>(initialFileName);
  const [filePath, setFilePath] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [replaceQuery, setReplaceQuery] = useState("");
  const [showSearch, setShowSearch] = useState(false);
  const [diffMode, setDiffMode] = useState(false);
  const [copied, setCopied] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  // Line count & cursor info
  const lines = useMemo(() => content.split("\n"), [content]);
  const originalLines = useMemo(() => originalContent.split("\n"), [originalContent]);

  // Search match count
  const searchMatches = useMemo(() => {
    if (!searchQuery) return 0;
    try {
      const matches = content.match(new RegExp(searchQuery.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"), "gi"));
      return matches ? matches.length : 0;
    } catch {
      return 0;
    }
  }, [content, searchQuery]);

  const handleFormatXml = () => {
    try {
      // Basic pretty-print indentation for XML
      let formatted = "";
      let indent = 0;
      const parts = content.replace(/>\s*</g, "><").split(/(<[^>]+>)/g).filter(Boolean);

      for (let i = 0; i < parts.length; i++) {
        const part = parts[i].trim();
        if (!part) continue;

        if (part.startsWith("</")) {
          indent = Math.max(0, indent - 1);
        }

        const tabs = "  ".repeat(indent);
        formatted += (formatted ? "\n" : "") + tabs + part;

        if (
          part.startsWith("<") &&
          !part.startsWith("</") &&
          !part.endsWith("/>") &&
          !part.startsWith("<?") &&
          !part.startsWith("<!")
        ) {
          indent++;
        }
      }

      setContent(formatted);
      setStatusMessage("XML reformaté avec succès !");
      setTimeout(() => setStatusMessage(null), 3000);
    } catch {
      setStatusMessage("Erreur lors du formatage du document.");
      setTimeout(() => setStatusMessage(null), 3000);
    }
  };

  const handleReplaceAll = () => {
    if (!searchQuery) return;
    const regex = new RegExp(searchQuery.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"), "g");
    const newContent = content.replace(regex, replaceQuery);
    setContent(newContent);
    setStatusMessage("Remplacement effectué.");
    setTimeout(() => setStatusMessage(null), 2500);
  };

  const handleOpenFileNative = async () => {
    try {
      const selected = await api.selectFileNative(
        "Ouvrir un fichier de métadonnées ou de code",
        "Fichiers GTA V (*.xml; *.meta; *.ymt; *.ymap; *.ytyp; *.gxt2; *.txt; *.json)",
        ["xml", "meta", "ymt", "ymap", "ytyp", "gxt2", "txt", "json"]
      );
      if (selected) {
        try {
          const res = await api.readFileText(selected);
          setContent(res.content);
          setOriginalContent(res.content);
          setFilePath(selected);
          setFileName(selected.split(/[\\/]/).pop() || "fichier.xml");
          setStatusMessage(`Fichier chargé: ${selected}`);
          setTimeout(() => setStatusMessage(null), 3000);
        } catch (err: any) {
          setStatusMessage(`Erreur de lecture: ${err.message}`);
          setTimeout(() => setStatusMessage(null), 4000);
        }
      }
    } catch { }
  };

  const handleCopyContent = () => {
    navigator.clipboard.writeText(content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleSaveFile = async () => {
    if (filePath) {
      try {
        await api.saveFileText(filePath, content);
        setOriginalContent(content);
        setStatusMessage(`Enregistré sur le disque: ${filePath}`);
        setTimeout(() => setStatusMessage(null), 2500);
        if (onSave) onSave(content);
        return;
      } catch (err: any) {
        setStatusMessage(`Erreur d'écriture: ${err.message}`);
        setTimeout(() => setStatusMessage(null), 3500);
      }
    }

    if (onSave) {
      onSave(content);
    } else {
      // Download as file
      const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      link.click();
      URL.revokeObjectURL(url);
    }
    setOriginalContent(content);
    setStatusMessage("Fichier enregistré.");
    setTimeout(() => setStatusMessage(null), 2500);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      setFileName(file.name);
      setFilePath(null);
      const reader = new FileReader();
      reader.onload = (ev) => {
        const text = ev.target?.result as string;
        if (text) {
          setContent(text);
          setOriginalContent(text);
          setStatusMessage(`Fichier importé: ${file.name}`);
          setTimeout(() => setStatusMessage(null), 3000);
        }
      };
      reader.readAsText(file);
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Top Toolbar */}
      <div className="p-3 border-b border-[#222D42] bg-[#121824] flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-xs font-semibold text-white">
            <FileCode className="w-4 h-4 text-[#FF7A29]" />
            <span className="font-mono bg-[#0A0E16] px-2.5 py-1 rounded-lg border border-[#222D42]">
              {fileName}
            </span>
          </div>

          <span className="text-[11px] text-[#64748B] font-mono">
            {lines.length} lignes • {content.length} caractères
          </span>

          {content !== originalContent && (
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-amber-500/15 text-amber-400 border border-amber-500/30">
              Modifié
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleOpenFileNative}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
            title="Ouvrir un fichier"
          >
            <FolderOpen className="w-3.5 h-3.5 text-[#FF7A29]" />
            <span>Ouvrir</span>
          </button>

          <button
            onClick={() => setShowSearch(!showSearch)}
            className={cn(
              "flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors cursor-pointer border",
              showSearch
                ? "bg-[#FF7A29] text-white border-[#FF7A29]"
                : "bg-[#172030] hover:bg-[#1E283D] border-[#222D42] text-[#94A3B8] hover:text-white"
            )}
            title="Rechercher et remplacer"
          >
            <Search className="w-3.5 h-3.5" />
            <span>Rechercher</span>
          </button>

          <button
            onClick={handleFormatXml}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
            title="Indenter et formater le XML"
          >
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
            <span>Formater</span>
          </button>

          <button
            onClick={() => setDiffMode(!diffMode)}
            className={cn(
              "flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors cursor-pointer border",
              diffMode
                ? "bg-sky-500 text-white border-sky-500"
                : "bg-[#172030] hover:bg-[#1E283D] border-[#222D42] text-[#94A3B8] hover:text-white"
            )}
            title="Vue Différence / Comparaison"
          >
            <Split className="w-3.5 h-3.5" />
            <span>Diff</span>
          </button>

          <button
            onClick={handleCopyContent}
            className="p-1.5 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white transition-colors cursor-pointer"
            title="Copier le code"
          >
            {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
          </button>

          <button
            onClick={handleSaveFile}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-all shadow-[0_0_15px_rgba(255,122,41,0.25)] cursor-pointer"
          >
            <Save className="w-3.5 h-3.5" />
            <span>Enregistrer</span>
          </button>
        </div>
      </div>

      {/* Search and Replace Floating Sub-bar */}
      {showSearch && (
        <div className="p-3 border-b border-[#222D42] bg-[#0E1420] flex flex-wrap items-center gap-3 text-xs">
          <div className="relative flex-1 min-w-[200px]">
            <Search className="w-3.5 h-3.5 text-[#64748B] absolute left-3 top-2.5" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Rechercher..."
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg pl-8 pr-3 py-1.5 text-xs text-white font-mono"
            />
          </div>

          <div className="relative flex-1 min-w-[200px]">
            <Replace className="w-3.5 h-3.5 text-[#64748B] absolute left-3 top-2.5" />
            <input
              type="text"
              value={replaceQuery}
              onChange={(e) => setReplaceQuery(e.target.value)}
              placeholder="Remplacer par..."
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg pl-8 pr-3 py-1.5 text-xs text-white font-mono"
            />
          </div>

          <div className="flex items-center gap-2">
            <span className="font-mono text-[11px] text-[#94A3B8]">
              {searchMatches} correspondance(s)
            </span>
            <button
              onClick={handleReplaceAll}
              disabled={!searchQuery}
              className="px-3 py-1.5 rounded-lg bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer disabled:opacity-40"
            >
              Tout remplacer
            </button>
          </div>
        </div>
      )}

      {statusMessage && (
        <div className="bg-[#FF7A29]/15 border-b border-[#FF7A29]/30 text-[#FF7A29] text-xs px-4 py-1.5 font-mono">
          {statusMessage}
        </div>
      )}

      {/* Editor Body: Normal Mode or Diff Mode */}
      <div
        className="flex-1 flex overflow-hidden"
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleDrop}
      >
        {diffMode ? (
          <div className="flex-1 grid grid-cols-2 overflow-hidden">
            {/* Left: Original */}
            <div className="border-r border-[#222D42] flex flex-col overflow-hidden bg-[#0A0E16]">
              <div className="p-2 border-b border-[#222D42] bg-[#121824] text-[11px] font-mono text-[#94A3B8]">
                ORIGINAL (Avant modification)
              </div>
              <div className="flex-1 overflow-y-auto p-4 font-mono text-xs space-y-1 custom-scrollbar text-[#94A3B8] opacity-80 select-text">
                {originalLines.map((line, idx) => (
                  <div key={idx} className="flex gap-4">
                    <span className="w-10 text-right text-[#64748B] select-none shrink-0 font-mono text-[11px]">
                      {idx + 1}
                    </span>
                    <pre className="flex-1 font-mono whitespace-pre-wrap">{line}</pre>
                  </div>
                ))}
              </div>
            </div>

            {/* Right: Modified */}
            <div className="flex flex-col overflow-hidden bg-[#0A0E16]">
              <div className="p-2 border-b border-[#222D42] bg-[#121824] text-[11px] font-mono text-emerald-400">
                ACTUEL (Modifié)
              </div>
              <div className="flex-1 overflow-y-auto p-4 font-mono text-xs space-y-1 custom-scrollbar select-text">
                {lines.map((line, idx) => {
                  const isDiff = originalLines[idx] !== line;
                  return (
                    <div key={idx} className={cn("flex gap-4", isDiff && "bg-emerald-950/20")}>
                      <span className="w-10 text-right text-[#64748B] select-none shrink-0 font-mono text-[11px]">
                        {idx + 1}
                      </span>
                      <pre className={cn("flex-1 font-mono whitespace-pre-wrap", isDiff ? "text-emerald-300 font-bold" : "text-[#F1F5F9]")}>
                        {line}
                      </pre>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        ) : (
          <div className="flex-1 flex overflow-hidden bg-[#0A0E16]">
            {/* Line numbers gutter */}
            <div className="w-12 bg-[#0A0E16] border-r border-[#222D42]/60 py-4 select-none text-right pr-3 font-mono text-[11px] text-[#64748B] overflow-hidden shrink-0">
              {lines.map((_, i) => (
                <div key={i} className="leading-6">
                  {i + 1}
                </div>
              ))}
            </div>

            {/* Code TextArea */}
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              spellCheck={false}
              className="flex-1 bg-transparent p-4 font-mono text-xs text-[#F1F5F9] focus:outline-none resize-none leading-6 custom-scrollbar whitespace-pre"
            />
          </div>
        )}
      </div>

      {/* Footer Status Bar */}
      <div className="px-4 py-1.5 border-t border-[#222D42] bg-[#0E1420] flex items-center justify-between text-[11px] font-mono text-[#64748B]">
        <div className="flex items-center gap-4">
          <span>Format : XML / META / PSO</span>
          <span>Encodage : UTF-8</span>
        </div>
        <div className="flex items-center gap-4">
          <span>LF (Unix)</span>
          <span>{lines.length} Lignes</span>
        </div>
      </div>
    </div>
  );
};
