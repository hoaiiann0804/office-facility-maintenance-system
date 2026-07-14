import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Badge, Panel, Spinner, StatCard, UserProfile } from "../../shared/ui";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { appRoutes } from "../../shared/config/routes";
import { logout } from "../../shared/api/auth";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";
import { useTicketsQuery } from "../../features/tickets/api/use-tickets-query";
import { useEquipmentQuery } from "../../features/tickets/api/use-equipment-query";
import { useUsersQuery } from "../../features/tickets/api/use-users-query";
import { useDepartmentsQuery } from "../../features/equipment/api/use-departments-query";

const STATUS_WORKFLOW: Array<{ status: string; label: string }> = [
  { status: "Pending", label: "Pending — Chờ tiếp nhận" },
  { status: "Assigned", label: "Assigned — Đã phân công" },
  { status: "InProgress", label: "InProgress — Đang xử lý" },
  { status: "Resolved", label: "Resolved — Đã xử lý" },
  { status: "Closed", label: "Closed — Đã đóng" },
  { status: "Cancelled", label: "Cancelled — Đã hủy" },
];

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
};

export function DashboardPage() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const navigate = useNavigate();

  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);
  const isAdmin = session?.user.roleName === "Admin";

  // Fetch real data from API
  const { data: ticketsPage, isLoading: isTicketsLoading } = useTicketsQuery({ pageSize: 100 });
  const { data: equipmentPage, isLoading: isEquipmentLoading } = useEquipmentQuery({
    pageSize: 100,
  });
  const { data: usersPage, isLoading: isUsersLoading } = useUsersQuery({ pageSize: 100 });
  const { data: deptsPage, isLoading: isDeptsLoading } = useDepartmentsQuery({ pageSize: 100 });

  const tickets = ticketsPage?.items ?? [];
  const equipmentList = equipmentPage?.items ?? [];
  const users = usersPage?.items ?? [];
  const departments = deptsPage?.items ?? [];

  const openTickets = tickets.filter(
    (ticket) => !["Closed", "Cancelled"].includes(ticket.status),
  ).length;
  const activeEquipment = equipmentList.filter((item) => item.status === "Active").length;
  const technicianCount = users.filter((user) => user.roleName === "Technician").length;
  const departmentCount = departments.length;

  const recentTickets = [...tickets]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  const isStatsLoading = isTicketsLoading || isEquipmentLoading || isUsersLoading || isDeptsLoading;

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

  return (
    <div className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">IM</div>
          <div>
            <strong>Management Console</strong>
            <span>Dashboard overview</span>
          </div>
        </div>

        <nav className="tabs" aria-label="Modules">
          <Link className="tab active" to={appRoutes.dashboard}>
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
            <Link className="tab" to={appRoutes.departments}>
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

      <div className="layout">
        <main className="main-panel">
          <div className="view-grid">
            <Panel>
              <span className="eyebrow">Overview</span>
              <h2>Dashboard overview</h2>
              <p className="section-lead">
                Tổng quan dữ liệu hệ thống quản lý bảo trì nội bộ — dữ liệu được cập nhật từ API
                thời gian thực.
              </p>
              {isStatsLoading ? (
                <Spinner />
              ) : (
                <div className="stats-grid compact">
                  <StatCard label="Open tickets" value={openTickets} />
                  <StatCard label="Active equipment" value={activeEquipment} />
                  <StatCard label="Technicians" value={technicianCount} />
                  <StatCard label="Departments" value={departmentCount} />
                </div>
              )}
            </Panel>

            <Panel>
              <span className="eyebrow">Workflow</span>
              <h2>Ticket life cycle</h2>
              <div className="stack">
                {STATUS_WORKFLOW.map((step, index) => (
                  <div key={step.status} className="mini-card">
                    <div className="card-row">
                      <strong>
                        {index + 1}. {step.label}
                      </strong>
                      <Badge
                        tone={
                          index === 0
                            ? "primary"
                            : index === STATUS_WORKFLOW.length - 1
                              ? "good"
                              : "default"
                        }
                      >
                        {index === 0
                          ? "entry"
                          : index === STATUS_WORKFLOW.length - 1
                            ? "finish"
                            : "step"}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </Panel>

            <Panel>
              <span className="eyebrow">Recent</span>
              <h2>Ticket mới nhất</h2>
              {isTicketsLoading ? (
                <Spinner />
              ) : recentTickets.length > 0 ? (
                <div className="timeline">
                  {recentTickets.map((ticket) => (
                    <article key={ticket.id} className="timeline-item">
                      <strong>
                        {ticket.ticketCode} — {ticket.title}
                      </strong>
                      <span>
                        <Badge
                          tone={
                            ticket.status === "Resolved" || ticket.status === "Closed"
                              ? "good"
                              : ticket.priority === "Critical" || ticket.priority === "High"
                                ? "bad"
                                : "default"
                          }
                        >
                          {ticket.status}
                        </Badge>{" "}
                        · {ticket.priority} · {formatDateTime(ticket.createdAt)}
                      </span>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="section-lead">Chưa có ticket nào.</p>
              )}
            </Panel>
          </div>
        </main>

        <aside className="inspector">
          <Panel>
            <span className="eyebrow">Next step</span>
            <h3>Go to Tickets</h3>
            <p className="section-lead">
              Truy cập trang Tickets để tạo mới, phân công, hoặc quản lý tiến trình xử lý.
            </p>
            <Link className="button primary" to={appRoutes.tickets}>
              Open tickets
            </Link>
          </Panel>

          <Panel>
            <span className="eyebrow">API layer</span>
            <h3>Kết nối hoàn tất</h3>
            <p className="section-lead">
              Dashboard đang hiển thị dữ liệu thời gian thực từ backend API — không còn sử dụng mock
              data.
            </p>
          </Panel>
        </aside>
      </div>

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
