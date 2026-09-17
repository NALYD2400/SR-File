import React, { useState, useEffect } from "react";
import { api } from "../../api/client";
import { JoaatResult, DictionaryItem } from "../../types";
import { cn } from "../../lib/utils";
import {
  Calculator,
  Search,
  BookOpen,
  Copy,
  Check,
  FileSpreadsheet,
  Sparkles,
  Layers
} from "lucide-react";

export const JenkinsTool: React.FC = () => {
  const [singleInput, setSingleInput] = useState<string>("prop_barrier_work05");
  const [encoding, setEncoding] = useState<"utf8" | "ascii">("utf8");
  const [hashResult, setHashResult] = useState<JoaatResult | null>(null);

  // Reverse lookup
  const [lookupQuery, setLookupQuery] = useState<string>("0x8D38D3FE");
  const [searchResults, setSearchResults] = useState<DictionaryItem[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  // Batch tool
  const [batchInput, setBatchInput] = useState<string>(
    "prop_weed_01\nvw_prop_vw_casino_door\nhei_prop_heist_weed_block\ncustom_vehicle\nweapons_pistol"
  );
  const [batchResults, setBatchResults] = useState<JoaatResult[]>([]);
  const [copiedBatch, setCopiedBatch] = useState(false);
  const [copiedSingle, setCopiedSingle] = useState(false);

  // Live single hash calculation
  useEffect(() => {
    if (!singleInput) {
      setHashResult(null);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await api.computeJoaat(singleInput, encoding);
        setHashResult(res);
      } catch { }
    }, 150);
    return () => clearTimeout(timer);
  }, [singleInput, encoding]);

  // Live dictionary search
  useEffect(() => {
    if (!lookupQuery.trim()) {
      setSearchResults([]);
      return;
    }
    const timer = setTimeout(async () => {
      setIsSearching(true);
      try {
        const res = await api.searchDictionary(lookupQuery.trim(), 20);
        setSearchResults(res);
      } catch { }
      finally {
        setIsSearching(false);
      }
    }, 250);
    return () => clearTimeout(timer);
  }, [lookupQuery]);

  const handleRunBatch = async () => {
    const lines = batchInput
      .split("\n")
      .map((l) => l.trim())
      .filter(Boolean);
    if (lines.length === 0) return;

    try {
      const results = await api.batchJoaat(lines, encoding);
      setBatchResults(results);
    } catch { }
  };

  const handleCopyBatchCsv = () => {
    if (batchResults.length === 0) return;
    const csv = ["Text,HashHex,HashUint,HashInt"]
      .concat(batchResults.map((r) => `"${r.text}",${r.hashHex},${r.hashUint},${r.hashInt}`))
      .join("\n");
    navigator.clipboard.writeText(csv);
    setCopiedBatch(true);
    setTimeout(() => setCopiedBatch(false), 2000);
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Header */}
      <div className="p-5 border-b border-[#222D42] bg-[#121824]/60 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-[#FF7A29]/15 text-[#FF7A29] border border-[#FF7A29]/30">
              HASHING UTILITIES
            </span>
            <span className="text-xs text-[#64748B] font-mono">JENKINS ONE-AT-A-TIME (JOAAT 32-BIT)</span>
          </div>
          <h1 className="text-xl font-bold text-white mt-1">Calculateur JOAAT & Dictionnaire Inverse</h1>
          <p className="text-xs text-[#94A3B8]">
            Résolution bidirectionnelle des hachages d'archétypes, textures, entités et shaders du moteur RAGE.
          </p>
        </div>

        {/* Encoding Selector */}
        <div className="flex items-center gap-2 bg-[#121824] p-1.5 rounded-xl border border-[#222D42] text-xs font-mono">
          <span className="text-[#64748B] px-2">Encodage :</span>
          <button
            onClick={() => setEncoding("utf8")}
            className={cn(
              "px-3 py-1 rounded-lg font-semibold transition-colors cursor-pointer",
              encoding === "utf8" ? "bg-[#FF7A29] text-white" : "text-[#94A3B8] hover:text-white"
            )}
          >
            UTF-8
          </button>
          <button
            onClick={() => setEncoding("ascii")}
            className={cn(
              "px-3 py-1 rounded-lg font-semibold transition-colors cursor-pointer",
              encoding === "ascii" ? "bg-[#FF7A29] text-white" : "text-[#94A3B8] hover:text-white"
            )}
          >
            ASCII
          </button>
        </div>
      </div>

      {/* Main Grid: 2 Columns */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-6 p-6 overflow-hidden">
        {/* Left Column: Live Calculator & Reverse Lookup (6 cols) */}
        <div className="lg:col-span-6 flex flex-col space-y-6 overflow-y-auto pr-1 custom-scrollbar">
          {/* Card 1: Live Calculator */}
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-4 shadow-sm">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <Calculator className="w-4 h-4 text-[#FF7A29]" />
              <span>Calculateur Instantané</span>
            </h2>

            <div className="space-y-1.5">
              <label className="text-xs font-medium text-[#94A3B8]">Chaîne de caractères à hacher :</label>
              <input
                type="text"
                value={singleInput}
                onChange={(e) => setSingleInput(e.target.value)}
                placeholder="Entrez une chaîne (ex: prop_barrier_work05)..."
                className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-4 py-2.5 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
              />
            </div>

            {/* Results Grid */}
            {hashResult && (
              <div className="grid grid-cols-2 gap-3 pt-2">
                <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                  <span className="text-[10px] font-mono text-[#64748B]">HEXADÉCIMAL (0x)</span>
                  <div className="flex items-center justify-between font-mono text-sm font-bold text-amber-400">
                    <span>{hashResult.hashHex}</span>
                    <button
                      onClick={() => {
                        navigator.clipboard.writeText(hashResult.hashHex);
                        setCopiedSingle(true);
                        setTimeout(() => setCopiedSingle(false), 1500);
                      }}
                      className="text-[#64748B] hover:text-white cursor-pointer"
                    >
                      {copiedSingle ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                    </button>
                  </div>
                </div>

                <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1">
                  <span className="text-[10px] font-mono text-[#64748B]">NON SIGNÉ (UInt32)</span>
                  <div className="font-mono text-sm font-bold text-emerald-400">
                    {hashResult.hashUint}
                  </div>
                </div>

                <div className="p-3 rounded-xl bg-[#0A0E16] border border-[#222D42] space-y-1 col-span-2">
                  <span className="text-[10px] font-mono text-[#64748B]">ENTIER SIGNÉ (Int32)</span>
                  <div className="font-mono text-xs font-bold text-sky-400">
                    {hashResult.hashInt}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Card 2: Reverse Dictionary Lookup */}
          <div className="p-5 rounded-2xl bg-[#121824] border border-[#222D42] space-y-4 shadow-sm flex-1 flex flex-col">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <BookOpen className="w-4 h-4 text-[#FF7A29]" />
              <span>Dictionnaire Inverse (Recherche par Hash ou Mot)</span>
            </h2>

            <div className="space-y-1.5">
              <label className="text-xs font-medium text-[#94A3B8]">Recherche (Hex 0x..., UInt32 ou Texte) :</label>
              <div className="relative">
                <Search className="w-4 h-4 text-[#64748B] absolute left-3 top-3" />
                <input
                  type="text"
                  value={lookupQuery}
                  onChange={(e) => setLookupQuery(e.target.value)}
                  placeholder="ex: 0x8D38D3FE ou barrier..."
                  className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl pl-9 pr-4 py-2.5 text-xs text-white placeholder-[#64748B] font-mono transition-colors"
                />
              </div>
            </div>

            {/* Results Table */}
            <div className="flex-1 overflow-y-auto max-h-60 rounded-xl bg-[#0A0E16] border border-[#222D42] p-2 space-y-1 custom-scrollbar">
              {searchResults.length === 0 ? (
                <div className="text-center text-xs text-[#64748B] py-8">
                  {isSearching ? "Recherche dans le dictionnaire..." : "Aucun hash correspondant trouvé."}
                </div>
              ) : (
                searchResults.map((item, idx) => (
                  <div
                    key={idx}
                    onClick={() => setSingleInput(item.text)}
                    className="flex items-center justify-between p-2 rounded-lg hover:bg-[#172030] cursor-pointer transition-colors text-xs font-mono"
                  >
                    <span className="text-white font-bold">{item.text}</span>
                    <span className="text-amber-400 font-semibold">{item.hashHex}</span>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Batch Hashing Tool (6 cols) */}
        <div className="lg:col-span-6 flex flex-col bg-[#121824] border border-[#222D42] rounded-2xl overflow-hidden p-5 space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-[#222D42]">
            <h2 className="text-xs font-mono uppercase tracking-wider text-[#64748B] font-semibold flex items-center gap-2">
              <Layers className="w-4 h-4 text-[#FF7A29]" />
              <span>Générateur de Hachage par Lot</span>
            </h2>

            <div className="flex items-center gap-2">
              <button
                onClick={handleRunBatch}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-all shadow-sm cursor-pointer"
              >
                <Sparkles className="w-3.5 h-3.5" />
                <span>Calculer le lot</span>
              </button>

              {batchResults.length > 0 && (
                <button
                  onClick={handleCopyBatchCsv}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
                  title="Exporter au format CSV"
                >
                  {copiedBatch ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <FileSpreadsheet className="w-3.5 h-3.5" />}
                  <span>CSV</span>
                </button>
              )}
            </div>
          </div>

          {/* Textarea for batch lines */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-[#94A3B8]">
              Une entrée par ligne :
            </label>
            <textarea
              value={batchInput}
              onChange={(e) => setBatchInput(e.target.value)}
              rows={5}
              className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl p-3 text-xs text-white font-mono resize-none leading-relaxed custom-scrollbar"
            />
          </div>

          {/* Results Table */}
          <div className="flex-1 overflow-y-auto rounded-xl bg-[#0A0E16] border border-[#222D42] custom-scrollbar">
            {batchResults.length === 0 ? (
              <div className="text-center text-xs text-[#64748B] py-16">
                Cliquez sur "Calculer le lot" pour afficher le tableau des hachages.
              </div>
            ) : (
              <table className="w-full text-left text-xs font-mono">
                <thead className="bg-[#121824] text-[#64748B] text-[11px] sticky top-0 border-b border-[#222D42]">
                  <tr>
                    <th className="p-3">CHAÎNE D'ORIGINE</th>
                    <th className="p-3">HEX (0x)</th>
                    <th className="p-3">UINT32</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#222D42]/60">
                  {batchResults.map((item, index) => (
                    <tr key={index} className="hover:bg-[#121824]/50 transition-colors">
                      <td className="p-3 text-white font-semibold">{item.text}</td>
                      <td className="p-3 text-amber-400 font-bold">{item.hashHex}</td>
                      <td className="p-3 text-emerald-400">{item.hashUint}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
