import React from "react";
import { Image as ImageIcon, FolderOpen } from "lucide-react";

interface TextureStudioProps {
  onNavigateToExplorer: () => void;
}

export const TextureStudio: React.FC<TextureStudioProps> = ({ onNavigateToExplorer }) => {

  return (
    <div className="flex-1 flex flex-col p-6 bg-[#0A0E16] text-[#F1F5F9] overflow-y-auto custom-scrollbar space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-sky-500/15 text-sky-400 border border-sky-500/30">
              MODULE TEXTURE STUDIO
            </span>
            <span className="text-xs text-[#64748B] font-mono">DDS / DXT / PNG ACCELERATION</span>
          </div>
          <h1 className="text-xl font-black text-white mt-1">
            Studio d'Inspection des Dictionnaires de Textures (.YTD)
          </h1>
          <p className="text-xs text-[#94A3B8] mt-1">
            Visualisez, décompressez et convertissez les textures du jeu sans perte de qualité directement via le pipeline C# DDSIO.
          </p>
        </div>

        <button
          onClick={onNavigateToExplorer}
          className="flex items-center gap-2 bg-[#FF7A29] hover:bg-[#FF8F4D] text-white px-4 py-2 rounded-xl text-xs font-bold transition-colors cursor-pointer"
        >
          <FolderOpen className="w-4 h-4" />
          <span>Parcourir les .ytd dans l'Explorateur</span>
        </button>
      </div>

      {/* Feature showcase cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-[#FF7A29] font-bold text-xs">Formats supportés</div>
          <p className="text-xs text-[#94A3B8]">
            DXT1 (BC1), DXT3 (BC2), DXT5 (BC3), ATI1 (BC4), ATI2 (BC5), RGBA8, B5G5R5A1.
          </p>
        </div>
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-sky-400 font-bold text-xs">Extraction & Mipmaps</div>
          <p className="text-xs text-[#94A3B8]">
            Accédez à tous les niveaux de mipmaps (de pleine résolution jusqu'aux vignettes 1x1).
          </p>
        </div>
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-emerald-400 font-bold text-xs">Conversion PNG sans GPU</div>
          <p className="text-xs text-[#94A3B8]">
            Encodeur PNG pur C# avec Deflate/Zlib pour une conversion instantanée sans dépendance GDI+.
          </p>
        </div>
      </div>

      {/* Info banner */}
      <div className="p-5 rounded-xl bg-[#121824] border border-[#222D42] flex items-center gap-4">
        <div className="p-3 rounded-xl bg-[#FF7A29]/15 text-[#FF7A29]">
          <ImageIcon className="w-8 h-8" />
        </div>
        <div className="space-y-1">
          <h3 className="text-sm font-bold text-white">Comment ouvrir un dictionnaire de textures ?</h3>
          <p className="text-xs text-[#94A3B8]">
            Dans l'onglet <strong>RPF Explorer</strong>, naviguez dans un dossier contenant un fichier <code>.ytd</code> (par exemple <code>cdimages/vehicles.rpf</code> ou <code>hud.ytd</code>) et cliquez dessus pour ouvrir instantanément l'inspecteur de textures interactif !
          </p>
        </div>
      </div>
    </div>
  );
};
