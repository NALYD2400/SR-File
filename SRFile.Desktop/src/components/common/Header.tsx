import React from "react";
import { AppStatus, RpfInfo } from "../../types";
import { Search, Settings as SettingsIcon, Database, HardDrive, AlertCircle, RefreshCw } from "lucide-react";

interface HeaderProps {
  status?: AppStatus;
  currentRpf?: RpfInfo | null;
  onOpenSettings: () => void;
  onOpenCommandPalette: () => void;
  onReloadStatus: () => void;
  isStatusLoading?: boolean;
}

export const Header: React.FC<HeaderProps> = ({
  status,
  currentRpf,
  onOpenSettings,
  onOpenCommandPalette,
  onReloadStatus,
  isStatusLoading,
}) => {
  return (
    <header className="h-14 border-b border-[#222D42] bg-[#0A0E16]/95 backdrop-blur-md px-4 flex items-center justify-between select-none z-30">
      {/* Brand logo & title */}
      <div className="flex items-center gap-3">
        <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-gradient-to-br from-[#FF7A29] to-[#D65910] shadow-[0_0_15px_rgba(255,122,41,0.35)] text-white font-black text-sm tracking-wider">
          SR
        </div>
        <div className="flex flex-col">
          <div className="flex items-center gap-2">
            <span className="text-sm font-bold tracking-tight text-[#F1F5F9]">SR FILE SUITE</span>
            <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
              v2.0 TAURI
            </span>
          </div>
          <span className="text-[10px] text-[#64748B] font-mono tracking-widest uppercase">
            CodeWalker.Core C# Sidecar
          </span>
        </div>
      </div>

      {/* Center: Current RPF Context or Search Pill */}
      <div className="flex items-center gap-3 max-w-md w-full mx-4">
        {currentRpf ? (
          <div className="flex items-center gap-2 bg-[#121824] border border-[#222D42] px-3 py-1.5 rounded-lg text-xs font-mono text-[#F1F5F9] w-full truncate shadow-inner">
            <Database className="w-3.5 h-3.5 text-[#FF7A29] shrink-0" />
            <span className="text-[#94A3B8]">RPF:</span>
            <span className="font-semibold text-white truncate">{currentRpf.name}</span>
            <span className="text-[10px] text-[#64748B] ml-auto shrink-0 font-mono">
              ({currentRpf.entryCount} entries)
            </span>
          </div>
        ) : (
          <button
            onClick={onOpenCommandPalette}
            className="flex items-center justify-between w-full bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#334360] px-3 py-1.5 rounded-lg text-xs text-[#64748B] transition-colors group cursor-pointer"
          >
            <div className="flex items-center gap-2">
              <Search className="w-3.5 h-3.5 text-[#64748B] group-hover:text-[#FF7A29] transition-colors" />
              <span>Rechercher des fichiers, textures, sons...</span>
            </div>
            <kbd className="font-mono text-[10px] bg-[#0A0E16] border border-[#222D42] px-1.5 py-0.5 rounded text-[#94A3B8]">
              Ctrl+K
            </kbd>
          </button>
        )}
      </div>

      {/* Right controls */}
      <div className="flex items-center gap-3">
        {/* GTA Folder status */}
        <div className="hidden lg:flex items-center gap-2 text-xs px-2.5 py-1 rounded-md bg-[#121824] border border-[#222D42]">
          <HardDrive className="w-3.5 h-3.5 text-[#94A3B8]" />
          <span className="text-[#64748B]">GTA V:</span>
          {status?.gtaFolder ? (
            <span className="text-emerald-400 font-mono text-[11px] truncate max-w-[120px]" title={status.gtaFolder}>
              Connecté
            </span>
          ) : (
            <span className="text-amber-400 font-mono text-[11px]">Non configuré</span>
          )}
        </div>

        {/* Sidecar status pill */}
        <div
          onClick={onReloadStatus}
          className="flex items-center gap-2 text-xs px-2.5 py-1 rounded-md bg-[#121824] border border-[#222D42] cursor-pointer hover:border-[#334360] transition-colors"
          title="Cliquez pour rafraîchir l'état du backend"
        >
          {status?.running ? (
            <>
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </span>
              <span className="text-emerald-400 font-mono text-[11px]">Sidecar :5890</span>
            </>
          ) : (
            <>
              <AlertCircle className="w-3 h-3 text-red-400" />
              <span className="text-red-400 font-mono text-[11px]">Sidecar Hors-ligne</span>
            </>
          )}
          {isStatusLoading && <RefreshCw className="w-2.5 h-2.5 animate-spin text-[#94A3B8]" />}
        </div>

        {/* Settings button */}
        <button
          onClick={onOpenSettings}
          className="p-1.5 rounded-lg bg-[#121824] hover:bg-[#172030] border border-[#222D42] hover:border-[#FF7A29]/50 text-[#94A3B8] hover:text-[#FF7A29] transition-all cursor-pointer shadow-sm"
          title="Paramètres & Configuration GTA V"
        >
          <SettingsIcon className="w-4 h-4" />
        </button>
      </div>
    </header>
  );
};
