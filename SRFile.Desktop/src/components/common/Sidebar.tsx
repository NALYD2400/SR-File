import React from "react";
import { ActiveView } from "../../types";
import {
  LayoutDashboard,
  FolderTree,
  Image,
  Music,
  Sliders,
  ShieldCheck,
  Layers,
  Map,
  FileCode,
  Binary,
  Hash,
  KeyRound,
  Cpu
} from "lucide-react";
import { cn } from "../../lib/utils";

interface SidebarProps {
  activeView: ActiveView;
  onSelectView: (view: ActiveView) => void;
  rpfLoaded: boolean;
}

interface NavSection {
  title: string;
  items: {
    id: ActiveView;
    label: string;
    icon: React.ComponentType<{ className?: string }>;
    badge?: string;
  }[];
}

export const Sidebar: React.FC<SidebarProps> = ({ activeView, onSelectView, rpfLoaded }) => {
  const sections: NavSection[] = [
    {
      title: "Exploration & Assets",
      items: [
        { id: "dashboard", label: "Hub Central", icon: LayoutDashboard },
        { id: "explorer", label: "RPF Explorer", icon: FolderTree, badge: rpfLoaded ? "Actif" : undefined },
        { id: "textures", label: "Texture Studio", icon: Image },
        { id: "audio", label: "Lecteur AWC", icon: Music },
      ],
    },
    {
      title: "Modding GTA V",
      items: [
        { id: "mods", label: "Mod Manager", icon: ShieldCheck },
        { id: "gen9", label: "Convertisseur Gen9", icon: Layers },
        { id: "project_editor", label: "Éditeur de Projet", icon: Map },
      ],
    },
    {
      title: "Outils 2D & Crypto",
      items: [
        { id: "code_editor", label: "Éditeur de Code", icon: FileCode },
        { id: "hex_viewer", label: "Visionneuse Hex", icon: Binary },
        { id: "jenkins", label: "Outil Jenkins", icon: Hash },
        { id: "crypto", label: "Sécurité & Clés", icon: KeyRound },
      ],
    },
    {
      title: "Système",
      items: [
        { id: "settings", label: "Configuration", icon: Sliders },
      ],
    },
  ];

  return (
    <aside className="w-56 border-r border-[#222D42] bg-[#0A0E16] flex flex-col justify-between p-3 select-none shrink-0 overflow-y-auto custom-scrollbar">
      <div className="space-y-5">
        {sections.map((sec, secIdx) => (
          <div key={secIdx} className="space-y-1.5">
            <span className="text-[10px] font-mono uppercase tracking-wider text-[#64748B] px-3 font-semibold">
              {sec.title}
            </span>
            <nav className="space-y-0.5">
              {sec.items.map((item) => {
                const Icon = item.icon;
                const isActive = activeView === item.id;
                return (
                  <button
                    key={item.id}
                    onClick={() => onSelectView(item.id)}
                    className={cn(
                      "w-full flex items-center justify-between px-3 py-1.5 rounded-lg text-xs font-medium transition-all group cursor-pointer",
                      isActive
                        ? "bg-[#121824] text-white border border-[#222D42] shadow-[inset_0_1px_0_rgba(255,122,41,0.2)]"
                        : "text-[#94A3B8] hover:text-[#F1F5F9] hover:bg-[#121824]/60"
                    )}
                  >
                    <div className="flex items-center gap-2.5 truncate">
                      <Icon
                        className={cn(
                          "w-4 h-4 shrink-0 transition-colors",
                          isActive ? "text-[#FF7A29]" : "text-[#64748B] group-hover:text-[#94A3B8]"
                        )}
                      />
                      <span className="truncate">{item.label}</span>
                    </div>
                    {item.badge && (
                      <span className="text-[9px] font-mono px-1.5 py-0.2 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 shrink-0">
                        {item.badge}
                      </span>
                    )}
                  </button>
                );
              })}
            </nav>
          </div>
        ))}
      </div>

      {/* Engine & Stack badge */}
      <div className="mt-4 p-3 rounded-xl bg-[#121824]/60 border border-[#222D42]/60 text-[11px] text-[#64748B] space-y-1.5 shrink-0">
        <div className="flex items-center gap-1.5 text-[#94A3B8] font-semibold text-[10px]">
          <Cpu className="w-3.5 h-3.5 text-[#FF7A29]" />
          <span>SR ENGINE STACK</span>
        </div>
        <p className="text-[10px] text-[#64748B] leading-tight font-mono">
          Tauri v2 + Rust Shell<br />
          .NET 8 Headless Sidecar<br />
          CodeWalker.Core Engine
        </p>
      </div>
    </aside>
  );
};
