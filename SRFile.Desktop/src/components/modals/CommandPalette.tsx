import React, { useState, useEffect } from "react";
import { RpfInfo, RpfEntry, ActiveView } from "../../types";
import { api } from "../../api/client";
import { Search, FolderOpen, Sliders, Database, File, X, ArrowRight, Type } from "lucide-react";
import { formatBytes } from "../../lib/utils";

interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
  currentRpf?: RpfInfo | null;
  onSelectView: (view: ActiveView) => void;
  onOpenAnotherRpf: () => void;
  onOpenSettings: () => void;
  onSelectEntry?: (entry: RpfEntry) => void;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({
  isOpen,
  onClose,
  currentRpf,
  onSelectView,
  onOpenAnotherRpf,
  onOpenSettings,
}) => {
  const [query, setQuery] = useState("");
  const [searchResults, setSearchResults] = useState<RpfEntry[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      setQuery("");
      setSearchResults([]);
    }
  }, [isOpen]);

  // Debounced search inside current RPF
  useEffect(() => {
    if (!isOpen || !currentRpf || !query.trim() || query.length < 2) {
      setSearchResults([]);
      return;
    }

    const timer = setTimeout(async () => {
      setIsSearching(true);
      try {
        const results = await api.searchEntries(currentRpf.filePath, query.trim(), 20);
        setSearchResults(results);
      } catch (err) {
        console.error("Command palette search error:", err);
      } finally {
        setIsSearching(false);
      }
    }, 250);

    return () => clearTimeout(timer);
  }, [query, currentRpf, isOpen]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center pt-20 bg-black/75 backdrop-blur-sm p-4">
      <div className="w-full max-w-xl rounded-2xl border border-[#222D42] bg-[#121824] shadow-2xl overflow-hidden flex flex-col">
        {/* Search Bar */}
        <div className="p-3 border-b border-[#222D42] bg-[#151D2D] flex items-center gap-3">
          <Search className="w-4 h-4 text-[#FF7A29] shrink-0" />
          <input
            autoFocus
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={
              currentRpf
                ? `Rechercher dans ${currentRpf.name} ou tapez une action...`
                : "Tapez une commande ou ouvrez une archive..."
            }
            className="flex-1 bg-transparent border-none outline-none text-xs text-white placeholder-[#64748B] font-mono"
          />
          <button
            onClick={onClose}
            className="p-1 rounded text-[#64748B] hover:text-white"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Results / Commands List */}
        <div className="max-h-80 overflow-y-auto p-2 space-y-1 custom-scrollbar text-xs">
          {/* Default Quick Actions */}
          <div className="text-[10px] font-mono text-[#64748B] px-3 py-1 font-semibold uppercase">
            Actions Rapides
          </div>

          <div
            onClick={() => {
              onClose();
              onOpenAnotherRpf();
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <FolderOpen className="w-4 h-4 text-[#FF7A29]" />
              <span>Ouvrir une archive RPF</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onOpenSettings();
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Sliders className="w-4 h-4 text-[#FF7A29]" />
              <span>Configurer le dossier de jeu GTA V</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("explorer");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-[#FF7A29]" />
              <span>Accéder à l'explorateur RPF virtuel</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("gxt2_studio");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Type className="w-4 h-4 text-emerald-400" />
              <span>Studio de Textes & Sous-Titres GXT2</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("mods");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-emerald-400" />
              <span>Gestionnaire de Mods GTA V & Conflits</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("gen9");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-amber-400" />
              <span>Convertisseur Gen9 Next-Gen</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("project_editor");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-rose-400" />
              <span>Éditeur de Map & Projet (.cwproj)</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("code_editor");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-teal-400" />
              <span>Éditeur de Code XML & Meta</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("hex_viewer");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-indigo-400" />
              <span>Visionneuse Hexadécimale</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("jenkins");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-cyan-400" />
              <span>Calculateur Jenkins JOAAT 32-bit</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          <div
            onClick={() => {
              onClose();
              onSelectView("crypto");
            }}
            className="flex items-center justify-between p-2.5 rounded-xl hover:bg-[#172030] text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
          >
            <div className="flex items-center gap-2.5">
              <Database className="w-4 h-4 text-orange-400" />
              <span>Sécurité & Clés AES</span>
            </div>
            <ArrowRight className="w-3.5 h-3.5 text-[#64748B]" />
          </div>

          {/* Search results inside loaded RPF */}
          {searchResults.length > 0 && (
            <>
              <div className="text-[10px] font-mono text-[#64748B] px-3 py-1 mt-2 font-semibold uppercase border-t border-[#222D42]/60 pt-2">
                Fichiers trouvés ({searchResults.length})
              </div>
              {searchResults.map((entry) => (
                <div
                  key={entry.path}
                  onClick={() => {
                    onClose();
                    onSelectView("explorer");
                  }}
                  className="flex items-center justify-between p-2 rounded-lg hover:bg-[#172030] text-xs font-mono text-[#94A3B8] hover:text-white cursor-pointer transition-colors"
                >
                  <div className="flex items-center gap-2 truncate">
                    <File className="w-3.5 h-3.5 text-[#FF7A29] shrink-0" />
                    <span className="truncate text-white">{entry.name}</span>
                    <span className="text-[10px] text-[#64748B] truncate">({entry.path})</span>
                  </div>
                  <span className="text-[10px] text-[#64748B] shrink-0 font-mono">
                    {formatBytes(entry.size)}
                  </span>
                </div>
              ))}
            </>
          )}

          {isSearching && (
            <div className="text-center py-4 text-xs text-[#64748B]">
              Recherche dans l'archive en cours...
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-2.5 border-t border-[#222D42] bg-[#0A0E16] text-[10px] font-mono text-[#64748B] flex items-center justify-between">
          <span>Utilisez les flèches ou la souris pour naviguer</span>
          <kbd className="px-1.5 py-0.5 rounded bg-[#121824] border border-[#222D42]">ECHAP pour fermer</kbd>
        </div>
      </div>
    </div>
  );
};
