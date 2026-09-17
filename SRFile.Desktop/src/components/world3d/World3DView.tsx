import React, { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../api/client";
import { useToast } from "../common/Toast";
import {
  Compass,
  Play,
  Navigation,
  MapPin,
  Eye,
  Keyboard,
  Layers,
  Sparkles,
  RefreshCw,
} from "lucide-react";

interface Landmark {
  name: string;
  zone: string;
  coords: string;
  description: string;
}

const LANDMARKS: Landmark[] = [
  { name: "Maze Bank Tower (Toit)", zone: "Centre-ville LS", coords: "-75.0, -818.0, 326.0", description: "Le point culminant de la skyline de Los Santos." },
  { name: "Los Santos Customs", zone: "Burton", coords: "-365.0, -131.0, 38.0", description: "Atelier de customisation emblématique." },
  { name: "Aéroport LSIA", zone: "Los Santos Sud", coords: "-1034.0, -2733.0, 13.8", description: "Pistes et terminaux de l'aéroport international." },
  { name: "Mont Chiliad (Sommet)", zone: "Blaine County", coords: "450.0, 5600.0, 785.0", description: "Le plus haut pic montagneux de San Andreas." },
  { name: "Sandy Shores Airstrip", zone: "Désert de Grand Senora", coords: "1730.0, 3280.0, 41.0", description: "L'aérodrome de Trevor Philips." },
  { name: "Jetée de Del Perro", zone: "Plage Del Perro", coords: "-1690.0, -1075.0, 13.0", description: "La grande roue et les attractions du front de mer." },
];

export const World3DView: React.FC = () => {
  const { showToast } = useToast();
  const [selectedCoords, setSelectedCoords] = useState<string>("-75.0, -818.0, 326.0");
  const [isLaunching, setIsLaunching] = useState(false);

  const { data: worldStatus, refetch: refetchStatus, isFetching } = useQuery({
    queryKey: ["world-status"],
    queryFn: () => api.getWorldStatus(),
    refetchInterval: 4000,
  });

  const handleLaunch = async (customPos?: string) => {
    try {
      setIsLaunching(true);
      const pos = customPos || selectedCoords;
      if (customPos) setSelectedCoords(customPos);
      const res = await api.launchWorld3D(pos);
      showToast(
        "Monde 3D Lancé !",
        res.message || "Le moteur DirectX 11 est actif avec la nouvelle interface moderne.",
        "success"
      );
      await refetchStatus();
    } catch (err: any) {
      showToast(
        "Erreur de lancement",
        err.message || "Impossible de démarrer SR File.exe.",
        "error"
      );
    } finally {
      setIsLaunching(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col overflow-hidden bg-[#0A0E16]">
      {/* Top Banner */}
      <div className="px-6 py-4 border-b border-[#222D42] bg-[#121824]/60 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
            <Compass className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-bold text-white tracking-wide">Monde 3D (DirectX 11 & NoClip)</h1>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-[#FF7A29]/20 text-[#FF7A29] border border-[#FF7A29]/40 font-semibold">
                Direct3D 11 Native
              </span>
            </div>
            <p className="text-xs text-[#94A3B8]">
              Rendu temps réel de Los Santos, streaming des YMAPs et nouveau panneau d'outils WebUI intégré.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => refetchStatus()}
            disabled={isFetching}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#182232] hover:bg-[#1E2B3E] text-[#94A3B8] hover:text-white border border-[#222D42] text-xs transition-colors cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isFetching ? "animate-spin" : ""}`} />
            <span>Actualiser</span>
          </button>

          <button
            onClick={() => handleLaunch()}
            disabled={isLaunching}
            className="flex items-center gap-2 px-5 py-2 rounded-xl bg-gradient-to-r from-[#FF7A29] to-[#FF8F4D] hover:from-[#FF8F4D] hover:to-[#FFA066] text-white font-bold text-xs shadow-lg shadow-[#FF7A29]/25 transition-all cursor-pointer disabled:opacity-50"
          >
            <Play className="w-4 h-4 fill-white" />
            <span>{isLaunching ? "Lancement en cours..." : worldStatus?.isRunning ? "Relancer le Monde 3D" : "Lancer le Monde 3D"}</span>
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 overflow-y-auto p-6 space-y-6 custom-scrollbar">
        {/* Status Bar Card */}
        <div className="p-4 rounded-xl border border-[#222D42] bg-[#121824] flex items-center justify-between">
          <div className="flex items-center gap-3">
            {worldStatus?.isRunning ? (
              <div className="flex items-center gap-2 text-emerald-400">
                <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 animate-ping" />
                <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 -ml-4.5" />
                <span className="text-xs font-semibold">Moteur 3D en cours d'exécution (Processus actif)</span>
              </div>
            ) : (
              <div className="flex items-center gap-2 text-[#94A3B8]">
                <span className="w-2.5 h-2.5 rounded-full bg-[#64748B]" />
                <span className="text-xs font-medium">Moteur 3D en attente de lancement</span>
              </div>
            )}
          </div>

          <div className="flex items-center gap-4 text-xs font-mono text-[#64748B]">
            <div>
              Exécutable:{" "}
              <span className={worldStatus?.exeFound ? "text-emerald-400 font-semibold" : "text-rose-400 font-semibold"}>
                {worldStatus?.exeFound ? "SR File.exe (Vérifié)" : "Non trouvé"}
              </span>
            </div>
            {worldStatus?.exePath && (
              <div className="truncate max-w-xs text-[11px] text-[#475569]" title={worldStatus.exePath}>
                {worldStatus.exePath}
              </div>
            )}
          </div>
        </div>

        {/* Feature Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="p-4 rounded-xl border border-[#222D42] bg-[#121824] space-y-2">
            <div className="flex items-center gap-2 text-[#FF7A29]">
              <Sparkles className="w-4 h-4" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">Panneau WebUI Moderne</h3>
            </div>
            <p className="text-xs text-[#94A3B8] leading-relaxed">
              L'ancien panneau WinForms sur le viewport 3D a été remplacé par un sidebar HTML5/CSS glassmorphic avec sliders haute précision, commutateurs tactiles et 4 onglets dédiés.
            </p>
          </div>

          <div className="p-4 rounded-xl border border-[#222D42] bg-[#121824] space-y-2">
            <div className="flex items-center gap-2 text-sky-400">
              <Eye className="w-4 h-4" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">Navigation NoClip 6-DOF</h3>
            </div>
            <p className="text-xs text-[#94A3B8] leading-relaxed">
              Volez librement à travers toute la carte de San Andreas. Déplacez-vous à travers les bâtiments, les intérieurs MLO et inspectez les collisions en temps réel.
            </p>
          </div>

          <div className="p-4 rounded-xl border border-[#222D42] bg-[#121824] space-y-2">
            <div className="flex items-center gap-2 text-emerald-400">
              <Layers className="w-4 h-4" />
              <h3 className="text-xs font-bold text-white uppercase tracking-wider">Rendu Direct3D 11</h3>
            </div>
            <p className="text-xs text-[#94A3B8] leading-relaxed">
              Prise en charge complète du deferred shading, de l'occlusion ambiante (SSAO), du bloom HDR, du cycle jour/nuit et du système météo GTA V.
            </p>
          </div>
        </div>

        {/* Landmark Quick Teleport Table */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <MapPin className="w-4 h-4 text-[#FF7A29]" />
              <h2 className="text-sm font-bold text-white">Points d'Intérêt & Téléportation Directe</h2>
            </div>
            <span className="text-xs text-[#64748B]">Cliquez sur un lieu pour démarrer directement à cet endroit</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {LANDMARKS.map((lm, idx) => (
              <div
                key={idx}
                onClick={() => handleLaunch(lm.coords)}
                className="group p-3.5 rounded-xl border border-[#222D42] bg-[#121824] hover:bg-[#182232] hover:border-[#FF7A29]/50 transition-all cursor-pointer space-y-2"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <h3 className="text-xs font-bold text-white group-hover:text-[#FF7A29] transition-colors">{lm.name}</h3>
                    <span className="text-[10px] text-[#64748B] font-mono">{lm.zone}</span>
                  </div>
                  <button className="opacity-0 group-hover:opacity-100 p-1 rounded-md bg-[#FF7A29]/20 text-[#FF7A29] transition-opacity">
                    <Navigation className="w-3.5 h-3.5" />
                  </button>
                </div>
                <p className="text-[11px] text-[#94A3B8] line-clamp-2">{lm.description}</p>
                <div className="text-[10px] font-mono text-[#64748B] bg-[#0A0E16] px-2 py-1 rounded border border-[#222D42]/60">
                  {lm.coords}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Controls & Shortcuts Cheat Sheet */}
        <div className="p-5 rounded-xl border border-[#222D42] bg-[#121824] space-y-4">
          <div className="flex items-center gap-2">
            <Keyboard className="w-4 h-4 text-[#FF7A29]" />
            <h2 className="text-sm font-bold text-white">Raccourcis Clavier en Mode NoClip 3D</h2>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
            <div className="space-y-1 bg-[#0A0E16] p-3 rounded-lg border border-[#222D42]/50">
              <span className="font-mono text-[11px] text-[#FF7A29] font-bold">W / A / S / D (ou ZQSD)</span>
              <p className="text-[11px] text-[#94A3B8]">Avancer, reculer, pas latéral gauche/droite</p>
            </div>
            <div className="space-y-1 bg-[#0A0E16] p-3 rounded-lg border border-[#222D42]/50">
              <span className="font-mono text-[11px] text-[#FF7A29] font-bold">Clic Droit + Souris</span>
              <p className="text-[11px] text-[#94A3B8]">Orienter le regard de la caméra (Look-at)</p>
            </div>
            <div className="space-y-1 bg-[#0A0E16] p-3 rounded-lg border border-[#222D42]/50">
              <span className="font-mono text-[11px] text-[#FF7A29] font-bold">Espace / Ctrl Gauche</span>
              <p className="text-[11px] text-[#94A3B8]">Monter (Altitude +) / Descendre (Altitude -)</p>
            </div>
            <div className="space-y-1 bg-[#0A0E16] p-3 rounded-lg border border-[#222D42]/50">
              <span className="font-mono text-[11px] text-[#FF7A29] font-bold">Shift / Molette</span>
              <p className="text-[11px] text-[#94A3B8]">Sprint caméra / Ajuster la vitesse de déplacement</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
