import { Link } from "react-router-dom";
import { wireframeData } from "../../shared/mock/wireframe-data";
import { Badge, Panel, StatCard } from "../../shared/ui";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { appRoutes } from "../../shared/config/routes";

const formatDateTime = (value: string) =>
  new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));

export function DashboardPage() {
  const session = useAuthStore((state) => state.session);

  const openTickets = wireframeData.tickets.filter(
    (ticket) => !["Closed", "Cancelled"].includes(ticket.status),
  ).length;
  const activeEquipment = wireframeData.equipment.filter((item) => item.status === "Active").length;

  const recentActivity = [
    ...wireframeData.tickets.flatMap((ticket) =>
      ticket.history.map((item) => ({
        text: `${ticket.ticketCode} -> ${item.status}`,
        detail: item.note,
        time: item.changedAt,
      })),
    ),
    ...wireframeData.tickets.flatMap((ticket) =>
      ticket.comments.map((comment) => ({
        text: `${ticket.ticketCode} comment by ${comment.userName}`,
        detail: comment.content,
        time: comment.createdAt,
      })),
    ),
  ]
    .sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime())
    .slice(0, 6);

  return (
    <div className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">IM</div>
          <div>
            <strong>Management Console</strong>
            <span>Split view React migration</span>
          </div>
        </div>

        <nav className="tabs" aria-label="Modules">
          <Link className="tab active" to={appRoutes.dashboard}>
            Dashboard
          </Link>
          <Link className="tab" to={appRoutes.tickets}>
            Tickets
          </Link>
        </nav>

        <div className="badge-row">
          <Badge tone="default">{session?.user.fullName ?? "Guest"}</Badge>
          <Badge tone={session?.user.roleName === "Admin" ? "primary" : "default"}>
            {session?.user.roleName ?? "Guest"}
          </Badge>
        </div>
      </header>

      <div className="layout">
        <main className="main-panel">
          <div className="view-grid">
            <Panel>
              <span className="eyebrow">Overview</span>
              <h2>Dashboard overview</h2>
              <p className="section-lead">
                Đây là page dashboard tách riêng, không còn nhồi chung trong một component monolith.
              </p>
              <div className="stats-grid compact">
                <StatCard label="Open tickets" value={openTickets} />
                <StatCard label="Active equipment" value={activeEquipment} />
                <StatCard
                  label="Technicians"
                  value={
                    wireframeData.users.filter((user) => user.roleName === "Technician").length
                  }
                />
                <StatCard label="Departments" value={wireframeData.departments.length} />
              </div>
            </Panel>

            <Panel>
              <span className="eyebrow">Workflow</span>
              <h2>Ticket life cycle</h2>
              <div className="stack">
                {wireframeData.workflow.map((step, index) => (
                  <div key={step} className="mini-card">
                    <div className="card-row">
                      <strong>
                        {index + 1}. {step}
                      </strong>
                      <Badge
                        tone={
                          index === 0
                            ? "primary"
                            : index === wireframeData.workflow.length - 1
                              ? "good"
                              : "default"
                        }
                      >
                        {index === 0
                          ? "entry"
                          : index === wireframeData.workflow.length - 1
                            ? "finish"
                            : "step"}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </Panel>

            <Panel>
              <span className="eyebrow">Activity</span>
              <h2>Latest updates</h2>
              <div className="timeline">
                {recentActivity.map((item) => (
                  <article key={`${item.text}-${item.time}`} className="timeline-item">
                    <strong>{item.text}</strong>
                    <span>
                      {item.detail} · {formatDateTime(item.time)}
                    </span>
                  </article>
                ))}
              </div>
            </Panel>
          </div>
        </main>

        <aside className="inspector">
          <Panel>
            <span className="eyebrow">Next step</span>
            <h3>Go to Tickets</h3>
            <p className="section-lead">
              Màn tickets đã được tách riêng để sau này thêm filter, detail, assign, comments dễ
              hơn.
            </p>
            <Link className="button primary" to={appRoutes.tickets}>
              Open tickets
            </Link>
          </Panel>

          <Panel>
            <span className="eyebrow">API layer</span>
            <h3>Ready to connect</h3>
            <p className="section-lead">
              `shared/api/*` đã chuẩn bị sẵn endpoint `/api/auth/*` và `/api/tickets/*` từ backend
              ASP.NET.
            </p>
          </Panel>
        </aside>
      </div>
    </div>
  );
}
