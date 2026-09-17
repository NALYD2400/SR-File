import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api/client";
import { CryptoKeysStatus, RpfEncryptionInfo } from "../../types";
import { cn } from "../../lib/utils";
import {
  KeyRound,
  Copy,
  Check,
  RefreshCw,
  Lock,
  Unlock,
  AlertTriangle,
  FileCheck2,
  Cpu
} from "lucide-react";

export const CryptoTool: React.FC = () => {
  const queryClient = useQueryClient();
  const [customKeyInput, setCustomKeyInput] = useState<string>("");
  const [copiedHex, setCopiedHex] = useState(false);
  const [copiedB64, setCopiedB64] = useState(false);
  const [inspectedRpf, setInspectedRpf] = useState<RpfEncryptionInfo | null>(null);
  const [inspectPath, setInspectPath] = useState<string>("");
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Query keys status
  const { data: keysStatus, refetch: refetchKeys } = useQuery<CryptoKeysStatus>({
    queryKey: ["crypto-keys"],
    queryFn: () => api.getCryptoKeys(),
  });

  const setKeyMutation = useMutation({
    mutationFn: (key: string) => api.setAesKey(key),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["crypto-keys"] });
      setStatusMessage("Clé AES mise à jour avec succès !");
      setCustomKeyInput("");
      setTimeout(() => setStatusMessage(null), 3000);
    },
    onError: (err: any) => {
      setErrorMessage(err.message || "Échec de l'application de la clé.");
      setTimeout(() => setErrorMessage(null), 4000);
    },
  });

  const handleInspectRpfNative = async () => {
    try {
      setErrorMessage(null);
      const selected = await api.selectFileNative(
        "Sélectionnez une archive RPF à inspecter",
        "Archives RPF (*.rpf)",
        ["rpf"]
      );
      if (selected) {
        setInspectPath(selected);
        const info = await api.inspectRpfEncryption(selected);
        setInspectedRpf(info);
      }
    } catch (err: any) {
      setErrorMessage(err.message);
    }
  };

  const handleManualInspect = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inspectPath.trim()) return;
    try {
      setErrorMessage(null);
      const info = await api.inspectRpfEncryption(inspectPath.trim());
      setInspectedRpf(info);
    } catch (err: any) {
      setErrorMessage(err.message);
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Header */}
      <div className="p-5 border-b border-[#222D42] bg-[#121824]/60 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
              CRYPTOGRAPHIC ENGINE
            </span>
            <span className="text-xs text-[#64748B] font-mono">GESTIONNAIRE CLÉS & RPF ENCRYPTION</span>
          </div>
          <h1 className="text-xl font-bold text-white mt-1">Sécurité, Clés AES & Inspecteur RPF</h1>
          <p className="text-xs text-[#94A3B8]">
            Gestion des clés AES-256, tables de déchiffrement Next-Gen et vérification d'intégrité d'en-tête RPF.
          </p>
        </div>

        <button
          onClick={() => refetchKeys()}
          className="p-2 bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] rounded-xl text-[#94A3B8] hover:text-white transition-all cursor-pointer"
          title="Actualiser les clés"
        >
          <RefreshCw className="w-4 h-4" />
        </button>
      </div>

      {statusMessage && (
        <div className="mx-6 mt-4 p-3 rounded-xl bg-emerald-950/40 border border-emerald-800/60 text-emerald-300 text-xs flex items-center gap-2">
          <Check className="w-4 h-4 text-emerald-400" />
          <span>{statusMessage}</span>
        </div>
      )}

      {errorMessage && (
        <div className="mx-6 mt-4 p-3 rounded-xl bg-red-950/40 border border-red-800/60 text-red-300 text-xs flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 text-red-400" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Main Content Grid */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-6 p-6 overflow-y-auto custom-scrollbar">
        {/* Left Column: Key Manager (6 cols) */}
        <div className="lg:col-span-6 flex flex-col space-y-4">
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-4 shadow-sm">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <KeyRound className="w-4 h-4 text-[#FF7A29]" />
              <span>État des Clés Cryptographiques</span>
            </h2>

            {/* Diagnostic Badges */}
            <div className="grid grid-cols-2 gap-3">
              <div className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                <span className="text-[10px] text-[#64748B] font-mono">CLÉ AES-256 (PC)</span>
                <div className="flex items-center gap-2 font-mono text-xs font-bold">
                  {keysStatus?.aesKeyLoaded ? (
                    <>
                      <Lock className="w-3.5 h-3.5 text-emerald-400" />
                      <span className="text-emerald-400">Chargée (32 octets)</span>
                    </>
                  ) : (
                    <>
                      <Unlock className="w-3.5 h-3.5 text-amber-400" />
                      <span className="text-amber-400">Non chargée</span>
                    </>
                  )}
                </div>
              </div>

              <div className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                <span className="text-[10px] text-[#64748B] font-mono">TABLES NG (101 KEYS)</span>
                <div className="flex items-center gap-2 font-mono text-xs font-bold">
                  <Cpu className="w-3.5 h-3.5 text-sky-400" />
                  <span className="text-sky-400">{keysStatus?.ngKeysCount ?? 0} clés actives</span>
                </div>
              </div>

              <div className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                <span className="text-[10px] text-[#64748B] font-mono">TABLES DE DÉCHIFFREMENT</span>
                <div className="flex items-center gap-2 font-mono text-xs font-bold">
                  <span className="text-white">{keysStatus?.decryptTablesCount ?? 0} tables (17x16)</span>
                </div>
              </div>

              <div className="p-3.5 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                <span className="text-[10px] text-[#64748B] font-mono">CLÉ AUDIO AWC (XXTEA)</span>
                <div className="flex items-center gap-2 font-mono text-xs font-bold">
                  <span className={keysStatus?.awcKeyLoaded ? "text-emerald-400" : "text-[#64748B]"}>
                    {keysStatus?.awcKeyLoaded ? "Chargée" : "Intégrée"}
                  </span>
                </div>
              </div>
            </div>

            {/* Display Current AES Key */}
            {keysStatus?.aesKeyLoaded && (
              <div className="space-y-3 pt-2">
                <div className="space-y-1">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-[#94A3B8] font-mono text-[11px]">Format Hexadécimal (64 car.) :</span>
                    <button
                      onClick={() => {
                        if (keysStatus.aesKeyHex) {
                          navigator.clipboard.writeText(keysStatus.aesKeyHex);
                          setCopiedHex(true);
                          setTimeout(() => setCopiedHex(false), 2000);
                        }
                      }}
                      className="text-[11px] text-[#64748B] hover:text-white flex items-center gap-1 cursor-pointer"
                    >
                      {copiedHex ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                      <span>Copier</span>
                    </button>
                  </div>
                  <div className="p-2.5 rounded-xl bg-[#0A0E16] border border-[#222D42] font-mono text-[11px] text-amber-400/90 break-all select-all">
                    {keysStatus.aesKeyHex}
                  </div>
                </div>

                <div className="space-y-1">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-[#94A3B8] font-mono text-[11px]">Format Base64 (44 car.) :</span>
                    <button
                      onClick={() => {
                        if (keysStatus.aesKeyBase64) {
                          navigator.clipboard.writeText(keysStatus.aesKeyBase64);
                          setCopiedB64(true);
                          setTimeout(() => setCopiedB64(false), 2000);
                        }
                      }}
                      className="text-[11px] text-[#64748B] hover:text-white flex items-center gap-1 cursor-pointer"
                    >
                      {copiedB64 ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                      <span>Copier</span>
                    </button>
                  </div>
                  <div className="p-2.5 rounded-xl bg-[#0A0E16] border border-[#222D42] font-mono text-[11px] text-sky-400/90 break-all select-all">
                    {keysStatus.aesKeyBase64}
                  </div>
                </div>
              </div>
            )}

            {/* Custom Key Injector */}
            <div className="pt-4 border-t border-[#222D42] space-y-2">
              <label className="text-xs font-medium text-[#94A3B8]">
                Définir ou remplacer manuellement la clé AES :
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={customKeyInput}
                  onChange={(e) => setCustomKeyInput(e.target.value)}
                  placeholder="Entrez 64 car. hex ou 44 car. base64..."
                  className="flex-1 bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
                />
                <button
                  type="button"
                  onClick={() => setKeyMutation.mutate(customKeyInput)}
                  disabled={!customKeyInput.trim()}
                  className="px-4 py-2 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-colors cursor-pointer disabled:opacity-50"
                >
                  Appliquer
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Right Column: RPF Encryption Inspector (6 cols) */}
        <div className="lg:col-span-6 flex flex-col space-y-4">
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-4 shadow-sm flex-1 flex flex-col">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <FileCheck2 className="w-4 h-4 text-[#FF7A29]" />
              <span>Inspecteur de Chiffrement d'Archive RPF</span>
            </h2>

            <form onSubmit={handleManualInspect} className="space-y-2">
              <div className="flex gap-2">
                <input
                  type="text"
                  value={inspectPath}
                  onChange={(e) => setInspectPath(e.target.value)}
                  placeholder="Chemin absolu vers un fichier .rpf..."
                  className="flex-1 bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
                />
                <button
                  type="button"
                  onClick={handleInspectRpfNative}
                  className="px-3 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer"
                >
                  Parcourir
                </button>
              </div>
            </form>

            {/* Inspection Results Card */}
            {inspectedRpf ? (
              <div className="p-4 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-3 font-mono text-xs">
                <div className="flex items-center justify-between pb-2 border-b border-[#222D42]">
                  <span className="text-[#94A3B8]">Type de chiffrement :</span>
                  <span
                    className={cn(
                      "font-bold px-2 py-0.5 rounded text-[11px]",
                      inspectedRpf.encryption.includes("OPEN") && "bg-emerald-500/15 text-emerald-400 border border-emerald-500/30",
                      inspectedRpf.encryption.includes("AES") && "bg-amber-500/15 text-amber-400 border border-amber-500/30",
                      inspectedRpf.encryption.includes("NG") && "bg-sky-500/15 text-sky-400 border border-sky-500/30"
                    )}
                  >
                    {inspectedRpf.encryption}
                  </span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#94A3B8]">Validité de l'en-tête :</span>
                  <span className={inspectedRpf.isValidHeader ? "text-emerald-400 font-bold" : "text-red-400 font-bold"}>
                    {inspectedRpf.isValidHeader ? "En-tête RPF valide" : "En-tête corrompu ou inconnu"}
                  </span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-[#94A3B8]">Version du format :</span>
                  <span className="text-white font-bold">RPF v{inspectedRpf.version}</span>
                </div>

                <div className="pt-2 border-t border-[#222D42] text-[10px] text-[#64748B] break-all">
                  Fichier : {inspectedRpf.filePath}
                </div>
              </div>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center text-center p-8 space-y-3 opacity-60 border border-dashed border-[#222D42] rounded-xl">
                <Lock className="w-8 h-8 text-[#64748B]" />
                <div className="text-xs font-semibold text-white">Sélectionnez une archive RPF</div>
                <p className="text-[11px] text-[#94A3B8] max-w-xs">
                  Analysez instantanément le flag d'encryption de n'importe quel fichier RPF (OpenIV, Vanilla AES ou NG).
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
