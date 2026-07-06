import { Navigate, Route, Routes } from "react-router-dom";
import { appRoutes } from "../shared/config/routes";
import { useAuthStore } from "../features/auth/model/auth-store";
import { LoginPage } from "../pages/login/page";
import { DashboardPage } from "../pages/dashboard/page";
import { TicketsPage } from "../pages/tickets/page";
import { AppLayout } from "./layouts/app-layout";
import type { ReactElement } from "react";

function RequireAuth({ children }: { children: ReactElement }) {
  const session = useAuthStore((state) => state.session);
  if (!session) {
    return <Navigate to={appRoutes.login} replace />;
  }
  return children;
}

export function AppRouter() {
  const session = useAuthStore((state) => state.session);

  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route
          path="/"
          element={<Navigate to={session ? appRoutes.dashboard : appRoutes.login} replace />}
        />
        <Route path={appRoutes.login} element={<LoginPage />} />
        <Route
          path={appRoutes.dashboard}
          element={
            <RequireAuth>
              <DashboardPage />
            </RequireAuth>
          }
        />
        <Route
          path={appRoutes.tickets}
          element={
            <RequireAuth>
              <TicketsPage />
            </RequireAuth>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
