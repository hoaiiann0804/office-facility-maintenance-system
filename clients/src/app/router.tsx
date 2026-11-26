import { lazy, Suspense, type ReactElement } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { appRoutes } from "../shared/config/routes";
import { useAuthStore } from "../features/auth/model/auth-store";
import { AppLayout } from "./layouts/app-layout";
import { PublicOnlyRoute } from "./guard/public-only-route";

// [FIX PERF-08 / BƯỚC 3A - TÁCH NHỎ BUNDLE BẰNG ROUTE-LEVEL CODE SPLITTING (React.lazy)]:
// Thay vì import tĩnh cả 8 màn hình (gồm cả thư viện biểu đồ Recharts rất nặng của ReportsPage)
// vào chung 1 file JS duy nhất (1.18 MB), sử dụng React.lazy() để trình duyệt mở trang nào
// chỉ tải đúng file chunk của trang đó. Giúp trang Login/Dashboard mở qua mạng Tunnel nhanh gấp 3-4 lần.
const LoginPage = lazy(() => import("../pages/login/page").then((m) => ({ default: m.LoginPage })));
const DashboardPage = lazy(() =>
  import("../pages/dashboard/page").then((m) => ({ default: m.DashboardPage })),
);
const TicketsPage = lazy(() =>
  import("../pages/tickets/page").then((m) => ({ default: m.TicketsPage })),
);
const EquipmentPage = lazy(() =>
  import("../pages/equipment/page").then((m) => ({ default: m.EquipmentPage })),
);
const UsersPage = lazy(() => import("../pages/users/page").then((m) => ({ default: m.UsersPage })));
const DepartmentsPage = lazy(() =>
  import("../pages/departments/page").then((m) => ({ default: m.DepartmentsPage })),
);
const VendorsPage = lazy(() =>
  import("../pages/vendors/page").then((m) => ({ default: m.VendorsPage })),
);
const ReportsPage = lazy(() =>
  import("../pages/reports/page").then((m) => ({ default: m.ReportsPage })),
);

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

function RequireManagerOrAdmin({ children }: { children: ReactElement }) {
  const session = useAuthStore((state) => state.session);
  if (!session || (session.user.roleName !== "Admin" && session.user.roleName !== "Manager")) {
    return <Navigate to={appRoutes.dashboard} replace />;
  }
  return children;
}

export function AppRouter() {
  const session = useAuthStore((state) => state.session);

  return (
    <Suspense
      fallback={
        <div className="min-h-[60vh] flex items-center justify-center">
          <div className="h-7 w-7 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
        </div>
      }
    >
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
          <Route
            path={appRoutes.vendors}
            element={
              <RequireAuth>
                <RequireManagerOrAdmin>
                  <VendorsPage />
                </RequireManagerOrAdmin>
              </RequireAuth>
            }
          />
          <Route
            path={appRoutes.reports}
            element={
              <RequireAuth>
                <RequireManagerOrAdmin>
                  <ReportsPage />
                </RequireManagerOrAdmin>
              </RequireAuth>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Routes>
    </Suspense>
  );
}
