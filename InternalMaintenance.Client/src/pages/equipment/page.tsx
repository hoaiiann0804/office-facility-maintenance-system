import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "sonner";
import axios from "axios";
import { Badge, EmptyState, Panel, Spinner, ThemeToggle } from "../../shared/ui";
import { appRoutes } from "../../shared/config/routes";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { logout } from "../../shared/api/auth";
import { useEquipmentQuery } from "../../features/tickets/api/use-equipment-query";
import { useDepartmentsQuery } from "../../features/equipment/api/use-departments-query";
import { useDeleteEquipmentMutation } from "../../features/equipment/api/use-equipment-mutations";
import { EquipmentModal } from "../../features/equipment/components/equipment-modal";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";
import type { Equipment, EquipmentStatus } from "../../entities/equipment/model/types";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium" }).format(new Date(value));
};

export function EquipmentPage() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const navigate = useNavigate();

  const role = session?.user.roleName;
  const isAdmin = role === "Admin";
  const isManager = role === "Manager";
  const canEdit = isAdmin || isManager;

  const [search, setSearch] = useState("");
  const [deptFilter, setDeptFilter] = useState<number | "">("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedEq, setSelectedEq] = useState<Equipment | null>(null);
  const [isEqModalOpen, setIsEqModalOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  const { data: deptsPage } = useDepartmentsQuery({ pageSize: 100 });
  const {
    data: eqPage,
    isLoading,
    isError,
  } = useEquipmentQuery({
    keyword: search.trim() || undefined,
    departmentId: deptFilter === "" ? undefined : deptFilter,
    status: (statusFilter === "" ? undefined : statusFilter) as EquipmentStatus | undefined,
    page,
    pageSize,
  });

  const deleteMutation = useDeleteEquipmentMutation();

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
    if (!window.confirm("Bạn có chắc chắn muốn xóa thiết bị này không?")) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success("Xóa thiết bị thành công!");
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Xóa thiết bị thất bại.");
      } else {
        toast.error("Xóa thiết bị thất bại.");
      }
    }
  };

  const departments = deptsPage?.items ?? [];
  const equipmentList = eqPage?.items ?? [];
  const totalPages = eqPage?.totalPages ?? 1;

  return (
    <div className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">IM</div>
          <div>
            <strong>Management Console</strong>
            <span>Thiết bị workspace</span>
          </div>
        </div>

        <nav className="tabs" aria-label="Modules">
          <Link className="tab" to={appRoutes.dashboard}>
            Dashboard
          </Link>
          <Link className="tab" to={appRoutes.tickets}>
            Tickets
          </Link>
          <Link className="tab active" to={appRoutes.equipment}>
            Equipment
          </Link>
          {isAdmin && (
            <Link className="tab" to={appRoutes.users}>
              Users
            </Link>
          )}
          {isAdmin && (
            <Link className="tab" to={appRoutes.departments}>
              Departments
            </Link>
          )}
        </nav>

        <div className="badge-row">
          <ThemeToggle />
          <button
            type="button"
            className="button secondary"
            onClick={() => setIsChangePasswordOpen(true)}
          >
            Đổi mật khẩu
          </button>
          <Badge tone="default">{session?.user.fullName ?? "Guest"}</Badge>
          <Badge tone={isAdmin ? "primary" : "default"}>{role ?? "Guest"}</Badge>
          <button type="button" className="button secondary" onClick={handleLogout}>
            Đăng xuất
          </button>
        </div>
      </header>

      <div className="layout" style={{ gridTemplateColumns: "1fr" }}>
        <main className="main-panel">
          <Panel>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <div>
                <span className="eyebrow">Equipment</span>
                <h2>Quản lý danh sách thiết bị văn phòng</h2>
              </div>
              {canEdit && (
                <button
                  type="button"
                  className="button primary"
                  onClick={() => {
                    setSelectedEq(null);
                    setIsEqModalOpen(true);
                  }}
                >
                  + Thêm Thiết Bị
                </button>
              )}
            </div>

            <div className="filter-grid" style={{ gridTemplateColumns: "2fr 1fr 1fr" }}>
              <label className="field">
                <span>Tìm kiếm</span>
                <input
                  className="input"
                  value={search}
                  placeholder="Mã, tên, hoặc mô tả thiết bị..."
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setPage(1);
                  }}
                />
              </label>

              <label className="field">
                <span>Phòng ban</span>
                <select
                  className="select"
                  value={deptFilter}
                  onChange={(e) => {
                    setDeptFilter(e.target.value ? Number(e.target.value) : "");
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả phòng ban</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Trạng thái</span>
                <select
                  className="select"
                  value={statusFilter}
                  onChange={(e) => {
                    setStatusFilter(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả trạng thái</option>
                  <option value="Active">Hoạt động (Active)</option>
                  <option value="Inactive">Ngưng hoạt động (Inactive)</option>
                  <option value="UnderMaintenance">Đang bảo trì (UnderMaintenance)</option>
                  <option value="Retired">Thanh lý (Retired)</option>
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
                  description="Không thể tải danh sách thiết bị. Vui lòng thử lại sau."
                />
              ) : equipmentList.length > 0 ? (
                <>
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Mã thiết bị</th>
                        <th>Tên thiết bị</th>
                        <th>Phòng ban</th>
                        <th>Trạng thái</th>
                        <th>Ngày mua</th>
                        <th>Mô tả</th>
                        {canEdit && <th style={{ textAlign: "right" }}>Thao tác</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {equipmentList.map((eq) => (
                        <tr key={eq.id}>
                          <td style={{ fontWeight: 800 }}>{eq.code}</td>
                          <td>{eq.name}</td>
                          <td>{eq.departmentName}</td>
                          <td>
                            <Badge
                              tone={
                                eq.status === "Active"
                                  ? "good"
                                  : eq.status === "UnderMaintenance"
                                    ? "warn"
                                    : eq.status === "Retired"
                                      ? "bad"
                                      : "default"
                              }
                            >
                              {eq.status}
                            </Badge>
                          </td>
                          <td>{formatDateTime(eq.purchasedDate)}</td>
                          <td>
                            <span style={{ fontSize: "13px", color: "var(--muted)" }}>
                              {eq.description || "N/A"}
                            </span>
                          </td>
                          {canEdit && (
                            <td style={{ textAlign: "right" }}>
                              <div
                                style={{ display: "flex", gap: "6px", justifyContent: "flex-end" }}
                              >
                                <button
                                  type="button"
                                  className="button secondary"
                                  style={{ padding: "6px 10px", fontSize: "13px" }}
                                  onClick={() => {
                                    setSelectedEq(eq);
                                    setIsEqModalOpen(true);
                                  }}
                                >
                                  Sửa
                                </button>
                                {isAdmin && (
                                  <button
                                    type="button"
                                    className="button danger"
                                    style={{ padding: "6px 10px", fontSize: "13px" }}
                                    onClick={() => handleDelete(eq.id)}
                                    disabled={deleteMutation.isPending}
                                  >
                                    Xóa
                                  </button>
                                )}
                              </div>
                            </td>
                          )}
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
                  title="Không tìm thấy thiết bị"
                  description="Hãy thử thay đổi điều kiện tìm kiếm hoặc thêm thiết bị mới."
                />
              )}
            </div>
          </Panel>
        </main>
      </div>

      <EquipmentModal
        equipment={selectedEq}
        isOpen={isEqModalOpen}
        onClose={() => {
          setIsEqModalOpen(false);
          setSelectedEq(null);
        }}
      />

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
