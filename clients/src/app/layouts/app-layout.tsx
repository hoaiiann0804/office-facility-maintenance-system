import { Outlet, Link, useLocation, useNavigate } from "react-router-dom";
import { Toaster } from "sonner";
import { useAuthStore } from "@/features/auth/model/auth-store";
import { appRoutes } from "@/shared/config/routes";
import { logout } from "@/shared/api/auth";
import { UserProfile, ThemeToggle } from "@/shared/ui";
import { useState } from "react";
import { ChangePasswordModal } from "@/features/auth/components/change-password-modal";
import {
  LayoutDashboard,
  Ticket,
  Wrench,
  Users,
  Building2,
  Store,
  BarChart3,
  Menu,
  X,
  ChevronRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

export function AppLayout() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const location = useLocation();
  const navigate = useNavigate();
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const isAdmin = session?.user.roleName === "Admin";
  const isManagerOrAdmin = isAdmin || session?.user.roleName === "Manager";
  const isLoginPage = location.pathname === appRoutes.login || !session;

  const [prevPath, setPrevPath] = useState(location.pathname);
  if (prevPath !== location.pathname) {
    setPrevPath(location.pathname);
    if (isMobileMenuOpen) {
      setIsMobileMenuOpen(false);
    }
  }

  const handleLogout = async (): Promise<void> => {
    const refreshToken = session?.refreshToken;
    try {
      if (refreshToken) {
        await logout(refreshToken);
      }
    } catch (error) {
      console.error("Logout request failed:", error);
    } finally {
      signOut();
      navigate(appRoutes.login);
    }
  };

  const navItems = [
    { to: appRoutes.dashboard, label: "Tổng quan", icon: LayoutDashboard },
    { to: appRoutes.tickets, label: "Phiếu yêu cầu", icon: Ticket },
    { to: appRoutes.equipment, label: "Thiết bị", icon: Wrench },
    ...(isManagerOrAdmin ? [{ to: appRoutes.vendors, label: "Nhà cung cấp", icon: Store }] : []),
    ...(isManagerOrAdmin ? [{ to: appRoutes.reports, label: "Báo cáo", icon: BarChart3 }] : []),
    ...(isAdmin ? [{ to: appRoutes.users, label: "Người dùng", icon: Users }] : []),
    ...(isAdmin ? [{ to: appRoutes.departments, label: "Phòng ban", icon: Building2 }] : []),
  ];

  const isNavItemActive = (to: string) =>
    location.pathname === to || (to !== appRoutes.dashboard && location.pathname.startsWith(to));

  const activeNavItem = navItems.find((item) => isNavItemActive(item.to));

  const renderNavLinks = (onItemClick?: () => void) => (
    <div className="space-y-1">
      <p className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground px-3 pb-1.5">
        Menu chức năng
      </p>
      {navItems.map((item) => {
        const Icon = item.icon;
        const isActive = isNavItemActive(item.to);

        return (
          <Link
            key={item.to}
            to={item.to}
            onClick={onItemClick}
            className={cn(
              "flex items-center justify-between px-3 py-2.5 rounded-xl text-[13px] font-medium transition-all select-none",
              isActive
                ? "bg-primary text-primary-foreground font-semibold shadow-sm"
                : "text-muted-foreground hover:text-foreground hover:bg-accent",
            )}
          >
            <div className="flex items-center gap-3 min-w-0">
              <Icon
                className={cn(
                  "h-4 w-4 shrink-0",
                  isActive ? "text-primary-foreground" : "text-muted-foreground",
                )}
              />
              <span className="truncate">{item.label}</span>
            </div>
            <ChevronRight
              className={cn(
                "h-3.5 w-3.5 shrink-0 transition-opacity",
                isActive ? "opacity-100" : "opacity-0 group-hover:opacity-50",
              )}
            />
          </Link>
        );
      })}
    </div>
  );

  return (
    <div className="min-h-screen bg-background text-foreground flex relative">
      <Toaster
        position="top-right"
        richColors
        duration={3500}
        visibleToasts={3}
        toastOptions={{ style: { borderRadius: "12px" } }}
      />

      {isLoginPage ? (
        /* Minimal layout for Login page — Sidebar & Topbar hidden, floating ThemeToggle only */
        <>
          <div className="absolute top-4 right-4 z-50">
            <ThemeToggle />
          </div>
          <main className="flex-1 w-full">
            <Outlet />
          </main>
        </>
      ) : (
        <>
          {/* Desktop Vertical Dashboard Sidebar (>= lg) */}
          <aside className="hidden lg:flex lg:flex-col lg:w-64 lg:shrink-0 border-r bg-card sticky top-0 h-screen z-30">
            {/* Brand Header */}
            <div className="h-16 px-5 border-b flex items-center shrink-0">
              <Link to={appRoutes.dashboard} className="flex items-center gap-2.5 group">
                <div className="w-9 h-9 rounded-xl bg-primary flex items-center justify-center font-bold text-primary-foreground text-xs shadow-sm transition-transform group-hover:scale-105">
                  IM
                </div>
                <div className="flex flex-col">
                  <span className="text-sm font-bold leading-none tracking-tight">
                    Hệ thống Quản lý
                  </span>
                  <span className="text-[10px] text-muted-foreground leading-none mt-1">
                    Bảo trì cơ sở vật chất
                  </span>
                </div>
              </Link>
            </div>

            {/* Vertical Navigation Links */}
            <nav className="flex-1 overflow-y-auto px-3 py-5">{renderNavLinks()}</nav>

            {/* Sidebar Footer */}
            <div className="p-4 border-t bg-muted/30 text-[11px] text-muted-foreground space-y-1 shrink-0">
              <p className="truncate">
                Đăng nhập:{" "}
                <strong className="text-foreground font-semibold">{session?.user.fullName}</strong>
              </p>
              <p className="flex items-center gap-1.5">
                <span>Vai trò:</span>
                <span className="inline-flex items-center gap-1 text-primary font-semibold">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                  {session?.user.roleName}
                </span>
              </p>
            </div>
          </aside>

          {/* Right Column: Top Header + Main Content */}
          <div className="flex-1 flex flex-col min-w-0">
            <header className="sticky top-0 z-20 h-16 w-full border-b bg-background/90 backdrop-blur-md">
              <div className="h-full max-w-7xl mx-auto flex items-center justify-between px-4 sm:px-6 lg:px-8">
                {/* Left section: Mobile Hamburger + Brand (< lg) OR Active Page Indicator (>= lg) */}
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                    className="lg:hidden p-2 rounded-xl border border-border text-foreground hover:bg-accent focus:outline-none transition-colors cursor-pointer"
                    aria-label="Mở menu điều hướng"
                  >
                    {isMobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
                  </button>

                  {/* Mobile Brand logo */}
                  <Link
                    to={appRoutes.dashboard}
                    className="flex lg:hidden items-center gap-2.5 group"
                  >
                    <div className="w-9 h-9 rounded-xl bg-primary flex items-center justify-center font-bold text-primary-foreground text-xs shadow-sm transition-transform group-hover:scale-105">
                      IM
                    </div>
                    <div className="flex flex-col">
                      <span className="text-sm font-bold leading-none tracking-tight">
                        Hệ thống Quản lý
                      </span>
                      <span className="text-[10px] text-muted-foreground leading-none mt-1 hidden sm:inline">
                        Bảo trì cơ sở vật chất
                      </span>
                    </div>
                  </Link>

                  {/* Desktop Current Section Indicator */}
                  {activeNavItem && (
                    <div className="hidden lg:flex items-center gap-2.5 text-sm">
                      <div className="w-8 h-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center">
                        <activeNavItem.icon className="h-4 w-4" />
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs font-semibold text-foreground leading-tight">
                          {activeNavItem.label}
                        </span>
                        <span className="text-[10px] text-muted-foreground leading-tight">
                          Quản lý bảo trì nội bộ
                        </span>
                      </div>
                    </div>
                  )}
                </div>

                {/* Right section: User Profile with Demo Role Switcher inside */}
                <UserProfile
                  fullName={session?.user.fullName}
                  roleName={session?.user.roleName}
                  onLogout={handleLogout}
                  onChangePassword={() => setIsChangePasswordOpen(true)}
                />
              </div>
            </header>

            {/* Mobile/Tablet Slide-Over Drawer Navigation */}
            {isMobileMenuOpen && (
              <div className="fixed inset-0 z-50 lg:hidden">
                {/* Backdrop overlay */}
                <div
                  className="fixed inset-0 bg-black/50 backdrop-blur-xs transition-opacity animate-in fade-in"
                  onClick={() => setIsMobileMenuOpen(false)}
                />

                {/* Drawer content */}
                <div className="fixed inset-y-0 left-0 w-72 max-w-[85vw] bg-card border-r shadow-2xl p-5 flex flex-col justify-between z-50 animate-in slide-in-from-left duration-200">
                  <div className="space-y-6">
                    {/* Drawer header */}
                    <div className="flex items-center justify-between pb-4 border-b">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center font-bold text-primary-foreground text-xs">
                          IM
                        </div>
                        <div>
                          <p className="text-xs font-bold leading-tight">Hệ thống Quản lý</p>
                          <p className="text-[10px] text-muted-foreground">
                            Bảo trì cơ sở vật chất
                          </p>
                        </div>
                      </div>
                      <button
                        type="button"
                        onClick={() => setIsMobileMenuOpen(false)}
                        className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>

                    {/* Navigation list */}
                    {renderNavLinks(() => setIsMobileMenuOpen(false))}
                  </div>

                  {/* Drawer footer info */}
                  <div className="pt-4 border-t text-[11px] text-muted-foreground flex flex-col gap-1">
                    <p>
                      Đăng nhập:{" "}
                      <strong className="text-foreground">{session?.user.fullName}</strong>
                    </p>
                    <p>
                      Vai trò:{" "}
                      <span className="text-primary font-semibold">{session?.user.roleName}</span>
                    </p>
                  </div>
                </div>
              </div>
            )}

            {/* Main Content Area */}
            <main className="flex-1 w-full max-w-7xl mx-auto p-4 sm:p-6 lg:p-8">
              <Outlet />
            </main>
          </div>
        </>
      )}

      {/* [FIX G-11 (Mục 3.1.3) - CƯỠNG CHẾ ĐỔI MẬT KHẨU LẦN ĐẦU]:
          - Khi session.user.mustChangePassword === true, biểu thức Boolean(...) giữ isOpen luôn là true.
          - Trong onClose() luôn gọi thẳng setIsChangePasswordOpen(false) mà không chặn bằng closure session cũ,
            để ngay khi mutateAsync cập nhật mustChangePassword = false và gọi onClose(), modal đóng lại ngay lập tức. */}
      <ChangePasswordModal
        isOpen={isChangePasswordOpen || Boolean(!isLoginPage && session?.user.mustChangePassword)}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
