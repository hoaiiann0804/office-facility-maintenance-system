import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { useEffect } from "react";
import "./index.css";
import App from "./App.tsx";
import { AppQueryProvider } from "./app/providers/query-provider";
import { useAuthStore } from "./features/auth/model/auth-store";

export function AuthBootstrap() {
  const hydrate = useAuthStore((state) => state.hydrate);
  const hydrated = useAuthStore((state) => state.hydrated);

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  if (!hydrated) {
    return null;
  }

  return <App />;
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <AppQueryProvider>
      <BrowserRouter>
        <AuthBootstrap />
      </BrowserRouter>
    </AppQueryProvider>
  </StrictMode>,
);
