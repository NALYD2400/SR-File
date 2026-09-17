import React, { useState, useEffect, useRef } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { api } from "../../api/client";
import { Gen9ConversionRequest, Gen9ConversionStatus } from "../../types";
import { cn } from "../../lib/utils";
import {
  FolderOpen,
  Play,
  Square,
  Trash2,
  Copy,
  Check,
  AlertTriangle,
  Terminal,
  Settings2,
  ListPlus
} from "lucide-react";

interface BatchJob {
  id: string;
  inputPath: string;
  outputPath: string;
  preset: "gen9_to_pc" | "pc_to_gen9" | "textures_only";
  status: "pending" | "processing" | "completed" | "error";
}

export const Gen9Converter: React.FC = () => {
  const [inputFolder, setInputFolder] = useState("");
  const [outputFolder, setOutputFolder] = useState("");
  const [preset, setPreset] = useState<"gen9_to_pc" | "pc_to_gen9" | "textures_only">("gen9_to_pc");
  const [processSubfolders, setProcessSubfolders] = useState(true);
  const [overwriteExisting, setOverwriteExisting] = useState(true);
  const [copyUnconverted, setCopyUnconverted] = useState(true);

  const [batchQueue, setBatchQueue] = useState<BatchJob[]>([]);
  const [isBatchRunning, setIsBatchRunning] = useState(false);
  const [activeJobId, setActiveJobId] = useState<string | null>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const [logFilter, setLogFilter] = useState<"all" | "errors" | "success">("all");
  const [copiedLogs, setCopiedLogs] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const terminalEndRef = useRef<HTMLDivElement>(null);

  // Poll status every 800ms
  const { data: status, refetch: refetchStatus } = useQuery<Gen9ConversionStatus>({
    queryKey: ["gen9-status"],
    queryFn: () => api.getGen9Status(),
    refetchInterval: 800,
  });

  // Auto-scroll log terminal
  useEffect(() => {
    if (autoScroll && terminalEndRef.current) {
      terminalEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [status?.logs, autoScroll]);

  const convertMutation = useMutation({
    mutationFn: (req: Gen9ConversionRequest) => api.startGen9Conversion(req),
    onSuccess: () => {
      refetchStatus();
    },
    onError: (err: any) => {
      setErrorMsg(err.message);
      if (isBatchRunning && activeJobId) {
        setBatchQueue((prev) =>
          prev.map((j) => (j.id === activeJobId ? { ...j, status: "error" } : j))
        );
        setActiveJobId(null);
      }
    },
  });

  const stopMutation = useMutation({
    mutationFn: () => api.stopGen9Conversion(),
    onSuccess: () => {
      setIsBatchRunning(false);
      setActiveJobId(null);
      refetchStatus();
    },
  });

  // Batch runner effect
  useEffect(() => {
    if (!isBatchRunning) return;
    if (status?.isConverting) return;

    if (activeJobId) {
      // Mark current job completed or error
      const hadError = !!status?.error;
      setBatchQueue((prev) =>
        prev.map((j) =>
          j.id === activeJobId ? { ...j, status: hadError ? "error" : "completed" } : j
        )
      );
      setActiveJobId(null);
      return;
    }

    // Find next pending job
    const nextJob = batchQueue.find((j) => j.status === "pending");
    if (nextJob) {
      setActiveJobId(nextJob.id);
      setBatchQueue((prev) =>
        prev.map((j) => (j.id === nextJob.id ? { ...j, status: "processing" } : j))
      );
      convertMutation.mutate({
        inputPath: nextJob.inputPath,
        outputPath: nextJob.outputPath,
        preset: nextJob.preset,
        processSubfolders,
        overwriteExisting,
        copyUnconverted,
      });
    } else {
      setIsBatchRunning(false);
    }
  }, [isBatchRunning, status?.isConverting, activeJobId, batchQueue]);

  const handleSelectInput = async () => {
    try {
      const selected = await api.selectFolderNative("Sélectionner le dossier source des assets");
      if (selected) setInputFolder(selected);
    } catch { }
  };

  const handleSelectOutput = async () => {
    try {
      const selected = await api.selectFolderNative("Sélectionner le dossier de destination");
      if (selected) setOutputFolder(selected);
    } catch { }
  };

  const handleStartDirect = () => {
    if (!inputFolder.trim() || !outputFolder.trim()) {
      setErrorMsg("Veuillez spécifier à la fois le dossier source et le dossier de destination.");
      return;
    }
    setErrorMsg(null);
    convertMutation.mutate({
      inputPath: inputFolder.trim(),
      outputPath: outputFolder.trim(),
      preset,
      processSubfolders,
      overwriteExisting,
      copyUnconverted,
    });
  };

  const handleAddToBatch = () => {
    if (!inputFolder.trim() || !outputFolder.trim()) {
      setErrorMsg("Veuillez spécifier le dossier source et le dossier de destination avant d'ajouter à la file.");
      return;
    }
    setErrorMsg(null);
    setBatchQueue((prev) => [
      ...prev,
      {
        id: Math.random().toString(36).substring(2, 9),
        inputPath: inputFolder.trim(),
        outputPath: outputFolder.trim(),
        preset,
        status: "pending",
      },
    ]);
  };

  const handleStartBatch = () => {
    if (batchQueue.length === 0) return;
    setErrorMsg(null);
    setIsBatchRunning(true);
  };

  const handleRemoveBatchJob = (id: string) => {
    setBatchQueue((prev) => prev.filter((j) => j.id !== id));
  };

  const handleClearBatch = () => {
    if (isBatchRunning) {
      stopMutation.mutate();
    }
    setBatchQueue([]);
    setIsBatchRunning(false);
    setActiveJobId(null);
  };

  const handleCopyLogs = () => {
    if (!status?.logs) return;
    navigator.clipboard.writeText(status.logs.join("\n"));
    setCopiedLogs(true);
    setTimeout(() => setCopiedLogs(false), 2000);
  };

  const isConverting = status?.isConverting ?? false;

  // Filter logs
  const visibleLogs = (status?.logs || []).filter((line) => {
    if (logFilter === "errors") return line.includes("[ERREUR]");
    if (logFilter === "success") return line.includes("[SUCCÈS]");
    return true;
  });

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Header */}
      <div className="p-5 border-b border-[#222D42] bg-[#121824]/60 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
              CODEWALKER GEN9 SUITE
            </span>
            <span className="text-xs text-[#64748B] font-mono">PS5 / XBOX SERIES TO PC ASSETS</span>
          </div>
          <h1 className="text-xl font-bold text-white mt-1">Convertisseur d'Assets Gen9 Next-Gen</h1>
          <p className="text-xs text-[#94A3B8]">
            Conversion haute fidélité des conteneurs de shaders, textures YTD et modèles YDR/YDD/YFT.
          </p>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          {isConverting ? (
            <button
              onClick={() => stopMutation.mutate()}
              className="flex items-center gap-2 bg-red-600 hover:bg-red-500 text-white px-4 py-2 rounded-xl text-xs font-bold shadow-[0_0_15px_rgba(239,68,68,0.3)] transition-all cursor-pointer"
            >
              <Square className="w-4 h-4 fill-current" />
              <span>Arrêter la conversion</span>
            </button>
          ) : (
            <button
              onClick={handleStartDirect}
              className="flex items-center gap-2 bg-[#FF7A29] hover:bg-[#FF8F4D] text-white px-5 py-2.5 rounded-xl text-xs font-bold shadow-[0_0_20px_rgba(255,122,41,0.25)] transition-all cursor-pointer"
            >
              <Play className="w-4 h-4 fill-current" />
              <span>Lancer la conversion</span>
            </button>
          )}
        </div>
      </div>

      {errorMsg && (
        <div className="mx-6 mt-4 p-3.5 rounded-xl bg-red-950/40 border border-red-800/60 text-red-300 text-xs flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <AlertTriangle className="w-4 h-4 text-red-400 shrink-0" />
            <span>{errorMsg}</span>
          </div>
          <button onClick={() => setErrorMsg(null)} className="text-red-400 hover:text-red-200">
            &times;
          </button>
        </div>
      )}

      {/* Main Grid: Parameters Form (Left) & Realtime Logs Console (Right) */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-6 p-6 overflow-hidden">
        {/* Left Column: Form & Presets (5 cols) */}
        <div className="lg:col-span-5 flex flex-col space-y-4 overflow-y-auto pr-1 custom-scrollbar">
          {/* Path selection card */}
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-4 shadow-sm">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <FolderOpen className="w-3.5 h-3.5 text-[#FF7A29]" />
              <span>Répertoires Source & Cible</span>
            </h2>

            {/* Input Folder */}
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-[#94A3B8]">Dossier source des fichiers :</label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={inputFolder}
                  onChange={(e) => setInputFolder(e.target.value)}
                  placeholder="ex: C:\GTA5_Gen9_Dump\mods..."
                  className="flex-1 bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
                />
                <button
                  type="button"
                  onClick={handleSelectInput}
                  className="px-3 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer"
                >
                  Parcourir
                </button>
              </div>
            </div>

            {/* Output Folder */}
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-[#94A3B8]">Dossier de destination :</label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={outputFolder}
                  onChange={(e) => setOutputFolder(e.target.value)}
                  placeholder="ex: C:\GTA5_PC_Converted\output..."
                  className="flex-1 bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
                />
                <button
                  type="button"
                  onClick={handleSelectOutput}
                  className="px-3 py-2 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-white text-xs font-semibold transition-colors cursor-pointer"
                >
                  Parcourir
                </button>
              </div>
            </div>
          </div>

          {/* Preset Selection */}
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-3 shadow-sm">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <Settings2 className="w-3.5 h-3.5 text-[#FF7A29]" />
              <span>Preset de Conversion</span>
            </h2>

            <div className="space-y-2">
              {[
                {
                  id: "gen9_to_pc",
                  title: "Gen9 (PS5 / Xbox Series) → PC (Legacy)",
                  desc: "Reconstruit les conteneurs de shaders DX11 et extrait les mips DDS standards.",
                },
                {
                  id: "pc_to_gen9",
                  title: "PC (Legacy) → Gen9 (Next-Gen)",
                  desc: "Convertit les modèles et textures vers la structure étendue Gen9.",
                },
                {
                  id: "textures_only",
                  title: "Textures & Dictionnaires YTD uniquement",
                  desc: "Recalcule uniquement les en-têtes et le compression mipmap des textures.",
                },
              ].map((p) => (
                <div
                  key={p.id}
                  onClick={() => setPreset(p.id as any)}
                  className={cn(
                    "p-3 rounded-xl border transition-all cursor-pointer space-y-1",
                    preset === p.id
                      ? "bg-[#172030] border-[#FF7A29] shadow-[inset_0_1px_0_rgba(255,122,41,0.2)]"
                      : "bg-[#0A0E16]/70 border-[#222D42] hover:border-[#334360]"
                  )}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-white">{p.title}</span>
                    <input
                      type="radio"
                      checked={preset === p.id}
                      onChange={() => setPreset(p.id as any)}
                      className="accent-[#FF7A29]"
                    />
                  </div>
                  <p className="text-[11px] text-[#94A3B8] leading-tight">{p.desc}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Options Checkboxes */}
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-3 shadow-sm">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold">
              Options d'exécution
            </h2>

            <div className="space-y-2.5 text-xs text-[#F1F5F9]">
              <label className="flex items-center gap-2.5 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={processSubfolders}
                  onChange={(e) => setProcessSubfolders(e.target.checked)}
                  className="rounded border-[#222D42] text-[#FF7A29] focus:ring-0 accent-[#FF7A29] w-4 h-4 cursor-pointer"
                />
                <span>Traiter récursivement tous les sous-dossiers</span>
              </label>

              <label className="flex items-center gap-2.5 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={overwriteExisting}
                  onChange={(e) => setOverwriteExisting(e.target.checked)}
                  className="rounded border-[#222D42] text-[#FF7A29] focus:ring-0 accent-[#FF7A29] w-4 h-4 cursor-pointer"
                />
                <span>Écraser les fichiers déjà existants dans la destination</span>
              </label>

              <label className="flex items-center gap-2.5 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={copyUnconverted}
                  onChange={(e) => setCopyUnconverted(e.target.checked)}
                  className="rounded border-[#222D42] text-[#FF7A29] focus:ring-0 accent-[#FF7A29] w-4 h-4 cursor-pointer"
                />
                <span>Copier directement les fichiers ne nécessitant pas de conversion</span>
              </label>
            </div>

            <div className="pt-2 border-t border-[#222D42]">
              <button
                type="button"
                onClick={handleAddToBatch}
                className="w-full flex items-center justify-center gap-2 bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white py-2 rounded-xl text-xs font-semibold transition-colors cursor-pointer"
              >
                <ListPlus className="w-3.5 h-3.5" />
                <span>Ajouter à la file d'attente par lot ({batchQueue.length})</span>
              </button>

              {batchQueue.length > 0 && (
                <div className="space-y-2 pt-3 border-t border-[#222D42]/60 mt-2">
                  <div className="flex items-center justify-between">
                    <div className="text-[10px] font-mono text-[#64748B] font-semibold uppercase">
                      File d'attente ({batchQueue.length})
                    </div>
                    <div className="flex items-center gap-1.5">
                      <button
                        type="button"
                        onClick={handleStartBatch}
                        disabled={isBatchRunning || isConverting}
                        className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-[11px] font-bold transition-colors cursor-pointer disabled:opacity-50"
                      >
                        <Play className="w-3 h-3 fill-current" />
                        <span>{isBatchRunning ? "En cours..." : "Lancer le lot"}</span>
                      </button>
                      <button
                        type="button"
                        onClick={handleClearBatch}
                        className="p-1 rounded-lg text-[#64748B] hover:text-red-400 hover:bg-[#172030] transition-colors cursor-pointer"
                        title="Vider la file"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>

                  <div className="space-y-1 max-h-40 overflow-y-auto custom-scrollbar">
                    {batchQueue.map((job) => (
                      <div
                        key={job.id}
                        className={cn(
                          "p-2 rounded-xl border text-[11px] font-mono flex items-center justify-between gap-2",
                          job.status === "processing" && "bg-[#172030] border-[#FF7A29]/60",
                          job.status === "completed" && "bg-emerald-950/20 border-emerald-800/40",
                          job.status === "error" && "bg-red-950/20 border-red-800/40",
                          job.status === "pending" && "bg-[#0A0E16] border-[#222D42]"
                        )}
                      >
                        <div className="min-w-0 flex-1 truncate">
                          <div className="text-white truncate font-bold">
                            {job.inputPath.split(/[\\/]/).pop()}
                          </div>
                          <div className="text-[10px] text-[#64748B] truncate">
                            Preset: <span className="text-amber-400">{job.preset}</span>
                          </div>
                        </div>

                        <div className="flex items-center gap-2 shrink-0">
                          <span
                            className={cn(
                              "text-[9px] px-1.5 py-0.5 rounded font-bold uppercase",
                              job.status === "processing" && "bg-[#FF7A29]/20 text-[#FF7A29] animate-pulse",
                              job.status === "completed" && "bg-emerald-500/20 text-emerald-400",
                              job.status === "error" && "bg-red-500/20 text-red-400",
                              job.status === "pending" && "bg-slate-800 text-slate-400"
                            )}
                          >
                            {job.status}
                          </span>
                          {!isBatchRunning && (
                            <button
                              type="button"
                              onClick={() => handleRemoveBatchJob(job.id)}
                              className="text-[#64748B] hover:text-red-400 p-0.5"
                            >
                              <Trash2 className="w-3 h-3" />
                            </button>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Progress & Virtual Log Terminal (7 cols) */}
        <div className="lg:col-span-7 flex flex-col bg-[#121824] border border-[#222D42] rounded-2xl overflow-hidden shadow-xl">
          {/* Progress Section */}
          <div className="p-5 border-b border-[#222D42] bg-[#0A0E16]/50 space-y-3">
            <div className="flex items-center justify-between text-xs">
              <div className="flex items-center gap-2">
                <span className="text-[#64748B] font-mono">Progression :</span>
                <span className="font-bold text-white font-mono">
                  {Math.round((status?.progress ?? 0) * 100)}%
                </span>
                {status?.totalFiles ? (
                  <span className="text-[11px] text-[#94A3B8] font-mono">
                    ({status.processedFiles} / {status.totalFiles} fichiers)
                  </span>
                ) : null}
              </div>

              <div className="flex items-center gap-2 font-mono text-[11px]">
                <span
                  className={cn(
                    "inline-block w-2 h-2 rounded-full",
                    isConverting ? "bg-emerald-400 animate-pulse" : "bg-slate-600"
                  )}
                />
                <span className={isConverting ? "text-emerald-400" : "text-[#64748B]"}>
                  {isConverting ? "Conversion active" : "En attente"}
                </span>
              </div>
            </div>

            {/* Progress bar */}
            <div className="w-full bg-[#0A0E16] rounded-full h-2.5 overflow-hidden border border-[#222D42]">
              <div
                className="bg-gradient-to-r from-[#FF7A29] to-[#FFA266] h-full transition-all duration-200"
                style={{ width: `${Math.round((status?.progress ?? 0) * 100)}%` }}
              />
            </div>

            {/* Current file name */}
            <div className="text-[11px] text-[#94A3B8] font-mono truncate">
              Fichier en cours :{" "}
              <span className="text-white font-semibold">
                {status?.currentFile || "Aucun"}
              </span>
            </div>
          </div>

          {/* Terminal Controls Bar */}
          <div className="px-4 py-2.5 border-b border-[#222D42] bg-[#0E1420] flex items-center justify-between text-xs">
            <div className="flex items-center gap-2 text-[#94A3B8]">
              <Terminal className="w-3.5 h-3.5 text-[#FF7A29]" />
              <span className="font-mono text-[11px]">Console de conversion temps réel</span>
            </div>

            <div className="flex items-center gap-2">
              <div className="flex items-center bg-[#0A0E16] rounded-lg p-0.5 border border-[#222D42] text-[10px] font-mono">
                {(["all", "errors", "success"] as const).map((filterMode) => (
                  <button
                    key={filterMode}
                    onClick={() => setLogFilter(filterMode)}
                    className={cn(
                      "px-2 py-0.5 rounded capitalize transition-colors cursor-pointer",
                      logFilter === filterMode
                        ? "bg-[#FF7A29] text-white font-bold"
                        : "text-[#64748B] hover:text-[#94A3B8]"
                    )}
                  >
                    {filterMode === "all" ? "Tous" : filterMode === "errors" ? "Erreurs" : "Succès"}
                  </button>
                ))}
              </div>

              <button
                onClick={() => setAutoScroll(!autoScroll)}
                className={cn(
                  "px-2 py-1 rounded text-[10px] font-mono transition-colors cursor-pointer",
                  autoScroll ? "bg-[#FF7A29]/20 text-[#FF7A29]" : "text-[#64748B] hover:text-[#94A3B8]"
                )}
              >
                Auto-scroll {autoScroll ? "ON" : "OFF"}
              </button>

              <button
                onClick={handleCopyLogs}
                className="p-1 rounded text-[#64748B] hover:text-white transition-colors cursor-pointer"
                title="Copier les logs"
              >
                {copiedLogs ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
              </button>

              <button
                onClick={() => api.clearGen9Logs().then(() => refetchStatus())}
                className="p-1 rounded text-[#64748B] hover:text-red-400 transition-colors cursor-pointer"
                title="Vider la console"
              >
                <Trash2 className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>

          {/* Terminal Console Output */}
          <div className="flex-1 p-4 bg-[#0A0E16] font-mono text-[11px] overflow-y-auto space-y-1 custom-scrollbar select-text">
            {visibleLogs.length === 0 ? (
              <div className="text-[#64748B] italic py-8 text-center">
                Aucun journal de conversion disponible pour le moment. Lancez une tâche pour voir les logs.
              </div>
            ) : (
              visibleLogs.map((log, index) => {
                const isErr = log.includes("[ERREUR]") || log.includes("[ERREUR FATALE]");
                const isSuccess = log.includes("[SUCCÈS]") || log.includes("[TERMINÉ]");
                const isWarning = log.includes("[ARRÊTÉ]") || log.includes("[IGNORÉ]");

                return (
                  <div
                    key={index}
                    className={cn(
                      "leading-relaxed break-all",
                      isErr && "text-red-400",
                      isSuccess && "text-emerald-400",
                      isWarning && "text-amber-400",
                      !isErr && !isSuccess && !isWarning && "text-[#94A3B8]"
                    )}
                  >
                    {log}
                  </div>
                );
              })
            )}
            <div ref={terminalEndRef} />
          </div>
        </div>
      </div>
    </div>
  );
};
