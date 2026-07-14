import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import axios from "axios";
import { EmptyState, Panel, Spinner, UserProfile } from "../../shared/ui";
import { appRoutes } from "../../shared/config/routes";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { logout } from "../../shared/api/auth";
import { useDepartmentsQuery } from "../../features/equipment/api/use-departments-query";
import { useDeleteDepartmentMutation } from "../../features/departments/api/use-department-mutations";
import { DepartmentModal } from "../../features/departments/components/department-modal";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";
import type { Department } from "../../entities/department/model/types";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
};

export function DepartmentsPage() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const navigate = useNavigate();

  const role = session?.user.roleName;
  const isAdmin = role === "Admin";

  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedDept, setSelectedDept] = useState<Department | null>(null);
  const [isDeptModalOpen, setIsDeptModalOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  const {
    data: deptsPage,
    isLoading,
    isError,
  } = useDepartmentsQuery({
    keyword: search.trim() || undefined,
    page,
    pageSize,
  });

  const deleteMutation = useDeleteDepartmentMutation();

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

  const handleDelete = async (id: number) => {
    if (!window.confirm("Bạn có chắc chắn muốn xóa phòng ban này không?")) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success("Xóa phòng ban thành công!");
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Xóa phòng ban thất bại.");
      } else {
        toast.error("Xóa phòng ban thất bại.");
      }
    }
  };

  const departments = deptsPage?.items ?? [];
  const totalPages = deptsPage?.totalPages ?? 1;

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
            <span>Phòng ban workspace</span>
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
          {isAdmin && (
            <Link className="tab" to={appRoutes.users}>
              Users
            </Link>
          )}
          {isAdmin && (
            <Link className="tab active" to={appRoutes.departments}>
              Departments
            </Link>
          )}
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
                <span className="eyebrow">Departments</span>
                <h2>Quản lý phòng ban tổ chức</h2>
              </div>
              <button
                type="button"
                className="button primary"
                onClick={() => {
                  setSelectedDept(null);
                  setIsDeptModalOpen(true);
                }}
              >
                + Thêm Phòng Ban
              </button>
            </div>

            <div className="filter-grid" style={{ gridTemplateColumns: "1fr" }}>
              <label className="field">
                <span>Tìm kiếm</span>
                <input
                  className="input"
                  value={search}
                  placeholder="Tên phòng ban..."
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setPage(1);
                  }}
                />
              </label>
            </div>

            <div className="table-wrap">
              {isLoading ? (
                <Spinner />
              ) : isError ? (
                <EmptyState
                  title="Lỗi tải dữ liệu"
                  description="Không thể lấy danh sách phòng ban. Vui lòng thử lại sau."
                />
              ) : departments.length > 0 ? (
                <>
                  <table className="table">
                    <thead>
                      <tr>
                        <th>ID</th>
                        <th>Tên phòng ban</th>
                        <th>Mô tả</th>
                        <th>Ngày tạo</th>
                        <th>Hành động</th>
                      </tr>
                    </thead>
                    <tbody>
                      {departments.map((dept) => (
                        <tr key={dept.id}>
                          <td>{dept.id}</td>
                          <td>
                            <strong>{dept.name}</strong>
                          </td>
                          <td>{dept.description || "—"}</td>
                          <td>{formatDateTime(dept.createdAt)}</td>
                          <td>
                            <div className="button-row">
                              <button
                                type="button"
                                className="button secondary"
                                onClick={() => {
                                  setSelectedDept(dept);
                                  setIsDeptModalOpen(true);
                                }}
                              >
                                Sửa
                              </button>
                              <button
                                type="button"
                                className="button danger"
                                onClick={() => handleDelete(dept.id)}
                                disabled={deleteMutation.isPending}
                              >
                                Xóa
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  <div
                    className="pagination-row"
                    style={{
                      display: "flex",
                      justifyContent: "center",
                      alignItems: "center",
                      gap: "16px",
                      marginTop: "20px",
                    }}
                  >
                    <button
                      type="button"
                      className="button secondary"
                      disabled={page <= 1}
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                    >
                      ← Trước
                    </button>
                    <span>
                      Trang {page} / {totalPages}
                    </span>
                    <button
                      type="button"
                      className="button secondary"
                      disabled={page >= totalPages}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      Sau →
                    </button>
                  </div>
                </>
              ) : (
                <EmptyState
                  title="Không tìm thấy phòng ban"
                  description="Hãy thử thay đổi từ khóa tìm kiếm hoặc thêm phòng ban mới."
                />
              )}
            </div>
          </Panel>
        </main>
      </div>

      <DepartmentModal
        department={selectedDept}
        isOpen={isDeptModalOpen}
        onClose={() => {
          setIsDeptModalOpen(false);
          setSelectedDept(null);
        }}
      />

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
