import { type ReactElement } from "react";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { Navigate } from "react-router-dom";
import { appRoutes } from "../../shared/config/routes";

export function PublicOnlyRoute({ children }: { children: ReactElement }) {
  const session = useAuthStore((state) => state.session);

  if (session) {
    return <Navigate to={appRoutes.dashboard} replace />;
  }

  return children;
}
