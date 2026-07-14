import { Navigate, Route, Routes } from "react-router-dom";
import { appRoutes } from "../shared/config/routes";
import { useAuthStore } from "../features/auth/model/auth-store";
import { LoginPage } from "../pages/login/page";
import { DashboardPage } from "../pages/dashboard/page";
import { TicketsPage } from "../pages/tickets/page";
import { EquipmentPage } from "../pages/equipment/page";
import { UsersPage } from "../pages/users/page";
import { DepartmentsPage } from "../pages/departments/page";
import { AppLayout } from "./layouts/app-layout";
import { PublicOnlyRoute } from "./guard/public-only-route";
import type { ReactElement } from "react";

function RequireAuth({ children }: { children: ReactElement }) {
  const session = useAuthStore((state) => state.session);
  if (!session) {
    return <Navigate to={appRoutes.login} replace />;
  }
  return children;
}

function RequireAdmin({ children }: { children: ReactElement }) {
  const session = useAuthStore((state) => state.session);
  if (!session || session.user.roleName !== "Admin") {
    return <Navigate to={appRoutes.dashboard} replace />;
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
        <Route
          path={appRoutes.login}
          element={
            <PublicOnlyRoute>
              <LoginPage />
            </PublicOnlyRoute>
          }
        />
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
        <Route
          path={appRoutes.equipment}
          element={
            <RequireAuth>
              <EquipmentPage />
            </RequireAuth>
          }
        />
        <Route
          path={appRoutes.users}
          element={
            <RequireAuth>
              <RequireAdmin>
                <UsersPage />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route
          path={appRoutes.departments}
          element={
            <RequireAuth>
              <RequireAdmin>
                <DepartmentsPage />
              </RequireAdmin>
            </RequireAuth>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
