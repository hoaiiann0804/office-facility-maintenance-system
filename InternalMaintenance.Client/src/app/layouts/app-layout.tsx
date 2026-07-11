import { Outlet } from "react-router-dom";
import { Toaster } from "sonner";

export function AppLayout() {
  return (
    <div className="app-shell">
      <Toaster
        position="top-right"
        richColors
        closeButton
        expand
        duration={6000}
        toastOptions={{ style: { borderRadius: "16px" } }}
      />
      <Outlet />
    </div>
  );
}
