import React, { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "../../api/client";
import { ProjectSummary, YmapEntity, Archetype, Vector4 } from "../../types";
import { cn } from "../../lib/utils";
import {
  FolderTree,
  Map,
  Plus,
  Trash2,
  Download,
  Upload,
  Box,
  Layers,
  FileCode,
  Search,
  ChevronRight,
  FolderOpen,
  Save,
  FilePlus,
} from "lucide-react";

// Euler <-> Quaternion helper in frontend
function eulerToQuat(pitch: number, roll: number, yaw: number): Vector4 {
  const p = (pitch * Math.PI) / 360.0;
  const r = (roll * Math.PI) / 360.0;
  const y = (yaw * Math.PI) / 360.0;

  const cp = Math.cos(p);
  const sp = Math.sin(p);
  const cr = Math.cos(r);
  const sr = Math.sin(r);
  const cy = Math.cos(y);
  const sy = Math.sin(y);

  return {
    w: Number((cp * cr * cy + sp * sr * sy).toFixed(6)),
    x: Number((sp * cr * cy - cp * sr * sy).toFixed(6)),
    y: Number((cp * sr * cy + sp * cr * sy).toFixed(6)),
    z: Number((cp * cr * sy - sp * sr * cy).toFixed(6)),
  };
}

export const ProjectEditor: React.FC = () => {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<"entities" | "archetypes" | "manifest">("entities");
  const [selectedEntityIdx, setSelectedEntityIdx] = useState<number>(0);
  const [selectedArchetypeIdx, setSelectedArchetypeIdx] = useState<number>(0);
  const [searchQuery, setSearchQuery] = useState("");
  const [exportModalXml, setExportModalXml] = useState<string | null>(null);
  const [importModalOpen, setImportModalOpen] = useState(false);
  const [importXmlText, setImportXmlText] = useState("");
  const [statusMsg, setStatusMsg] = useState<string | null>(null);

  // Queries
  const { data: project } = useQuery<ProjectSummary>({
    queryKey: ["current-project"],
    queryFn: () => api.getCurrentProject(),
  });

  const { data: entities = [] } = useQuery<YmapEntity[]>({
    queryKey: ["project-entities"],
    queryFn: () => api.getEntities(),
  });

  const { data: archetypes = [] } = useQuery<Archetype[]>({
    queryKey: ["project-archetypes"],
    queryFn: () => api.getArchetypes(),
  });

  // Mutations
  const updateEntityMutation = useMutation({
    mutationFn: ({ index, entity }: { index: number; entity: YmapEntity }) =>
      api.updateEntity(index, entity),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["project-entities"] });
    },
  });

  const deleteEntityMutation = useMutation({
    mutationFn: (index: number) => api.deleteEntity(index),
    onSuccess: () => {
      setSelectedEntityIdx(Math.max(0, selectedEntityIdx - 1));
      queryClient.invalidateQueries({ queryKey: ["project-entities"] });
    },
  });

  const selectedEntity = entities[selectedEntityIdx];
  const selectedArchetype = archetypes[selectedArchetypeIdx];

  const handlePositionChange = (axis: "x" | "y" | "z", val: number) => {
    if (!selectedEntity) return;
    const updated = {
      ...selectedEntity,
      position: {
        ...selectedEntity.position,
        [axis]: val,
      },
    };
    updateEntityMutation.mutate({ index: selectedEntityIdx, entity: updated });
  };

  const handleEulerChange = (axis: "x" | "y" | "z", val: number) => {
    if (!selectedEntity) return;
    const newEuler = {
      ...selectedEntity.eulerRotation,
      [axis]: val,
    };
    const newQuat = eulerToQuat(newEuler.x, newEuler.y, newEuler.z);
    const updated = {
      ...selectedEntity,
      eulerRotation: newEuler,
      rotation: newQuat,
    };
    updateEntityMutation.mutate({ index: selectedEntityIdx, entity: updated });
  };

  const handleAddEntity = () => {
    const newEnt: YmapEntity = {
      name: `entity_${entities.length + 1}`,
      archetypeName: "prop_barrier_work05",
      position: { x: 0, y: 0, z: 0 },
      rotation: { x: 0, y: 0, z: 0, w: 1 },
      eulerRotation: { x: 0, y: 0, z: 0 },
      lodDist: 150,
      childLodDist: 0,
      flags: 32,
      guid: entities.length + 1000,
    };
    updateEntityMutation.mutate({ index: entities.length, entity: newEnt });
    setSelectedEntityIdx(entities.length);
  };

  const handleNewProject = async () => {
    const name = prompt("Nom du nouveau projet GTA V:", "Mon Nouveau Projet GTA V");
    if (!name?.trim()) return;
    try {
      await api.createProject(name.trim());
      queryClient.invalidateQueries({ queryKey: ["current-project"] });
      queryClient.invalidateQueries({ queryKey: ["project-entities"] });
      queryClient.invalidateQueries({ queryKey: ["project-archetypes"] });
      setSelectedEntityIdx(0);
      setStatusMsg(`Projet "${name.trim()}" créé avec succès !`);
      setTimeout(() => setStatusMsg(null), 3000);
    } catch (err: any) {
      alert("Erreur lors de la création du projet: " + err.message);
    }
  };

  const handleOpenProject = async () => {
    try {
      const selected = await api.selectFileNative(
        "Ouvrir un projet CodeWalker/SRFile",
        "Projets CodeWalker (*.cwproj)",
        ["cwproj"]
      );
      if (selected) {
        await api.openProject(selected);
        queryClient.invalidateQueries({ queryKey: ["current-project"] });
        queryClient.invalidateQueries({ queryKey: ["project-entities"] });
        queryClient.invalidateQueries({ queryKey: ["project-archetypes"] });
        setSelectedEntityIdx(0);
        setStatusMsg(`Projet chargé: ${selected}`);
        setTimeout(() => setStatusMsg(null), 3000);
      }
    } catch (err: any) {
      alert("Erreur lors de l'ouverture du projet: " + err.message);
    }
  };

  const handleSaveProject = async () => {
    try {
      const projPath = project?.filepath || prompt("Chemin de sauvegarde du fichier .cwproj:", "C:\\GTA5_Modding\\MonProjet.cwproj");
      if (!projPath?.trim()) return;

      await api.saveProject({
        filePath: projPath.trim(),
        name: project?.name || "Projet GTA V",
        version: project?.version || 1,
        ymapFiles: project?.ymapFiles || ["custom_map.ymap"],
        ytypFiles: project?.ytypFiles || ["custom_types.ytyp"],
        ybnFiles: project?.ybnFiles || [],
      });
      queryClient.invalidateQueries({ queryKey: ["current-project"] });
      setStatusMsg(`Projet enregistré: ${projPath.trim()}`);
      setTimeout(() => setStatusMsg(null), 3000);
    } catch (err: any) {
      alert("Erreur lors de l'enregistrement du projet: " + err.message);
    }
  };

  const handleBrowseXmlFile = async () => {
    try {
      const selected = await api.selectFileNative(
        "Sélectionner un fichier YMAP XML",
        "Fichiers XML / YMAP (*.xml; *.ymap)",
        ["xml", "ymap"]
      );
      if (selected) {
        const res = await api.readFileText(selected);
        setImportXmlText(res.content);
        setStatusMsg(`Fichier chargé: ${selected}`);
        setTimeout(() => setStatusMsg(null), 3000);
      }
    } catch (err: any) {
      alert("Erreur de lecture du fichier XML: " + err.message);
    }
  };

  const handleExportXml = async () => {
    try {
      const res = await api.exportYmapXml(project?.name || "custom_map", entities);
      setExportModalXml(res.xml);
    } catch (err: any) {
      alert("Erreur lors de l'export XML: " + err.message);
    }
  };

  const handleSaveExportXml = async () => {
    if (!exportModalXml) return;
    const defaultName = `${project?.name || "custom_map"}.ymap.xml`;
    // Download as file
    const blob = new Blob([exportModalXml], { type: "text/xml;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = defaultName;
    link.click();
    URL.revokeObjectURL(url);
    setStatusMsg(`Fichier ${defaultName} téléchargé avec succès !`);
    setExportModalXml(null);
    setTimeout(() => setStatusMsg(null), 3000);
  };

  const handleImportXml = async () => {
    if (!importXmlText.trim()) return;
    try {
      await api.importYmapXml(importXmlText);
      queryClient.invalidateQueries({ queryKey: ["project-entities"] });
      setImportModalOpen(false);
      setImportXmlText("");
      setStatusMsg("YMAP XML importé avec succès !");
      setTimeout(() => setStatusMsg(null), 3000);
    } catch (err: any) {
      alert("Erreur d'importation XML: " + err.message);
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0A0E16] text-[#F1F5F9] overflow-hidden">
      {/* Top Header & Actions Bar */}
      <div className="p-4 border-b border-[#222D42] bg-[#121824] flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-xl bg-[#FF7A29]/15 text-[#FF7A29]">
            <Map className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-sm font-bold text-white font-mono">
                {project?.name || "Projet GTA V"}
              </h1>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[#222D42] text-[#94A3B8]">
                v{project?.version || 1}.0
              </span>
              {project?.filepath && (
                <span className="text-[10px] font-mono text-[#64748B] truncate max-w-xs">
                  ({project.filepath})
                </span>
              )}
            </div>
            <p className="text-xs text-[#94A3B8]">
              Éditeur de maps, inspecteur d'entités vectoriel & conversion bidirectionnelle YMAP XML.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleNewProject}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
            title="Nouveau projet CodeWalker"
          >
            <FilePlus className="w-3.5 h-3.5 text-[#FF7A29]" />
            <span>Nouveau</span>
          </button>

          <button
            onClick={handleOpenProject}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
            title="Ouvrir un projet .cwproj existant"
          >
            <FolderOpen className="w-3.5 h-3.5 text-sky-400" />
            <span>Ouvrir</span>
          </button>

          <button
            onClick={handleSaveProject}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
            title="Enregistrer le projet .cwproj"
          >
            <Save className="w-3.5 h-3.5 text-emerald-400" />
            <span>Enregistrer</span>
          </button>

          <div className="w-[1px] h-6 bg-[#222D42] mx-1" />

          <button
            onClick={() => setImportModalOpen(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-[#94A3B8] hover:text-white text-xs font-semibold transition-colors cursor-pointer"
          >
            <Upload className="w-3.5 h-3.5 text-amber-400" />
            <span>Importer XML</span>
          </button>

          <button
            onClick={handleExportXml}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold transition-all shadow-[0_0_15px_rgba(255,122,41,0.25)] cursor-pointer"
          >
            <Download className="w-3.5 h-3.5" />
            <span>Exporter YMAP XML</span>
          </button>
        </div>
      </div>

      {statusMsg && (
        <div className="bg-emerald-950/40 border-b border-emerald-800/60 text-emerald-300 text-xs px-4 py-1.5 font-mono">
          {statusMsg}
        </div>
      )}

      {/* Main 3-Column Layout: Project Tree (Left), List (Middle), Blender Inspector (Right) */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left Column: Project Explorer Tree */}
        <div className="w-56 border-r border-[#222D42] bg-[#0E1420] p-4 flex flex-col space-y-4 shrink-0">
          <div className="text-[10px] font-mono uppercase tracking-wider text-[#64748B] font-semibold">
            Explorateur de Projet
          </div>

          <div className="space-y-1 text-xs font-mono">
            <button
              onClick={() => setActiveTab("entities")}
              className={cn(
                "w-full flex items-center justify-between px-3 py-2 rounded-xl transition-colors cursor-pointer",
                activeTab === "entities"
                  ? "bg-[#172030] text-[#FF7A29] font-bold border border-[#222D42]"
                  : "text-[#94A3B8] hover:text-white hover:bg-[#121824]"
              )}
            >
              <div className="flex items-center gap-2">
                <Box className="w-4 h-4" />
                <span>Entités ({entities.length})</span>
              </div>
            </button>

            <button
              onClick={() => setActiveTab("archetypes")}
              className={cn(
                "w-full flex items-center justify-between px-3 py-2 rounded-xl transition-colors cursor-pointer",
                activeTab === "archetypes"
                  ? "bg-[#172030] text-[#FF7A29] font-bold border border-[#222D42]"
                  : "text-[#94A3B8] hover:text-white hover:bg-[#121824]"
              )}
            >
              <div className="flex items-center gap-2">
                <Layers className="w-4 h-4" />
                <span>Archétypes ({archetypes.length})</span>
              </div>
            </button>

            <button
              onClick={() => setActiveTab("manifest")}
              className={cn(
                "w-full flex items-center justify-between px-3 py-2 rounded-xl transition-colors cursor-pointer",
                activeTab === "manifest"
                  ? "bg-[#172030] text-[#FF7A29] font-bold border border-[#222D42]"
                  : "text-[#94A3B8] hover:text-white hover:bg-[#121824]"
              )}
            >
              <div className="flex items-center gap-2">
                <FolderTree className="w-4 h-4" />
                <span>Fichiers (.cwproj)</span>
              </div>
            </button>
          </div>

          <div className="pt-4 border-t border-[#222D42]/60 space-y-2 text-[11px] font-mono text-[#64748B]">
            <div>YMAPs: {project?.ymapFiles?.length || 1}</div>
            <div>YTYPs: {project?.ytypFiles?.length || 1}</div>
            <div>Collisions: {project?.ybnFiles?.length || 0}</div>
          </div>
        </div>

        {/* Middle Column: Items List */}
        <div className="w-72 border-r border-[#222D42] bg-[#121824] flex flex-col overflow-hidden shrink-0">
          <div className="p-3 border-b border-[#222D42] flex items-center justify-between gap-2">
            <div className="relative flex-1">
              <Search className="w-3.5 h-3.5 text-[#64748B] absolute left-2.5 top-2.5" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Filtrer..."
                className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-lg pl-8 pr-3 py-1 text-xs text-white placeholder-[#64748B]"
              />
            </div>

            {activeTab === "entities" && (
              <button
                onClick={handleAddEntity}
                className="p-1.5 rounded-lg bg-[#FF7A29] hover:bg-[#FF8F4D] text-white transition-colors cursor-pointer"
                title="Ajouter une entité"
              >
                <Plus className="w-3.5 h-3.5" />
              </button>
            )}
          </div>

          <div className="flex-1 overflow-y-auto p-2 space-y-1 custom-scrollbar">
            {activeTab === "entities" ? (
              entities.map((ent, idx) => (
                <div
                  key={idx}
                  onClick={() => setSelectedEntityIdx(idx)}
                  className={cn(
                    "p-2.5 rounded-xl border transition-all cursor-pointer flex items-center justify-between text-xs font-mono",
                    selectedEntityIdx === idx
                      ? "bg-[#172030] border-[#FF7A29] text-white shadow-sm"
                      : "bg-[#0A0E16]/40 border-transparent hover:bg-[#172030]/50 text-[#94A3B8]"
                  )}
                >
                  <div className="truncate">
                    <div className="font-bold text-white truncate">{ent.name}</div>
                    <div className="text-[10px] text-[#64748B] truncate">{ent.archetypeName}</div>
                  </div>
                  <ChevronRight className="w-3.5 h-3.5 text-[#64748B]" />
                </div>
              ))
            ) : activeTab === "archetypes" ? (
              archetypes.map((arch, idx) => (
                <div
                  key={idx}
                  onClick={() => setSelectedArchetypeIdx(idx)}
                  className={cn(
                    "p-2.5 rounded-xl border transition-all cursor-pointer flex items-center justify-between text-xs font-mono",
                    selectedArchetypeIdx === idx
                      ? "bg-[#172030] border-[#FF7A29] text-white shadow-sm"
                      : "bg-[#0A0E16]/40 border-transparent hover:bg-[#172030]/50 text-[#94A3B8]"
                  )}
                >
                  <div className="truncate">
                    <div className="font-bold text-white truncate">{arch.name}</div>
                    <div className="text-[10px] text-[#64748B] truncate">{arch.textureDictionary}</div>
                  </div>
                  <ChevronRight className="w-3.5 h-3.5 text-[#64748B]" />
                </div>
              ))
            ) : (
              <div className="p-3 text-xs space-y-2 text-[#94A3B8] font-mono">
                <div className="font-bold text-white">Archives associées :</div>
                {(project?.ymapFiles || []).map((f, i) => (
                  <div key={i}>• {f}</div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Right Column: Blender-Style Property Inspector */}
        <div className="flex-1 bg-[#0A0E16] flex flex-col overflow-y-auto p-6 space-y-6 custom-scrollbar">
          {activeTab === "entities" && selectedEntity ? (
            <div className="max-w-xl space-y-6">
              {/* Entity Title & Header */}
              <div className="flex items-center justify-between pb-4 border-b border-[#222D42]">
                <div>
                  <span className="text-[10px] font-mono text-[#64748B] uppercase">Entité CEntityDef</span>
                  <h2 className="text-base font-bold text-white font-mono">{selectedEntity.name}</h2>
                </div>

                <button
                  onClick={() => deleteEntityMutation.mutate(selectedEntityIdx)}
                  className="p-2 rounded-xl bg-red-950/30 hover:bg-red-900/50 border border-red-800/40 text-red-300 transition-colors cursor-pointer"
                  title="Supprimer cette entité"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>

              {/* Archetype Name */}
              <div className="space-y-1.5">
                <label className="text-xs font-mono text-[#94A3B8]">Nom de l'archétype (ArchetypeName) :</label>
                <input
                  type="text"
                  value={selectedEntity.archetypeName}
                  onChange={(e) => {
                    const updated = { ...selectedEntity, archetypeName: e.target.value };
                    updateEntityMutation.mutate({ index: selectedEntityIdx, entity: updated });
                  }}
                  className="w-full bg-[#121824] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white font-mono"
                />
              </div>

              {/* Blender-Style Vector Input: Position (X, Y, Z) */}
              <div className="p-4 rounded-2xl bg-[#121824] border border-[#222D42] space-y-3">
                <div className="text-xs font-mono font-semibold text-white uppercase tracking-wider">
                  Position Vector3 (Monde GTA V)
                </div>

                <div className="grid grid-cols-3 gap-3">
                  {/* X Axis (Red) */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-rose-500/20 text-rose-400 border border-rose-500/30">
                        X
                      </span>
                      <span className="text-[#64748B]">Mètres</span>
                    </div>
                    <input
                      type="number"
                      step="0.01"
                      value={selectedEntity.position.x}
                      onChange={(e) => handlePositionChange("x", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-rose-500/30 focus:border-rose-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>

                  {/* Y Axis (Green) */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                        Y
                      </span>
                      <span className="text-[#64748B]">Mètres</span>
                    </div>
                    <input
                      type="number"
                      step="0.01"
                      value={selectedEntity.position.y}
                      onChange={(e) => handlePositionChange("y", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-emerald-500/30 focus:border-emerald-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>

                  {/* Z Axis (Blue) */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-sky-500/20 text-sky-400 border border-sky-500/30">
                        Z
                      </span>
                      <span className="text-[#64748B]">Mètres</span>
                    </div>
                    <input
                      type="number"
                      step="0.01"
                      value={selectedEntity.position.z}
                      onChange={(e) => handlePositionChange("z", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-sky-500/30 focus:border-sky-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>
                </div>
              </div>

              {/* Blender-Style Vector Input: Euler Rotation (Pitch, Roll, Yaw in Degrees) */}
              <div className="p-4 rounded-2xl bg-[#121824] border border-[#222D42] space-y-3">
                <div className="flex items-center justify-between">
                  <div className="text-xs font-mono font-semibold text-white uppercase tracking-wider">
                    Rotation Euler (Degrés)
                  </div>
                  <span className="text-[10px] font-mono text-[#64748B]">Synchro Quaternion 4D</span>
                </div>

                <div className="grid grid-cols-3 gap-3">
                  {/* Pitch / X */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-rose-500/20 text-rose-400 border border-rose-500/30">
                        Pitch (X)
                      </span>
                      <span className="text-[#64748B]">°</span>
                    </div>
                    <input
                      type="number"
                      step="1"
                      value={selectedEntity.eulerRotation.x}
                      onChange={(e) => handleEulerChange("x", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-rose-500/30 focus:border-rose-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>

                  {/* Roll / Y */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                        Roll (Y)
                      </span>
                      <span className="text-[#64748B]">°</span>
                    </div>
                    <input
                      type="number"
                      step="1"
                      value={selectedEntity.eulerRotation.y}
                      onChange={(e) => handleEulerChange("y", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-emerald-500/30 focus:border-emerald-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>

                  {/* Yaw / Z */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className="px-1.5 py-0.2 rounded font-bold bg-sky-500/20 text-sky-400 border border-sky-500/30">
                        Yaw (Z)
                      </span>
                      <span className="text-[#64748B]">°</span>
                    </div>
                    <input
                      type="number"
                      step="1"
                      value={selectedEntity.eulerRotation.z}
                      onChange={(e) => handleEulerChange("z", parseFloat(e.target.value) || 0)}
                      className="w-full bg-[#0A0E16] border border-sky-500/30 focus:border-sky-500 focus:outline-none rounded-xl px-3 py-1.5 text-xs text-white font-mono text-center"
                    />
                  </div>
                </div>

                {/* Internal Quaternion Representation */}
                <div className="pt-2 border-t border-[#222D42]/60 text-[10px] font-mono text-[#64748B] flex justify-between">
                  <span>Quaternion :</span>
                  <span>
                    ({selectedEntity.rotation.x}, {selectedEntity.rotation.y},{" "}
                    {selectedEntity.rotation.z}, {selectedEntity.rotation.w})
                  </span>
                </div>
              </div>

              {/* LOD Distances & Flags */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <label className="text-xs font-mono text-[#94A3B8]">Distance LOD (lodDist) :</label>
                  <input
                    type="number"
                    value={selectedEntity.lodDist}
                    onChange={(e) => {
                      const updated = { ...selectedEntity, lodDist: parseFloat(e.target.value) || 0 };
                      updateEntityMutation.mutate({ index: selectedEntityIdx, entity: updated });
                    }}
                    className="w-full bg-[#121824] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white font-mono"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-mono text-[#94A3B8]">Drapeaux (Flags) :</label>
                  <input
                    type="number"
                    value={selectedEntity.flags}
                    onChange={(e) => {
                      const updated = { ...selectedEntity, flags: parseInt(e.target.value) || 0 };
                      updateEntityMutation.mutate({ index: selectedEntityIdx, entity: updated });
                    }}
                    className="w-full bg-[#121824] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl px-3.5 py-2 text-xs text-white font-mono"
                  />
                </div>
              </div>
            </div>
          ) : activeTab === "archetypes" && selectedArchetype ? (
            <div className="max-w-xl space-y-4">
              <h2 className="text-base font-bold text-white font-mono pb-3 border-b border-[#222D42]">
                {selectedArchetype.name}
              </h2>

              <div className="grid grid-cols-2 gap-4 text-xs font-mono">
                <div className="space-y-1">
                  <span className="text-[#94A3B8]">Texture Dictionary :</span>
                  <div className="p-2.5 rounded-xl bg-[#121824] border border-[#222D42] text-white">
                    {selectedArchetype.textureDictionary}
                  </div>
                </div>

                <div className="space-y-1">
                  <span className="text-[#94A3B8]">Physics Dictionary :</span>
                  <div className="p-2.5 rounded-xl bg-[#121824] border border-[#222D42] text-white">
                    {selectedArchetype.physicsDictionary}
                  </div>
                </div>

                <div className="space-y-1">
                  <span className="text-[#94A3B8]">LOD Distance :</span>
                  <div className="p-2.5 rounded-xl bg-[#121824] border border-[#222D42] text-white">
                    {selectedArchetype.lodDist}m
                  </div>
                </div>

                <div className="space-y-1">
                  <span className="text-[#94A3B8]">Bounding Sphere Radius :</span>
                  <div className="p-2.5 rounded-xl bg-[#121824] border border-[#222D42] text-white">
                    {selectedArchetype.bsRadius}m
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center text-[#64748B] text-xs py-16">
              Sélectionnez un élément pour afficher ses propriétés dans l'inspecteur.
            </div>
          )}
        </div>
      </div>

      {/* Export XML Preview Modal */}
      {exportModalXml && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-6">
          <div className="bg-[#121824] border border-[#222D42] rounded-2xl w-full max-w-2xl max-h-[85vh] flex flex-col overflow-hidden shadow-2xl">
            <div className="p-4 border-b border-[#222D42] flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FileCode className="w-4 h-4 text-[#FF7A29]" />
                <h3 className="text-xs font-bold text-white font-mono">Aperçu YMAP XML Exporté</h3>
              </div>
              <button
                onClick={() => setExportModalXml(null)}
                className="text-[#94A3B8] hover:text-white"
              >
                &times;
              </button>
            </div>
            <pre className="flex-1 p-4 bg-[#0A0E16] font-mono text-[10px] text-emerald-400 overflow-auto custom-scrollbar select-all">
              {exportModalXml}
            </pre>
            <div className="p-3 border-t border-[#222D42] flex justify-end gap-2">
              <button
                type="button"
                onClick={handleSaveExportXml}
                className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs text-white font-semibold cursor-pointer"
              >
                <Download className="w-3.5 h-3.5 text-[#FF7A29]" />
                <span>Enregistrer sous...</span>
              </button>
              <button
                onClick={() => {
                  navigator.clipboard.writeText(exportModalXml);
                  setStatusMsg("XML copié dans le presse-papier !");
                  setExportModalXml(null);
                  setTimeout(() => setStatusMsg(null), 3000);
                }}
                className="px-4 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold cursor-pointer"
              >
                Copier le XML
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Import XML Modal */}
      {importModalOpen && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-6">
          <div className="bg-[#121824] border border-[#222D42] rounded-2xl w-full max-w-2xl flex flex-col overflow-hidden shadow-2xl">
            <div className="p-4 border-b border-[#222D42] flex items-center justify-between">
              <h3 className="text-xs font-bold text-white font-mono">Importer un fichier YMAP XML</h3>
              <button onClick={() => setImportModalOpen(false)} className="text-[#94A3B8] hover:text-white">
                &times;
              </button>
            </div>
            <div className="p-4 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-xs text-[#94A3B8]">Collez le contenu XML ou sélectionnez un fichier .ymap.xml :</p>
                <button
                  type="button"
                  onClick={handleBrowseXmlFile}
                  className="flex items-center gap-1.5 px-3 py-1 rounded-xl bg-[#172030] hover:bg-[#1E283D] border border-[#222D42] text-xs text-[#FF7A29] font-mono cursor-pointer"
                >
                  <FolderOpen className="w-3 h-3" />
                  <span>Parcourir un fichier...</span>
                </button>
              </div>
              <textarea
                value={importXmlText}
                onChange={(e) => setImportXmlText(e.target.value)}
                rows={10}
                placeholder="<CMapData>..."
                className="w-full bg-[#0A0E16] border border-[#222D42] focus:border-[#FF7A29] focus:outline-none rounded-xl p-3 text-xs text-white font-mono"
              />
            </div>
            <div className="p-3 border-t border-[#222D42] flex justify-end gap-2">
              <button
                onClick={() => setImportModalOpen(false)}
                className="px-3 py-1.5 rounded-xl bg-[#172030] text-xs text-white font-semibold cursor-pointer"
              >
                Annuler
              </button>
              <button
                onClick={handleImportXml}
                className="px-4 py-1.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white text-xs font-bold cursor-pointer"
              >
                Importer les entités
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
