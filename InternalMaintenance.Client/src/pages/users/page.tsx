import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import axios from "axios";
import { Badge, EmptyState, Panel, Spinner, UserProfile } from "../../shared/ui";
import { appRoutes } from "../../shared/config/routes";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { logout } from "../../shared/api/auth";
import { useUsersQuery } from "../../features/tickets/api/use-users-query";
import { useUpdateUserActiveMutation } from "../../features/users/api/use-user-mutations";
import { UserModal } from "../../features/users/components/user-modal";
import { ResetPasswordModal } from "../../features/users/components/reset-password-modal";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";
import type { User } from "../../entities/user/model/types";
import type { RoleName } from "../../entities/auth/model/types";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "Chưa từng đăng nhập";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
};

export function UsersPage() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const navigate = useNavigate();

  const role = session?.user.roleName;
  const isAdmin = role === "Admin";

  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [isUserModalOpen, setIsUserModalOpen] = useState(false);
  const [isResetOpen, setIsResetOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  const {
    data: usersPage,
    isLoading,
    isError,
  } = useUsersQuery({
    keyword: search.trim() || undefined,
    role: (roleFilter === "" ? undefined : roleFilter) as RoleName | undefined,
    page,
    pageSize,
  });

  const toggleActiveMutation = useUpdateUserActiveMutation();

  const handleLogout = async (): Promise<void> => {
    const refreshToken = session?.refreshToken;
    try {
      if (refreshToken) {
        await logout(refreshToken);
      }
    } finally {
      signOut();
      navigate(appRoutes.login);
    }
  };

  const handleToggleStatus = async (id: number, currentActive: boolean) => {
    const actionText = currentActive ? "khóa" : "kích hoạt";
    if (!window.confirm(`Bạn có chắc chắn muốn ${actionText} tài khoản này không?`)) return;

    try {
      await toggleActiveMutation.mutateAsync({ id, isActive: !currentActive });
      toast.success(`Đã ${actionText} tài khoản thành công!`);
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : `Thao tác ${actionText} thất bại.`);
      } else {
        toast.error(`Thao tác ${actionText} thất bại.`);
      }
    }
  };

  const usersList = usersPage?.items ?? [];
  const totalPages = usersPage?.totalPages ?? 1;

  // Bảo vệ ở cả UI component
  if (!isAdmin) {
    return (
      <div className="centered-content" style={{ height: "100vh" }}>
        <EmptyState
          title="Không có quyền truy cập"
          description="Trang web này chỉ dành cho người quản trị hệ thống (Admin)."
        />
      </div>
    );
  }

  return (
    <div className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">IM</div>
          <div>
            <strong>Management Console</strong>
            <span>Nhân sự workspace</span>
          </div>
        </div>

        <nav className="tabs" aria-label="Modules">
          <Link className="tab" to={appRoutes.dashboard}>
            Dashboard
          </Link>
          <Link className="tab" to={appRoutes.tickets}>
            Tickets
          </Link>
          <Link className="tab" to={appRoutes.equipment}>
            Equipment
          </Link>
          <Link className="tab active" to={appRoutes.users}>
            Users
          </Link>
          <Link className="tab" to={appRoutes.departments}>
            Departments
          </Link>
        </nav>

        <UserProfile
          fullName={session?.user.fullName}
          roleName={session?.user.roleName}
          onLogout={handleLogout}
          onChangePassword={() => setIsChangePasswordOpen(true)}
        />
      </header>

      <div className="layout" style={{ gridTemplateColumns: "1fr" }}>
        <main className="main-panel">
          <Panel>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <span className="eyebrow">Personnel</span>
                <h2>Quản lý danh sách nhân viên hệ thống</h2>
              </div>
              <button
                type="button"
                className="button primary"
                onClick={() => {
                  setSelectedUser(null);
                  setIsUserModalOpen(true);
                }}
              >
                + Thêm Nhân Viên
              </button>
            </div>

            <div className="filter-grid" style={{ gridTemplateColumns: "3fr 1fr" }}>
              <label className="field">
                <span>Tìm kiếm nhân sự</span>
                <input
                  className="input"
                  value={search}
                  placeholder="Tìm theo họ tên hoặc địa chỉ email..."
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setPage(1);
                  }}
                />
              </label>

              <label className="field">
                <span>Vai trò</span>
                <select
                  className="select"
                  value={roleFilter}
                  onChange={(e) => {
                    setRoleFilter(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả vai trò</option>
                  <option value="Admin">Admin</option>
                  <option value="Manager">Manager</option>
                  <option value="Staff">Staff</option>
                  <option value="Technician">Technician</option>
                </select>
              </label>
            </div>

            <div className="table-wrap">
              {isLoading ? (
                <div className="centered-content" style={{ padding: "40px" }}>
                  <Spinner />
                </div>
              ) : isError ? (
                <EmptyState
                  title="Đã xảy ra lỗi"
                  description="Không thể tải danh sách nhân viên. Vui lòng thử lại sau."
                />
              ) : usersList.length > 0 ? (
                <>
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Họ và tên</th>
                        <th>Địa chỉ Email</th>
                        <th>Vai trò</th>
                        <th>Phòng ban</th>
                        <th>Trạng thái</th>
                        <th>Lần đăng nhập cuối</th>
                        <th style={{ textAlign: "right" }}>Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {usersList.map((u) => (
                        <tr key={u.id}>
                          <td style={{ fontWeight: 800 }}>{u.fullName}</td>
                          <td>{u.email}</td>
                          <td>
                            <Badge tone={u.roleName === "Admin" ? "primary" : "default"}>
                              {u.roleName}
                            </Badge>
                          </td>
                          <td>{u.departmentName || "Hệ thống"}</td>
                          <td>
                            <Badge tone={u.isActive ? "good" : "bad"}>
                              {u.isActive ? "Đang hoạt động" : "Bị khóa"}
                            </Badge>
                          </td>
                          <td>{formatDateTime(u.lastLoginAt)}</td>
                          <td style={{ textAlign: "right" }}>
                            <div
                              style={{ display: "flex", gap: "6px", justifyContent: "flex-end" }}
                            >
                              <button
                                type="button"
                                className="button secondary"
                                style={{ padding: "6px 10px", fontSize: "13px" }}
                                onClick={() => {
                                  setSelectedUser(u);
                                  setIsUserModalOpen(true);
                                }}
                              >
                                Sửa
                              </button>
                              <button
                                type="button"
                                className="button secondary"
                                style={{ padding: "6px 10px", fontSize: "13px" }}
                                onClick={() => {
                                  setSelectedUser(u);
                                  setIsResetOpen(true);
                                }}
                              >
                                Reset Pass
                              </button>
                              <button
                                type="button"
                                className={u.isActive ? "button danger" : "button primary"}
                                style={{ padding: "6px 10px", fontSize: "13px" }}
                                onClick={() => handleToggleStatus(u.id, u.isActive)}
                                disabled={toggleActiveMutation.isPending}
                              >
                                {u.isActive ? "Khóa" : "Kích hoạt"}
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  <div
                    className="button-row spaced"
                    style={{ justifyContent: "center", marginTop: "20px" }}
                  >
                    <button
                      type="button"
                      className="button secondary"
                      disabled={page === 1}
                      onClick={() => setPage((p) => p - 1)}
                    >
                      Trước
                    </button>
                    <span style={{ alignSelf: "center", fontWeight: "bold" }}>
                      Trang {page} / {totalPages}
                    </span>
                    <button
                      type="button"
                      className="button secondary"
                      disabled={page === totalPages}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      Sau
                    </button>
                  </div>
                </>
              ) : (
                <EmptyState
                  title="Không tìm thấy nhân viên"
                  description="Hãy thử đổi từ khóa tìm kiếm hoặc tạo nhân viên mới."
                />
              )}
            </div>
          </Panel>
        </main>
      </div>

      <UserModal
        user={selectedUser}
        isOpen={isUserModalOpen}
        onClose={() => {
          setIsUserModalOpen(false);
          setSelectedUser(null);
        }}
      />

      <ResetPasswordModal
        user={selectedUser}
        isOpen={isResetOpen}
        onClose={() => {
          setIsResetOpen(false);
          setSelectedUser(null);
        }}
      />

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
