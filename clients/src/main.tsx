import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { useEffect } from "react";
import { GoogleOAuthProvider } from "@react-oauth/google";
import "sonner/dist/styles.css";
import "./index.css";
import App from "./App.tsx";
import { AppQueryProvider } from "./app/providers/query-provider";
import { ThemeProvider } from "./app/providers/theme-provider";
import { useAuthStore } from "./features/auth/model/auth-store";
import { useAuthMeQuery } from "./features/auth/api/use-auth-me-query";

const googleClientId =
  import.meta.env.VITE_GOOGLE_CLIENT_ID || "demo-google-client-id.apps.googleusercontent.com";

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
    <GoogleOAuthProvider clientId={googleClientId}>
      <AppQueryProvider>
        <ThemeProvider>
          <BrowserRouter>
            <AuthBootstrap />
          </BrowserRouter>
        </ThemeProvider>
      </AppQueryProvider>
    </GoogleOAuthProvider>
  </StrictMode>,
);
