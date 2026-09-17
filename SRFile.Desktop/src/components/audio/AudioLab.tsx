import React from "react";
import { FolderOpen, Volume2 } from "lucide-react";

interface AudioLabProps {
  onNavigateToExplorer: () => void;
}

export const AudioLab: React.FC<AudioLabProps> = ({ onNavigateToExplorer }) => {
  return (
    <div className="flex-1 flex flex-col p-6 bg-[#0A0E16] text-[#F1F5F9] overflow-y-auto custom-scrollbar space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-purple-500/15 text-purple-400 border border-purple-500/30">
              MODULE AUDIO AWC
            </span>
            <span className="text-xs text-[#64748B] font-mono">STREAMING MULTI-CANAL PCM/WAV</span>
          </div>
          <h1 className="text-xl font-black text-white mt-1">
            Laboratoire Audio Rockstar Wave Container (.AWC)
          </h1>
          <p className="text-xs text-[#94A3B8] mt-1">
            Décodage en mémoire de l'algorithme XXTEA Rockstar et streaming direct des ondes sonores vers les haut-parleurs.
          </p>
        </div>

        <button
          onClick={onNavigateToExplorer}
          className="flex items-center gap-2 bg-[#FF7A29] hover:bg-[#FF8F4D] text-white px-4 py-2 rounded-xl text-xs font-bold transition-colors cursor-pointer"
        >
          <FolderOpen className="w-4 h-4" />
          <span>Parcourir les .awc dans l'Explorateur</span>
        </button>
      </div>

      {/* Feature showcase */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-purple-400 font-bold text-xs">Décodage RS-XXTEA</div>
          <p className="text-xs text-[#94A3B8]">
            Déchiffrement natif des blocs audio protégés par la clé propriétaire AWC de GTA V.
          </p>
        </div>
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-emerald-400 font-bold text-xs">Support Stéréo & Multi-pistes</div>
          <p className="text-xs text-[#94A3B8]">
            Lecture simultanée des flux mono combinés en canaux gauche/droite et dialogue radio.
          </p>
        </div>
        <div className="p-4 rounded-xl bg-[#121824] border border-[#222D42] space-y-2">
          <div className="text-[#FF7A29] font-bold text-xs">Streaming HTTP WAV 44.1/48kHz</div>
          <p className="text-xs text-[#94A3B8]">
            Génération à chaud des en-têtes RIFF WAVE sans écriture sur disque dur pour une latence zéro.
          </p>
        </div>
      </div>

      {/* Instructions */}
      <div className="p-5 rounded-xl bg-[#121824] border border-[#222D42] flex items-center gap-4">
        <div className="p-3 rounded-xl bg-purple-500/15 text-purple-400">
          <Volume2 className="w-8 h-8" />
        </div>
        <div className="space-y-1">
          <h3 className="text-sm font-bold text-white">Écoute d'un conteneur AWC</h3>
          <p className="text-xs text-[#94A3B8]">
            Rendez-vous dans l'Explorateur RPF, naviguez par exemple dans <code>x64/audio/sfx</code>, et sélectionnez n'importe quel fichier <code>.awc</code>. Vous aurez accès à la liste complète des pistes avec lecture et scrubbing interactifs.
          </p>
        </div>
      </div>
    </div>
  );
};
