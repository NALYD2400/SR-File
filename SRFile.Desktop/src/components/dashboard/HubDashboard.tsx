import React, { useState } from "react";
import { AppStatus, RpfInfo, ActiveView } from "../../types";
import { api } from "../../api/client";
import { 
  FolderOpen, 
  Database, 
  Settings, 
  AlertTriangle, 
  ArrowRight,
  HardDrive,
  Cpu,
  KeyRound,
  FileCode,
  Layers,
  Image as ImageIcon,
  Music,
  ShieldCheck,
  Binary,
  Hash,
  Map
} from "lucide-react";
import { formatBytes } from "../../lib/utils";

interface HubDashboardProps {
  status?: AppStatus;
  currentRpf?: RpfInfo | null;
  onOpenRpf: (path: string) => Promise<void>;
  onNavigate: (view: ActiveView) => void;
  onOpenSettings: () => void;
}

export const HubDashboard: React.FC<HubDashboardProps> = ({
  status,
  currentRpf,
  onOpenRpf,
  onNavigate,
  onOpenSettings,
}) => {
  const [loadingPath, setLoadingPath] = useState<string | null>(null);
  const [manualPath, setManualPath] = useState("");
  const [error, setError] = useState<string | null>(null);

  const handleOpenNativeDialog = async () => {
    try {
      setError(null);
      const selected = await api.selectFileNative(
        "Sélectionnez une archive RPF",
        "Archives GTA V (*.rpf)",
        ["rpf"]
      );
      if (selected) {
        setLoadingPath(selected);
        await onOpenRpf(selected);
      }
    } catch (err: any) {
      setError(err.message || "Erreur lors de l'ouverture du fichier");
    } finally {
      setLoadingPath(null);
    }
  };

  const handleOpenManual = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!manualPath.trim()) return;
    try {
      setError(null);
      setLoadingPath(manualPath);
      await onOpenRpf(manualPath.trim());
      setManualPath("");
    } catch (err: any) {
      setError(err.message || "Erreur lors de l'ouverture du fichier");
    } finally {
      setLoadingPath(null);
    }
  };

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-[#0A0E16] text-[#F1F5F9] custom-scrollbar">
      {/* Welcome & Status Banner */}
      <div className="relative overflow-hidden rounded-2xl border border-[#222D42] bg-gradient-to-r from-[#121824] via-[#151D2D] to-[#121824] p-6 shadow-xl">
        <div className="absolute top-0 right-0 w-96 h-96 bg-[#FF7A29]/10 rounded-full blur-3xl pointer-events-none -mr-20 -mt-20" />
        
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
                ARCHITECTURE NOUVELLE GÉNÉRATION
              </span>
              <span className="text-xs text-[#64748B] font-mono">CODEWALKER HYBRID CORE</span>
            </div>
            <h1 className="text-2xl font-black tracking-tight text-white">
              SR File Suite — Hub de Recherche & Modding
            </h1>
            <p className="text-sm text-[#94A3B8] max-w-2xl">
              Exploration haute performance d'archives RPF, décompression instantanée de textures DDS vers PNG, lecture audio native AWC et streaming d'assets propulsés par C# .NET 8 et Tauri v2.
            </p>
          </div>

          <div className="flex flex-col gap-2 shrink-0">
            <button
              onClick={handleOpenNativeDialog}
              disabled={!!loadingPath}
              className="flex items-center justify-center gap-2 bg-[#FF7A29] hover:bg-[#FF8F4D] active:scale-[0.98] text-white px-5 py-2.5 rounded-xl font-bold text-sm shadow-[0_0_20px_rgba(255,122,41,0.3)] transition-all cursor-pointer disabled:opacity-50"
            >
              <FolderOpen className="w-4 h-4" />
              <span>{loadingPath ? "Chargement..." : "Ouvrir une archive RPF"}</span>
            </button>
            <button
              onClick={onOpenSettings}
              className="flex items-center justify-center gap-2 bg-[#121824] hover:bg-[#1A2234] border border-[#222D42] hover:border-[#334360] text-[#94A3B8] hover:text-white px-4 py-2 rounded-xl text-xs font-semibold transition-all cursor-pointer"
            >
              <Settings className="w-3.5 h-3.5" />
              <span>Configurer le dossier GTA V</span>
            </button>
          </div>
        </div>

        {/* Quick Diagnostic Pills */}
        <div className="relative z-10 grid grid-cols-2 sm:grid-cols-4 gap-3 mt-6 pt-6 border-t border-[#222D42]/60">
          <div className="flex items-center gap-2.5 p-2 rounded-lg bg-[#0A0E16]/60 border border-[#222D42]/40">
            <Cpu className="w-4 h-4 text-[#FF7A29]" />
            <div className="text-xs">
              <div className="text-[#64748B] text-[10px]">Sidecar Backend</div>
              <div className="font-mono font-semibold text-emerald-400">
                {status?.running ? "Actif (127.0.0.1:5890)" : "Hors-ligne"}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2.5 p-2 rounded-lg bg-[#0A0E16]/60 border border-[#222D42]/40">
            <HardDrive className="w-4 h-4 text-[#FF7A29]" />
            <div className="text-xs">
              <div className="text-[#64748B] text-[10px]">Répertoire GTA V</div>
              <div className="font-mono font-semibold text-white truncate max-w-[140px]">
                {status?.gtaFolder ? status.gtaFolder : "Non lié"}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2.5 p-2 rounded-lg bg-[#0A0E16]/60 border border-[#222D42]/40">
            <KeyRound className="w-4 h-4 text-[#FF7A29]" />
            <div className="text-xs">
              <div className="text-[#64748B] text-[10px]">Clés AES / NG</div>
              <div className="font-mono font-semibold text-emerald-400">
                {status?.keysLoaded ? "Chargées (AES 256)" : "Standard Magic"}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2.5 p-2 rounded-lg bg-[#0A0E16]/60 border border-[#222D42]/40">
            <Database className="w-4 h-4 text-[#FF7A29]" />
            <div className="text-xs">
              <div className="text-[#64748B] text-[10px]">Archives en cache</div>
              <div className="font-mono font-semibold text-white">
                {status?.loadedRpfsCount ?? 0} active(s)
              </div>
            </div>
          </div>
        </div>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-red-950/40 border border-red-800/60 text-red-300 text-xs flex items-center gap-3">
          <AlertTriangle className="w-4 h-4 text-red-400 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Manual Path Input Bar */}
      <form onSubmit={handleOpenManual} className="flex gap-2">
        <div className="relative flex-1">
          <input
            type="text"
            value={manualPath}
            onChange={(e) => setManualPath(e.target.value)}
            placeholder="Entrez un chemin absolu vers un fichier .rpf (ex: D:\Games\GTA V\update\update.rpf)..."
            className="w-full bg-[#121824] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-4 py-2.5 text-xs text-white placeholder-[#64748B] transition-colors font-mono"
          />
        </div>
        <button
          type="submit"
          disabled={!manualPath.trim() || !!loadingPath}
          className="bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] hover:border-[#FF7A29]/50 text-white px-5 py-2.5 rounded-xl text-xs font-semibold transition-all cursor-pointer disabled:opacity-50"
        >
          Ouvrir
        </button>
      </form>

      {/* Current Loaded RPF Card (if any) */}
      {currentRpf && (
        <div className="p-5 rounded-xl border border-[#FF7A29]/30 bg-[#121824]/90 space-y-3 shadow-lg">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-[#FF7A29]/15 text-[#FF7A29]">
                <Database className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-bold text-white text-sm">{currentRpf.name}</h3>
                <p className="text-[11px] text-[#94A3B8] font-mono">{currentRpf.filePath}</p>
              </div>
            </div>
            <button
              onClick={() => onNavigate("explorer")}
              className="flex items-center gap-1.5 bg-[#FF7A29] hover:bg-[#FF8F4D] text-white px-4 py-2 rounded-lg text-xs font-bold transition-colors cursor-pointer"
            >
              <span>Explorer l'archive</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-3 border-t border-[#222D42]/60 text-xs font-mono">
            <div>
              <span className="text-[#64748B]">Taille: </span>
              <span className="text-white font-semibold">{formatBytes(currentRpf.fileSize)}</span>
            </div>
            <div>
              <span className="text-[#64748B]">Entrées: </span>
              <span className="text-white font-semibold">{currentRpf.entryCount}</span>
            </div>
            <div>
              <span className="text-[#64748B]">Fichiers: </span>
              <span className="text-white font-semibold">{currentRpf.totalFileCount}</span>
            </div>
            <div>
              <span className="text-[#64748B]">Cryptage: </span>
              <span className="text-emerald-400 font-semibold">{currentRpf.encryption}</span>
            </div>
          </div>
        </div>
      )}

      {/* Quick Action Grid */}
      <div>
        <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] mb-3 font-semibold">
          Modules & Outils SR File Suite
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {/* Card 1: RPF Explorer */}
          <div
            onClick={() => onNavigate("explorer")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-[#FF7A29]/10 text-[#FF7A29] group-hover:scale-110 transition-transform">
                <Database className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                RPF Explorer Virtuel
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Parcourez des dizaines de milliers d'entrées sans latence grâce à la table virtualisée TanStack.
              </p>
            </div>
          </div>

          {/* Card 2: Texture Studio */}
          <div
            onClick={() => onNavigate("textures")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-sky-500/10 text-sky-400 group-hover:scale-110 transition-transform">
                <ImageIcon className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Studio de Textures (.YTD)
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Décompression à chaud des formats DXT1, DXT5, ATI1, ATI2 et exportation directe en PNG ou DDS natif.
              </p>
            </div>
          </div>

          {/* Card 3: Audio Lab */}
          <div
            onClick={() => onNavigate("audio")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-purple-500/10 text-purple-400 group-hover:scale-110 transition-transform">
                <Music className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Lecteur & Extracteur AWC
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Décodage audio Rockstar XXTEA et streaming PCM/WAV instantané vers le navigateur.
              </p>
            </div>
          </div>

          {/* Card 4: Mod Manager */}
          <div
            onClick={() => onNavigate("mods")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400 group-hover:scale-110 transition-transform">
                <ShieldCheck className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Mod Manager & Conflits
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Gestionnaire de paquets OIV/DLC/ASI, détection automatique des collisions de fichiers et bascule active.
              </p>
            </div>
          </div>

          {/* Card 5: Gen9 Converter */}
          <div
            onClick={() => onNavigate("gen9")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-amber-500/10 text-amber-400 group-hover:scale-110 transition-transform">
                <Layers className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Convertisseur Gen9 Next-Gen
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Conversion automatisée par lots de conteneurs et shaders entre PS5/Xbox Series et PC avec logs temps réel.
              </p>
            </div>
          </div>

          {/* Card 6: Project & Map Editor */}
          <div
            onClick={() => onNavigate("project_editor")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-rose-500/10 text-rose-400 group-hover:scale-110 transition-transform">
                <Map className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Éditeur de Map & Projet (.cwproj)
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Inspecteur d'entités avec entrées vectorielles style Blender et import/export complet YMAP XML.
              </p>
            </div>
          </div>

          {/* Card 7: Code & Meta Editor */}
          <div
            onClick={() => onNavigate("code_editor")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-teal-500/10 text-teal-400 group-hover:scale-110 transition-transform">
                <FileCode className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Éditeur de Code & Meta
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Éditeur syntaxique XML/META avec recherche/remplacement, vue diff et reformatage automatique.
              </p>
            </div>
          </div>

          {/* Card 8: Hex Viewer */}
          <div
            onClick={() => onNavigate("hex_viewer")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-indigo-500/10 text-indigo-400 group-hover:scale-110 transition-transform">
                <Binary className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Visionneuse Hexadécimale
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Inspection d'octets multi-types (Int8/16/32, Float, Binaire), décodeur ASCII et navigation par offset.
              </p>
            </div>
          </div>

          {/* Card 9: Jenkins Tool */}
          <div
            onClick={() => onNavigate("jenkins")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-cyan-500/10 text-cyan-400 group-hover:scale-110 transition-transform">
                <Hash className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Calculateur Jenkins JOAAT
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Calculateur de hachage 32-bit en direct, recherche inverse par dictionnaire et générateur par lot.
              </p>
            </div>
          </div>

          {/* Card 10: Crypto & Keys Tool */}
          <div
            onClick={() => onNavigate("crypto")}
            className="group p-5 rounded-xl bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/40 transition-all cursor-pointer shadow-sm space-y-3"
          >
            <div className="flex items-center justify-between">
              <div className="p-2.5 rounded-lg bg-orange-500/10 text-orange-400 group-hover:scale-110 transition-transform">
                <KeyRound className="w-5 h-5" />
              </div>
              <ArrowRight className="w-4 h-4 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white group-hover:text-[#FF7A29] transition-colors">
                Sécurité & Clés AES
              </h3>
              <p className="text-xs text-[#94A3B8] mt-1 leading-relaxed">
                Gestionnaire de clés AES-256 / NG et inspecteur d'intégrité de cryptage d'en-tête RPF.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
