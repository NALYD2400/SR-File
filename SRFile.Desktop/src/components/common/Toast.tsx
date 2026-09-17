import React, { createContext, useContext, useState, useCallback } from "react";
import { CheckCircle2, AlertTriangle, Info, XCircle, X } from "lucide-react";
import { cn } from "../../lib/utils";

export type ToastType = "success" | "error" | "info" | "warning";

export interface ToastItem {
  id: string;
  title: string;
  description?: string;
  type: ToastType;
}

interface ToastContextType {
  showToast: (title: string, description?: string, type?: ToastType) => void;
  success: (title: string, description?: string) => void;
  error: (title: string, description?: string) => void;
  info: (title: string, description?: string) => void;
  warning: (title: string, description?: string) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const showToast = useCallback(
    (title: string, description?: string, type: ToastType = "info") => {
      const id = Math.random().toString(36).substring(2, 9);
      const newToast: ToastItem = { id, title, description, type };

      setToasts((prev) => [...prev.slice(-4), newToast]);

      setTimeout(() => {
        removeToast(id);
      }, 4000);
    },
    [removeToast]
  );

  const success = useCallback(
    (title: string, description?: string) => showToast(title, description, "success"),
    [showToast]
  );
  const error = useCallback(
    (title: string, description?: string) => showToast(title, description, "error"),
    [showToast]
  );
  const info = useCallback(
    (title: string, description?: string) => showToast(title, description, "info"),
    [showToast]
  );
  const warning = useCallback(
    (title: string, description?: string) => showToast(title, description, "warning"),
    [showToast]
  );

  const getIcon = (type: ToastType) => {
    switch (type) {
      case "success":
        return <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />;
      case "error":
        return <XCircle className="w-4 h-4 text-rose-400 shrink-0" />;
      case "warning":
        return <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0" />;
      case "info":
      default:
        return <Info className="w-4 h-4 text-[#FF7A29] shrink-0" />;
    }
  };

  const getBorderColor = (type: ToastType) => {
    switch (type) {
      case "success":
        return "border-emerald-500/40 bg-[#0F1D17]/95 shadow-[0_8px_30px_rgba(16,185,129,0.15)]";
      case "error":
        return "border-rose-500/40 bg-[#1D0F13]/95 shadow-[0_8px_30px_rgba(244,63,94,0.15)]";
      case "warning":
        return "border-amber-500/40 bg-[#1F190D]/95 shadow-[0_8px_30px_rgba(245,158,11,0.15)]";
      case "info":
      default:
        return "border-[#FF7A29]/40 bg-[#15120F]/95 shadow-[0_8px_30px_rgba(255,122,41,0.15)]";
    }
  };

  return (
    <ToastContext.Provider value={{ showToast, success, error, info, warning }}>
      {children}
      {/* Toast floating container */}
      <div className="fixed bottom-5 right-5 z-[9999] flex flex-col gap-2.5 max-w-sm pointer-events-none">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cn(
              "pointer-events-auto flex items-start gap-3 p-3.5 rounded-2xl border backdrop-blur-xl transition-all duration-300 transform translate-y-0 opacity-100 animate-in fade-in slide-in-from-bottom-3 select-none",
              getBorderColor(toast.type)
            )}
          >
            <div className="pt-0.5">{getIcon(toast.type)}</div>
            <div className="flex-1 text-xs">
              <h4 className="font-bold text-white tracking-wide">{toast.title}</h4>
              {toast.description && (
                <p className="text-[11px] text-[#94A3B8] mt-0.5 leading-relaxed font-mono">
                  {toast.description}
                </p>
              )}
            </div>
            <button
              onClick={() => removeToast(toast.id)}
              className="p-1 rounded-lg hover:bg-white/10 text-[#64748B] hover:text-white transition-colors cursor-pointer"
            >
              <X className="w-3.5 h-3.5" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
};

export const useToast = (): ToastContextType => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider");
  }
  return context;
};
