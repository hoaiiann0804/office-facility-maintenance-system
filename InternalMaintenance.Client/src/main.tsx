import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { useEffect } from "react";
import "./index.css";
import App from "./App.tsx";
import { AppQueryProvider } from "./app/providers/query-provider";
import { useAuthStore } from "./features/auth/model/auth-store";
import { useAuthMeQuery } from "./features/auth/api/use-auth-me-query";

export function AuthBootstrap() {
  const hydrated = useAuthStore((state) => state.hydrated);
  const hydrate = useAuthStore((state) => state.hydrate);
  const { isCheckingAuth } = useAuthMeQuery();

  useEffect(() => {
    hydrate();
  }, [hydrate]);

  if (!hydrated || isCheckingAuth) {
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
