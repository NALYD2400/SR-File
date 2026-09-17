import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api/client";
import { ModPackageInfo, OivManifestInfo } from "../../types";
import { formatBytes, cn } from "../../lib/utils";
import {
  Plus,
  Trash2,
  AlertTriangle,
  FileCode,
  Package,
  Search,
  Clock,
  X,
  RefreshCw,
} from "lucide-react";

export const ModManager: React.FC = () => {
  const queryClient = useQueryClient();
  const [filterType, setFilterType] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedMod, setSelectedMod] = useState<ModPackageInfo | null>(null);
  const [inspectModal, setInspectModal] = useState<OivManifestInfo | null>(null);
  const [inspectingFilePath, setInspectingFilePath] = useState<string | null>(null);
  const [isInspecting, setIsInspecting] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Queries
  const { data: mods = [], isLoading: isModsLoading, refetch: refetchMods } = useQuery({
    queryKey: ["mods-list"],
    queryFn: () => api.getMods(),
    refetchInterval: 5000,
  });

  const { data: queue = [], refetch: refetchQueue } = useQuery({
    queryKey: ["mods-queue"],
    queryFn: () => api.getModQueue(),
    refetchInterval: 2000,
  });

  // Mutations
  const toggleMutation = useMutation({
    mutationFn: ({ modId, enabled }: { modId: string; enabled: boolean }) =>
      api.toggleMod(modId, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mods-list"] });
    },
    onError: (err: any) => setErrorMsg(err.message),
  });

  const uninstallMutation = useMutation({
    mutationFn: (modId: string) => api.uninstallMod(modId),
    onSuccess: () => {
      setSelectedMod(null);
      queryClient.invalidateQueries({ queryKey: ["mods-list"] });
    },
    onError: (err: any) => setErrorMsg(err.message),
  });

  const installMutation = useMutation({
    mutationFn: (filePath: string) => api.installMod(filePath),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["mods-queue"] });
      queryClient.invalidateQueries({ queryKey: ["mods-list"] });
    },
    onError: (err: any) => setErrorMsg(err.message),
  });

  const handleInstallFile = async () => {
    try {
      setErrorMsg(null);
      const selected = await api.selectFileNative(
        "Sélectionner un package de mod GTA V",
        "Packages de Mods (*.oiv; *.rpf; *.asi)",
        ["oiv", "rpf", "asi"]
      );
      if (selected) {
        if (selected.toLowerCase().endsWith(".oiv")) {
          // Pre-inspect OIV
          setIsInspecting(true);
          try {
            const manifest = await api.inspectOiv(selected);
            setInspectingFilePath(selected);
            setInspectModal(manifest);
          } catch {
            // If inspection fails, still queue install
            await installMutation.mutateAsync(selected);
          } finally {
            setIsInspecting(false);
          }
        } else {
          await installMutation.mutateAsync(selected);
        }
      }
    } catch (err: any) {
      setErrorMsg(err.message);
    }
  };

  const handleInspectDirectly = async () => {
    try {
      setErrorMsg(null);
      const selected = await api.selectFileNative(
        "Sélectionner une archive OIV à inspecter",
        "OpenIV Package (*.oiv)",
        ["oiv"]
      );
      if (selected) {
        setIsInspecting(true);
        const manifest = await api.inspectOiv(selected);
        setInspectModal(manifest);
      }
    } catch (err: any) {
      setErrorMsg(err.message);
    } finally {
      setIsInspecting(false);
    }
  };

  // Filtered mods
  const filteredMods = mods.filter((mod) => {
    const matchesFilter = filterType === "all" || mod.type.toLowerCase() === filterType.toLowerCase();
    const matchesSearch =
      mod.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      mod.author.toLowerCase().includes(searchQuery.toLowerCase()) ||
      mod.description.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesFilter && matchesSearch;
  });

  const totalConflicts = mods.reduce((acc, m) => acc + m.conflicts.length, 0);

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Top Header & Actions Bar */}
      <div className="p-5 border-b border-[#222D42] bg-[#121824]/60 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
              GTA V MODS ENGINE
            </span>
            <span className="text-xs text-[#64748B] font-mono">GESTIONNAIRE D'ARCHIVES & PACKAGES</span>
          </div>
          <h1 className="text-xl font-bold text-white mt-1">Gestionnaire de Mods & Conflits</h1>
          <p className="text-xs text-[#94A3B8]">
            Détection automatique de collisions, aperçu de manifestes OIV et bascule instantanée d'activation.
          </p>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          <button
            onClick={handleInspectDirectly}
            disabled={isInspecting}
            className="flex items-center gap-2 bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white px-3.5 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer"
            title="Inspecter le fichier assembly.xml d'un package .oiv sans l'installer"
          >
            <FileCode className="w-4 h-4 text-sky-400" />
            <span>{isInspecting ? "Inspection..." : "Inspecter OIV"}</span>
          </button>

          <button
            onClick={handleInstallFile}
            className="flex items-center gap-2 bg-[#FF7A29] hover:bg-[#FF8F4D] text-white px-4 py-2 rounded-xl text-xs font-bold shadow-[0_0_15px_rgba(255,122,41,0.25)] transition-all cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            <span>Installer un Mod (.oiv, .rpf, .asi)</span>
          </button>

          <button
            onClick={() => {
              refetchMods();
              refetchQueue();
            }}
            className="p-2 bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] rounded-xl text-[#94A3B8] hover:text-white transition-all cursor-pointer"
            title="Rafraîchir"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Global Alerts & Conflict Banner */}
      {errorMsg && (
        <div className="mx-6 mt-4 p-3.5 rounded-xl bg-red-950/40 border border-red-800/60 text-red-300 text-xs flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <AlertTriangle className="w-4 h-4 text-red-400 shrink-0" />
            <span>{errorMsg}</span>
          </div>
          <button onClick={() => setErrorMsg(null)} className="text-red-400 hover:text-red-200">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {totalConflicts > 0 && (
        <div className="mx-6 mt-4 p-3.5 rounded-xl bg-amber-950/30 border border-amber-800/50 text-amber-300 text-xs flex items-center gap-3">
          <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0" />
          <div>
            <span className="font-bold">Attention : </span>
            <span>
              {totalConflicts} collision(s) de fichiers détectée(s) entre vos mods actifs. Désactivez l'un des mods en conflit pour prévenir les crashs du jeu.
            </span>
          </div>
        </div>
      )}

      {/* Installation Queue Bar (if any jobs) */}
      {queue.some((q) => q.status === "Queued" || q.status === "Installing") && (
        <div className="mx-6 mt-4 p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="flex items-center justify-between text-xs">
            <div className="flex items-center gap-2 font-semibold text-white">
              <Clock className="w-4 h-4 text-[#FF7A29] animate-spin" />
              <span>File d'attente d'installation en cours</span>
            </div>
            <span className="text-[11px] font-mono text-[#94A3B8]">
              {queue.filter((q) => q.status === "Installing").length} actif / {queue.length} total
            </span>
          </div>
          <div className="space-y-2">
            {queue.map((item) => (
              <div key={item.id} className="p-2 rounded-lg bg-[#0A0E16]/60 border border-[#222D42]/40 text-xs space-y-1">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-white">{item.packageName}</span>
                  <span
                    className={cn(
                      "text-[10px] font-mono px-2 py-0.5 rounded",
                      item.status === "Completed" && "bg-emerald-500/10 text-emerald-400",
                      item.status === "Installing" && "bg-[#FF7A29]/15 text-[#FF7A29]",
                      item.status === "Failed" && "bg-red-500/10 text-red-400",
                      item.status === "Queued" && "bg-slate-700/30 text-slate-400"
                    )}
                  >
                    {item.status}
                  </span>
                </div>
                <div className="w-full bg-[#1A2234] rounded-full h-1.5 overflow-hidden">
                  <div
                    className="bg-[#FF7A29] h-full transition-all duration-300"
                    style={{ width: `${Math.round(item.progress * 100)}%` }}
                  />
                </div>
                <div className="text-[10px] text-[#94A3B8] font-mono">{item.message}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Main Split Layout: Mod List (Left) & Inspector/Details (Right) */}
      <div className="flex-1 flex overflow-hidden p-6 gap-6">
        {/* Left Side: Filter bar + Mod Cards List */}
        <div className="flex-1 flex flex-col space-y-4 overflow-hidden">
          {/* Search and Filters */}
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="relative flex-1 min-w-[200px]">
              <Search className="w-4 h-4 text-[#64748B] absolute left-3 top-3" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Rechercher un mod par nom, auteur..."
                className="w-full bg-[#121824] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl pl-9 pr-4 py-2 text-xs text-white placeholder-[#64748B] transition-colors"
              />
            </div>

            <div className="flex items-center gap-1 bg-[#121824] p-1 rounded-xl border border-[#222D42] text-xs">
              {["all", "oiv", "dlc", "asi", "loose"].map((type) => (
                <button
                  key={type}
                  onClick={() => setFilterType(type)}
                  className={cn(
                    "px-3 py-1.5 rounded-lg text-xs font-semibold capitalize transition-colors cursor-pointer",
                    filterType === type
                      ? "bg-[#FF7A29] text-white"
                      : "text-[#94A3B8] hover:text-white"
                  )}
                >
                  {type === "all" ? "Tous" : type.toUpperCase()}
                </button>
              ))}
            </div>
          </div>

          {/* Mods Virtual List */}
          <div className="flex-1 overflow-y-auto space-y-2.5 pr-2 custom-scrollbar">
            {isModsLoading ? (
              <div className="p-12 text-center text-[#64748B] text-xs">Chargement des mods installés...</div>
            ) : filteredMods.length === 0 ? (
              <div className="p-12 text-center border border-dashed border-[#222D42] rounded-2xl space-y-3">
                <Package className="w-8 h-8 text-[#64748B] mx-auto opacity-50" />
                <div className="text-sm font-semibold text-white">Aucun mod trouvé</div>
                <p className="text-xs text-[#94A3B8] max-w-sm mx-auto">
                  Déposez vos packages .oiv, conteneurs .rpf ou scripts .asi dans votre dossier mods ou cliquez sur Installer un Mod.
                </p>
                <button
                  onClick={handleInstallFile}
                  className="px-4 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold text-xs transition-colors cursor-pointer"
                >
                  Installer le premier mod
                </button>
              </div>
            ) : (
              filteredMods.map((mod) => {
                const isSelected = selectedMod?.id === mod.id;
                const hasConflicts = mod.conflicts.length > 0;

                return (
                  <div
                    key={mod.id}
                    onClick={() => setSelectedMod(mod)}
                    className={cn(
                      "p-4 rounded-xl border transition-all cursor-pointer flex items-center justify-between gap-4 group",
                      isSelected
                        ? "bg-[#172030] border-[#FF7A29] shadow-[0_0_15px_rgba(255,122,41,0.15)]"
                        : "bg-[#121824] border-[#222D42] hover:border-[#334360]"
                    )}
                  >
                    <div className="flex items-center gap-3.5 min-w-0">
                      <div
                        className={cn(
                          "w-10 h-10 rounded-xl flex items-center justify-center shrink-0 font-mono text-xs font-bold",
                          mod.type === "OIV" && "bg-sky-500/15 text-sky-400 border border-sky-500/30",
                          mod.type === "DLC" && "bg-purple-500/15 text-purple-400 border border-purple-500/30",
                          mod.type === "ASI" && "bg-amber-500/15 text-amber-400 border border-amber-500/30",
                          mod.type === "Loose" && "bg-slate-500/15 text-slate-300 border border-slate-500/30"
                        )}
                      >
                        {mod.type}
                      </div>

                      <div className="min-w-0 space-y-1">
                        <div className="flex items-center gap-2">
                          <h3 className="text-xs font-bold text-white truncate">{mod.name}</h3>
                          {hasConflicts && (
                            <span className="flex items-center gap-1 text-[10px] font-mono px-1.5 py-0.2 rounded bg-amber-500/10 text-amber-400 border border-amber-500/30">
                              <AlertTriangle className="w-3 h-3" />
                              <span>{mod.conflicts.length} conflit(s)</span>
                            </span>
                          )}
                        </div>
                        <div className="flex items-center gap-3 text-[11px] text-[#94A3B8] font-mono">
                          <span>v{mod.version}</span>
                          <span>•</span>
                          <span>{mod.author}</span>
                          <span>•</span>
                          <span>{mod.fileCount} fichiers</span>
                          <span>•</span>
                          <span>{formatBytes(mod.size)}</span>
                        </div>
                      </div>
                    </div>

                    {/* Enable/Disable Toggle Switch */}
                    <div className="flex items-center gap-3 shrink-0" onClick={(e) => e.stopPropagation()}>
                      <button
                        onClick={() =>
                          toggleMutation.mutate({ modId: mod.id, enabled: !mod.isEnabled })
                        }
                        className={cn(
                          "relative inline-flex h-5 w-10 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none",
                          mod.isEnabled ? "bg-emerald-500" : "bg-[#222D42]"
                        )}
                      >
                        <span
                          className={cn(
                            "pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out",
                            mod.isEnabled ? "translate-x-5" : "translate-x-0"
                          )}
                        />
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* Right Side: Selected Mod Details / Inspector Panel */}
        <div className="w-80 lg:w-96 flex flex-col bg-[#121824] border border-[#222D42] rounded-2xl overflow-hidden p-5 shrink-0">
          {selectedMod ? (
            <div className="flex-1 flex flex-col justify-between space-y-4 overflow-hidden">
              <div className="space-y-4 overflow-y-auto pr-1 custom-scrollbar">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <span
                      className={cn(
                        "text-[10px] font-mono px-2 py-0.5 rounded font-bold uppercase",
                        selectedMod.type === "OIV" && "bg-sky-500/15 text-sky-400",
                        selectedMod.type === "DLC" && "bg-purple-500/15 text-purple-400",
                        selectedMod.type === "ASI" && "bg-amber-500/15 text-amber-400",
                        selectedMod.type === "Loose" && "bg-slate-500/15 text-slate-300"
                      )}
                    >
                      {selectedMod.type} Package
                    </span>
                    <h2 className="text-base font-bold text-white">{selectedMod.name}</h2>
                    <p className="text-xs text-[#94A3B8] font-mono">Auteur: {selectedMod.author}</p>
                  </div>

                  <span
                    className={cn(
                      "text-[10px] font-mono px-2 py-0.5 rounded font-semibold",
                      selectedMod.isEnabled
                        ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20"
                        : "bg-red-500/10 text-red-400 border border-red-500/20"
                    )}
                  >
                    {selectedMod.isEnabled ? "Activé" : "Désactivé"}
                  </span>
                </div>

                <div className="p-3 rounded-xl bg-[#0A0E16]/60 border border-[#222D42]/60 text-xs space-y-2">
                  <div className="text-[10px] font-mono uppercase text-[#64748B]">Description</div>
                  <p className="text-xs text-[#94A3B8] leading-relaxed">
                    {selectedMod.description || "Aucune description fournie dans le manifeste."}
                  </p>
                </div>

                {/* Conflicting Files Section */}
                {selectedMod.conflicts.length > 0 && (
                  <div className="p-3.5 rounded-xl bg-amber-950/30 border border-amber-800/40 space-y-2">
                    <div className="flex items-center gap-1.5 text-xs font-bold text-amber-400">
                      <AlertTriangle className="w-3.5 h-3.5" />
                      <span>Fichiers en conflit ({selectedMod.conflicts.length})</span>
                    </div>
                    <div className="space-y-1 max-h-32 overflow-y-auto custom-scrollbar">
                      {selectedMod.conflicts.map((c, i) => (
                        <div key={i} className="text-[11px] font-mono text-amber-200/90 break-all">
                          • {c}
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Contained Files List */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-semibold text-white">Contenu du package</span>
                    <span className="text-[10px] font-mono text-[#64748B]">
                      {selectedMod.files.length} fichier(s)
                    </span>
                  </div>
                  <div className="max-h-48 overflow-y-auto rounded-xl bg-[#0A0E16] border border-[#222D42] p-2 space-y-1 font-mono text-[10px] text-[#94A3B8] custom-scrollbar">
                    {selectedMod.files.map((file, idx) => (
                      <div key={idx} className="truncate hover:text-white" title={file}>
                        {file}
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              {/* Bottom Action: Uninstall */}
              <div className="pt-4 border-t border-[#222D42]/60 flex items-center justify-between gap-2">
                <button
                  onClick={() => {
                    if (confirm(`Êtes-vous sûr de vouloir désinstaller définitivement le mod ${selectedMod.name} ?`)) {
                      uninstallMutation.mutate(selectedMod.id);
                    }
                  }}
                  className="flex items-center justify-center gap-2 bg-red-950/30 hover:bg-red-900/50 border border-red-800/50 text-red-300 hover:text-red-100 px-4 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer w-full"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Désinstaller le mod</span>
                </button>
              </div>
            </div>
          ) : (
            <div className="flex-1 flex flex-col items-center justify-center text-center p-6 space-y-3 opacity-60">
              <Package className="w-10 h-10 text-[#64748B]" />
              <div className="text-xs font-semibold text-white">Sélectionnez un mod</div>
              <p className="text-[11px] text-[#94A3B8]">
                Cliquez sur un mod dans la liste pour voir sa description, ses conflits de fichiers et ses options de gestion.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* OIV Manifest Preview Modal */}
      {inspectModal && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-6">
          <div className="bg-[#121824] border border-[#222D42] rounded-2xl w-full max-w-2xl max-h-[85vh] flex flex-col overflow-hidden shadow-2xl">
            <div className="p-5 border-b border-[#222D42] flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <FileCode className="w-5 h-5 text-sky-400" />
                <div>
                  <h3 className="text-sm font-bold text-white">Manifeste OIV — {inspectModal.name}</h3>
                  <p className="text-xs text-[#94A3B8] font-mono">
                    Auteur: {inspectModal.author} • v{inspectModal.version}
                  </p>
                </div>
              </div>
              <button
                onClick={() => setInspectModal(null)}
                className="p-1.5 rounded-lg text-[#94A3B8] hover:text-white hover:bg-[#1E283D] transition-colors cursor-pointer"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-5 space-y-4 custom-scrollbar">
              <div className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] text-xs space-y-1">
                <div className="text-[10px] font-mono uppercase text-[#64748B]">Description du package</div>
                <p className="text-[#94A3B8] leading-relaxed">{inspectModal.description}</p>
              </div>

              <div className="space-y-2">
                <div className="text-xs font-semibold text-white">
                  Archives et conteneurs cibles ({inspectModal.targetComponents.length})
                </div>
                <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1 font-mono text-[11px] text-sky-300">
                  {inspectModal.targetComponents.map((c, i) => (
                    <div key={i}>• {c}</div>
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <div className="text-xs font-semibold text-white">Extrait XML (assembly.xml)</div>
                <pre className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] font-mono text-[10px] text-emerald-400/90 overflow-x-auto max-h-48 custom-scrollbar">
                  {inspectModal.rawXml}
                </pre>
              </div>
            </div>

            <div className="p-4 border-t border-[#222D42] bg-[#0A0E16]/60 flex justify-end gap-2">
              <button
                onClick={() => {
                  setInspectModal(null);
                  setInspectingFilePath(null);
                }}
                className="px-4 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer"
              >
                Fermer
              </button>
              {inspectingFilePath && (
                <button
                  onClick={async () => {
                    if (inspectingFilePath) {
                      await installMutation.mutateAsync(inspectingFilePath);
                      setInspectModal(null);
                      setInspectingFilePath(null);
                    }
                  }}
                  className="flex items-center gap-2 px-4 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-all shadow-[0_0_15px_rgba(255,122,41,0.25)] cursor-pointer"
                >
                  <Plus className="w-4 h-4" />
                  <span>Installer ce package OIV</span>
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
