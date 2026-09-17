import { useState, useEffect } from "react";
import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import { AppStatus, RpfInfo, ActiveView } from "./types";
import { api } from "./api/client";
import { Header } from "./components/common/Header";
import { Sidebar } from "./components/common/Sidebar";
import { HubDashboard } from "./components/dashboard/HubDashboard";
import { RpfExplorer } from "./components/explorer/RpfExplorer";
import { TextureStudio } from "./components/textures/TextureStudio";
import { AudioLab } from "./components/audio/AudioLab";
import { SettingsModal } from "./components/modals/SettingsModal";
import { CommandPalette } from "./components/modals/CommandPalette";
import { ModManager } from "./components/mods/ModManager";
import { Gen9Converter } from "./components/gen9/Gen9Converter";
import { CodeEditor } from "./components/editors/CodeEditor";
import { HexViewer } from "./components/editors/HexViewer";
import { JenkinsTool } from "./components/crypto/JenkinsTool";
import { CryptoTool } from "./components/crypto/CryptoTool";
import { ProjectEditor } from "./components/project/ProjectEditor";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

function AppContent() {
  const [activeView, setActiveView] = useState<ActiveView>("dashboard");
  const [currentRpf, setCurrentRpf] = useState<RpfInfo | null>(null);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);

  // Fetch Sidecar status periodically
  const { data: status, refetch: refetchStatus, isFetching: isStatusLoading } = useQuery<AppStatus>({
    queryKey: ["sidecar-status"],
    queryFn: () => api.getStatus(),
    refetchInterval: 10000,
  });

  // Global Ctrl+K / Cmd+K listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setIsCommandPaletteOpen((prev) => !prev);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  const handleOpenRpf = async (filePath: string) => {
    const info = await api.openRpf(filePath);
    setCurrentRpf(info);
    setActiveView("explorer");
  };

  const handleSaveConfig = async (folder: string, isGen9: boolean, key?: string) => {
    await api.configureGtaFolder(folder, isGen9, key);
    await refetchStatus();
  };

  return (
    <div className="flex flex-col h-screen w-screen overflow-hidden bg-[#0A0E16] select-none text-[#F1F5F9]">
      {/* Top Header */}
      <Header
        status={status}
        currentRpf={currentRpf}
        onOpenSettings={() => setIsSettingsOpen(true)}
        onOpenCommandPalette={() => setIsCommandPaletteOpen(true)}
        onReloadStatus={() => refetchStatus()}
        isStatusLoading={isStatusLoading}
      />

      {/* Main Body */}
      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar */}
        <Sidebar
          activeView={activeView}
          onSelectView={setActiveView}
          rpfLoaded={!!currentRpf}
        />

        {/* View Surface */}
        <main className="flex-1 flex flex-col overflow-hidden">
          {activeView === "dashboard" && (
            <HubDashboard
              status={status}
              currentRpf={currentRpf}
              onOpenRpf={handleOpenRpf}
              onNavigate={setActiveView}
              onOpenSettings={() => setIsSettingsOpen(true)}
            />
          )}

          {activeView === "explorer" && (
            <RpfExplorer
              currentRpf={currentRpf}
              onOpenAnotherRpf={() => setActiveView("dashboard")}
            />
          )}

          {activeView === "textures" && (
            <TextureStudio
              onNavigateToExplorer={() => setActiveView("explorer")}
            />
          )}

          {activeView === "audio" && (
            <AudioLab
              onNavigateToExplorer={() => setActiveView("explorer")}
            />
          )}

          {activeView === "mods" && <ModManager />}

          {activeView === "gen9" && <Gen9Converter />}

          {activeView === "project_editor" && <ProjectEditor />}

          {activeView === "code_editor" && <CodeEditor />}

          {activeView === "hex_viewer" && <HexViewer />}

          {activeView === "jenkins" && <JenkinsTool />}

          {activeView === "crypto" && <CryptoTool />}

          {activeView === "settings" && (
            <div className="flex-1 p-6 bg-[#0A0E16]">
              <div className="max-w-2xl">
                <h1 className="text-xl font-bold text-white mb-2">Paramètres du Système</h1>
                <p className="text-xs text-[#94A3B8] mb-6">
                  Gérez l'intégration de votre jeu Grand Theft Auto V et configurez le Sidecar.
                </p>
                <button
                  onClick={() => setIsSettingsOpen(true)}
                  className="px-5 py-2.5 rounded-xl bg-[#FF7A29] hover:bg-[#FF8F4D] text-white font-bold text-xs shadow-lg transition-colors cursor-pointer"
                >
                  Ouvrir la fenêtre de configuration
                </button>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* Modals */}
      <SettingsModal
        isOpen={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        status={status}
        onSaveConfig={handleSaveConfig}
      />

      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
        currentRpf={currentRpf}
        onSelectView={setActiveView}
        onOpenAnotherRpf={() => {
          setActiveView("dashboard");
        }}
        onOpenSettings={() => setIsSettingsOpen(true)}
      />
    </div>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppContent />
    </QueryClientProvider>
  );
}
