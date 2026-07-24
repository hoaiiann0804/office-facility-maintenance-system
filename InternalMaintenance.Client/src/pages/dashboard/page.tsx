import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Badge, Panel, Spinner, StatCard, UserProfile } from "../../shared/ui";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { appRoutes } from "../../shared/config/routes";
import { logout } from "../../shared/api/auth";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";
import { useTicketsQuery } from "../../features/tickets/api/use-tickets-query";
import { useDashboardSummaryQuery } from "../../features/dashboard/api/use-dashboard-summary-query";
import { useDashboardChartsQuery } from "../../features/dashboard/api/use-dashboard-charts-query";
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
} from "recharts";

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

  const { data: summary, isLoading: isSummaryLoading } = useDashboardSummaryQuery();
  const { data: charts, isLoading: isChartsLoading } = useDashboardChartsQuery();
  const { data: ticketsPage, isLoading: isTicketsLoading } = useTicketsQuery({ pageSize: 5 });

  const recentTickets = ticketsPage?.items ?? [];
  const isStatsLoading = isSummaryLoading || isChartsLoading || isTicketsLoading;

  const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#8884d8", "#82ca9d"];

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
              {isStatsLoading || !summary ? (
                <Spinner />
              ) : (
                <div className="stats-grid compact">
                  <StatCard label="Open tickets" value={summary.openTickets} />
                  <StatCard label="Active equipment" value={summary.activeEquipment} />
                  <StatCard label="Technicians" value={summary.totalTechnicians} />
                  <StatCard label="Departments" value={summary.totalDepartments} />
                </div>
              )}
            </Panel>

            <Panel>
              <span className="eyebrow">Analytics</span>
              <h2>Biểu đồ phân tích</h2>
              {isStatsLoading || !charts ? (
                <Spinner />
              ) : (
                <div
                  className="filter-grid"
                  style={{ gridTemplateColumns: "1fr 1fr", gap: "24px" }}
                >
                  <div
                    className="mini-card"
                    style={{ height: "300px", display: "flex", flexDirection: "column" }}
                  >
                    <strong>Tickets theo trạng thái</strong>
                    <div style={{ flex: 1, minHeight: 0, marginTop: 12 }}>
                      <ResponsiveContainer width="100%" height="100%">
                        <PieChart>
                          <Pie
                            data={charts.ticketsByStatus}
                            dataKey="value"
                            nameKey="name"
                            cx="50%"
                            cy="50%"
                            outerRadius={80}
                            label
                          >
                            {charts.ticketsByStatus.map((_, index) => (
                              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                            ))}
                          </Pie>
                          <Tooltip />
                        </PieChart>
                      </ResponsiveContainer>
                    </div>
                  </div>
                  <div
                    className="mini-card"
                    style={{ height: "300px", display: "flex", flexDirection: "column" }}
                  >
                    <strong>Thiết bị theo phòng ban</strong>
                    <div style={{ flex: 1, minHeight: 0, marginTop: 12 }}>
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart
                          data={charts.equipmentByDepartment}
                          margin={{ top: 20, right: 30, left: 0, bottom: 5 }}
                        >
                          <CartesianGrid strokeDasharray="3 3" />
                          <XAxis dataKey="name" />
                          <YAxis />
                          <Tooltip />
                          <Bar dataKey="value" fill="#8884d8" />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  </div>
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
          {/* <Panel>
            <span className="eyebrow">Next step</span>
            <h3>Go to Tickets</h3>
            <p className="section-lead">
              Truy cập trang Tickets để tạo mới, phân công, hoặc quản lý tiến trình xử lý.
            </p>
            <Link className="button primary" to={appRoutes.tickets}>
              Open tickets
            </Link>
          </Panel> */}

          {/* <Panel>
            <span className="eyebrow">API layer</span>
            <h3>Kết nối hoàn tất</h3>
            <p className="section-lead">
              Dashboard đang hiển thị dữ liệu thời gian thực từ backend API — không còn sử dụng mock
              data.
            </p>
          </Panel> */}
        </aside>
      </div>

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
