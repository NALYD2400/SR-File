import React, { useState, useEffect } from "react";
import { AppStatus } from "../../types";
import { api } from "../../api/client";
import { X, Folder, HardDrive, CheckCircle2, AlertCircle, RefreshCw, Cpu } from "lucide-react";

interface SettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  status?: AppStatus;
  onSaveConfig: (folder: string, isGen9: boolean, key?: string) => Promise<void>;
}

export const SettingsModal: React.FC<SettingsModalProps> = ({
  isOpen,
  onClose,
  status,
  onSaveConfig,
}) => {
  const [folderPath, setFolderPath] = useState<string>("");
  const [isGen9, setIsGen9] = useState<boolean>(false);
  const [aesKey, setAesKey] = useState<string>("");
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [saveMessage, setSaveMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  useEffect(() => {
    if (status) {
      if (status.gtaFolder) setFolderPath(status.gtaFolder);
      setIsGen9(status.isGen9);
    }
  }, [status]);

  if (!isOpen) return null;

  const handlePickFolder = async () => {
    try {
      const selected = await api.selectFolderNative("Sélectionnez le dossier racine de Grand Theft Auto V");
      if (selected) {
        setFolderPath(selected);
      }
    } catch (err: any) {
      console.error(err);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setSaveMessage(null);
    try {
      await onSaveConfig(folderPath.trim(), isGen9, aesKey.trim() || undefined);
      setSaveMessage({
        type: "success",
        text: "Configuration enregistrée. Les clés GTA V ont été synchronisées avec le Sidecar.",
      });
      setTimeout(() => {
        setSaveMessage(null);
      }, 3000);
    } catch (err: any) {
      setSaveMessage({
        type: "error",
        text: err.message || "Erreur lors de la configuration du dossier.",
      });
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4">
      <div className="w-full max-w-lg rounded-2xl border border-[#222D42] bg-[#121824] shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="p-4 border-b border-[#222D42] bg-[#151D2D] flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="p-1.5 rounded-lg bg-[#FF7A29]/15 text-[#FF7A29]">
              <HardDrive className="w-4 h-4" />
            </div>
            <div>
              <h2 className="text-sm font-bold text-white">Configuration du Jeu GTA V</h2>
              <p className="text-[11px] text-[#64748B]">Paramètres d'accès aux archives et clés de chiffrement</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-[#64748B] hover:text-white hover:bg-[#1A2234] transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSave} className="p-5 space-y-4 text-xs">
          {/* Game Folder Picker */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-semibold text-[#94A3B8] block">
              Dossier d'installation GTA V (ex: GTA5.exe)
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={folderPath}
                onChange={(e) => setFolderPath(e.target.value)}
                placeholder="C:\Program Files\Rockstar Games\Grand Theft Auto V"
                className="flex-1 bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3 py-2 text-white placeholder-[#64748B] font-mono text-xs"
              />
              <button
                type="button"
                onClick={handlePickFolder}
                className="flex items-center gap-1.5 bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white px-3 py-2 rounded-xl font-semibold cursor-pointer"
              >
                <Folder className="w-3.5 h-3.5 text-[#FF7A29]" />
                <span>Parcourir</span>
              </button>
            </div>
            <p className="text-[10px] text-[#64748B]">
              Nécessaire pour le chargement des archives cryptées (AES/NG) et l'exploration de l'arbre complet du jeu.
            </p>
          </div>

          {/* Gen9 Toggle */}
          <div className="flex items-center justify-between p-3 rounded-xl bg-[#0A0E16] border border-[#222D42]">
            <div className="space-y-0.5">
              <span className="font-semibold text-white block">Mode Gen9 (Enhanced / Consoles)</span>
              <span className="text-[10px] text-[#64748B]">
                Activer pour les archives et formats de GTA V Enhanced / Expanded
              </span>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={isGen9}
                onChange={(e) => setIsGen9(e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-9 h-5 bg-[#222D42] peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-[#FF7A29]"></div>
            </label>
          </div>

          {/* Custom AES Key */}
          <div className="space-y-1.5">
            <label className="text-[11px] font-semibold text-[#94A3B8] block flex items-center justify-between">
              <span>Clé AES Personnalisée (Base64 - optionnel)</span>
              <span className="text-[10px] text-[#64748B] font-mono">Auto-détection si vide</span>
            </label>
            <input
              type="password"
              value={aesKey}
              onChange={(e) => setAesKey(e.target.value)}
              placeholder="Laissez vide pour extraire la clé automatiquement de GTA5.exe"
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3 py-2 text-white placeholder-[#64748B] font-mono text-xs"
            />
          </div>

          {/* Sidecar Status Summary */}
          <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] flex items-center justify-between text-[11px] font-mono">
            <div className="flex items-center gap-2">
              <Cpu className="w-3.5 h-3.5 text-[#FF7A29]" />
              <span className="text-[#64748B]">État des clés:</span>
              <span className={status?.keysLoaded ? "text-emerald-400 font-bold" : "text-amber-400"}>
                {status?.keysLoaded ? "Clés valides" : "Non initialisées"}
              </span>
            </div>
            <div className="text-[#64748B]">Port IPC: :5890</div>
          </div>

          {/* Messages */}
          {saveMessage && (
            <div
              className={`p-3 rounded-xl border text-xs flex items-center gap-2 ${
                saveMessage.type === "success"
                  ? "bg-emerald-950/40 border-emerald-800 text-emerald-300"
                  : "bg-red-950/40 border-red-800 text-red-300"
              }`}
            >
              {saveMessage.type === "success" ? (
                <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-400" />
              ) : (
                <AlertCircle className="w-4 h-4 shrink-0 text-red-400" />
              )}
              <span>{saveMessage.text}</span>
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] text-[#94A3B8] hover:text-white font-semibold cursor-pointer transition-colors"
            >
              Fermer
            </button>
            <button
              type="submit"
              disabled={isSaving}
              className="flex items-center gap-2 px-5 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold cursor-pointer transition-all disabled:opacity-50 shadow-lg"
            >
              {isSaving && <RefreshCw className="w-3.5 h-3.5 animate-spin" />}
              <span>{isSaving ? "Synchronisation..." : "Enregistrer & Charger"}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
