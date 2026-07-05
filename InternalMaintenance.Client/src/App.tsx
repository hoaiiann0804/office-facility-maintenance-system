import { useState } from "react";
import { wireframeData } from "./data/wireframeData";
import type {
  Equipment,
  Priority,
  Ticket,
  TicketStatus,
  User,
  Role,
} from "./types/wireframe";
import { Badge, EmptyState, Panel, StatCard } from "./components/ui";

type Banner = {
  tone: "info" | "success" | "error";
  message: string;
};

type View = "overview" | "tickets" | "catalog" | "users" | "api";

type AuthDraft = {
  email: string;
  password: string;
};

const clone = <T,>(value: T): T => JSON.parse(JSON.stringify(value)) as T;

const initialBanner: Banner = {
  tone: "info",
  message: "Sẵn sàng cho wireframe split view. Hãy đăng nhập để xem dashboard.",
};

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
};

const formatDate = (value: string | null | undefined) => {
  if (!value) return "N/A";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
  }).format(date);
};

const roleTone = (role?: Role) => {
  if (role === "Admin") return "primary";
  if (role === "Manager") return "warn";
  if (role === "Technician") return "good";
  return "default";
};

const statusTone = (status?: TicketStatus) => {
  if (status === "Closed" || status === "Resolved") return "good";
  if (status === "InProgress" || status === "Assigned") return "warn";
  if (status === "Cancelled") return "bad";
  return "primary";
};

const priorityTone = (priority?: Priority) => {
  if (priority === "Critical") return "bad";
  if (priority === "High") return "warn";
  if (priority === "Medium") return "primary";
  return "default";
};

const equipmentTone = (status?: Equipment["status"]) => {
  if (status === "Active") return "good";
  if (status === "UnderMaintenance") return "warn";
  if (status === "Inactive") return "default";
  return "bad";
};

function App() {
  const [view, setView] = useState<View>("overview");
  const [banner, setBanner] = useState<Banner>(initialBanner);
  const [currentUserId, setCurrentUserId] = useState<number | null>(null);
  const [draft, setDraft] = useState<AuthDraft>({
    email: wireframeData.quickLogins[0]?.email ?? "",
    password: wireframeData.quickLogins[0]?.password ?? "",
  });
  const [search, setSearch] = useState("");
  const [ticketStatus, setTicketStatus] = useState<"All" | TicketStatus>("All");
  const [ticketPriority, setTicketPriority] = useState<"All" | Priority>("All");
  const [selectedTicketId, setSelectedTicketId] = useState<number>(wireframeData.tickets[0]?.id ?? 0);
  const [tickets, setTickets] = useState<Ticket[]>(() => clone(wireframeData.tickets));
  const [users, setUsers] = useState<User[]>(() => clone(wireframeData.users));
  const [departments] = useState(() => clone(wireframeData.departments));
  const [equipment] = useState<Equipment[]>(() => clone(wireframeData.equipment));
  const [assignTechnicianId, setAssignTechnicianId] = useState<string>("");
  const [assignmentNote, setAssignmentNote] = useState("");
  const [resolutionNote, setResolutionNote] = useState("");
  const [commentDraft, setCommentDraft] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const currentUser = users.find((user) => user.id === currentUserId) ?? null;
  const selectedTicket = tickets.find((ticket) => ticket.id === selectedTicketId) ?? tickets[0] ?? null;
  const technicians = users.filter((user) => user.role === "Technician" && user.isActive);
  const isAdmin = currentUser?.role === "Admin";
  const isManager = currentUser?.role === "Manager";
  const canManageCatalog = isAdmin;
  const canEditEquipment = isAdmin || isManager;

  const login = (email: string, password: string) => {
    const user = users.find((item) => item.email.toLowerCase() === email.toLowerCase());
    if (!user || user.password !== password) {
      setBanner({ tone: "error", message: "Email hoặc mật khẩu không đúng." });
      return;
    }

    setCurrentUserId(user.id);
    setView("overview");
    setSelectedTicketId(tickets[0]?.id ?? 0);
    setBanner({
      tone: "success",
      message: `Chào mừng ${user.fullName}. Đang mở dashboard theo vai trò ${user.role}.`,
    });
  };

  const logout = () => {
    setCurrentUserId(null);
    setView("overview");
    setSearch("");
    setTicketStatus("All");
    setTicketPriority("All");
    setAssignTechnicianId("");
    setAssignmentNote("");
    setResolutionNote("");
    setCommentDraft("");
    setCurrentPassword("");
    setNewPassword("");
    setBanner({
      tone: "info",
      message: "Đã đăng xuất. Bạn có thể chọn quick login khác ở panel bên trái.",
    });
  };

  const updateSelectedTicket = (patcher: (ticket: Ticket) => Ticket) => {
    if (!selectedTicket) return;
    setTickets((items) => items.map((ticket) => (ticket.id === selectedTicket.id ? patcher(ticket) : ticket)));
  };

  const handleAssignTicket = () => {
    if (!selectedTicket) return;

    const techId = Number(assignTechnicianId);
    if (!techId) {
      setBanner({ tone: "error", message: "Hãy chọn technician." });
      return;
    }

    const technician = users.find((user) => user.id === techId && user.role === "Technician");
    if (!technician) {
      setBanner({ tone: "error", message: "Người được chọn phải là technician." });
      return;
    }

    const note = assignmentNote.trim() || "Assigned from React migration.";
    updateSelectedTicket((ticket) => ({
      ...ticket,
      assignedTechnicianId: technician.id,
      assignedTechnicianName: technician.fullName,
      status: "Assigned",
      history: [
        ...ticket.history,
        {
          id: ticket.history.length + 1,
          status: "Assigned",
          note,
          changedBy: currentUser?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        },
      ],
    }));
    setBanner({ tone: "success", message: `Đã assign ${selectedTicket.ticketCode} cho ${technician.fullName}.` });
  };

  const moveTicketStatus = (status: Exclude<TicketStatus, "Cancelled">) => {
    if (!selectedTicket) return;
    if (status === "Resolved" && !resolutionNote.trim()) {
      setBanner({ tone: "error", message: "Resolution note là bắt buộc khi chuyển sang Resolved." });
      return;
    }

    const note = status === "Resolved" ? resolutionNote.trim() : `Status changed to ${status}.`;
    updateSelectedTicket((ticket) => ({
      ...ticket,
      status,
      resolutionNote: status === "Resolved" ? note : ticket.resolutionNote,
      resolvedAt: status === "Resolved" ? new Date().toISOString() : ticket.resolvedAt,
      closedAt: status === "Closed" ? new Date().toISOString() : ticket.closedAt,
      history: [
        ...ticket.history,
        {
          id: ticket.history.length + 1,
          status,
          note,
          changedBy: currentUser?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        },
      ],
    }));
    setBanner({ tone: "success", message: `${selectedTicket.ticketCode} đã chuyển sang ${status}.` });
  };

  const cancelTicket = () => {
    if (!selectedTicket) return;
    updateSelectedTicket((ticket) => ({
      ...ticket,
      status: "Cancelled",
      history: [
        ...ticket.history,
        {
          id: ticket.history.length + 1,
          status: "Cancelled",
          note: "Cancelled from React migration.",
          changedBy: currentUser?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        },
      ],
    }));
    setBanner({ tone: "success", message: `${selectedTicket.ticketCode} đã bị huỷ.` });
  };

  const addComment = () => {
    if (!selectedTicket) return;
    const content = commentDraft.trim();
    if (!content) {
      setBanner({ tone: "error", message: "Comment không được để trống." });
      return;
    }

    updateSelectedTicket((ticket) => ({
      ...ticket,
      comments: [ 
        ...ticket.comments,
        {
          id: ticket.comments.length + 1,
          userName: currentUser?.fullName ?? "System",
          content,
          createdAt: new Date().toISOString(),
        },
      ],
    }));
    setCommentDraft("");
    setBanner({ tone: "success", message: "Đã thêm comment." });
  };

  const handleChangePassword = () => {
    if (!currentUser) return;
    if (!currentPassword.trim() || !newPassword.trim()) {
      setBanner({ tone: "error", message: "Nhập đủ current password và new password." });
      return;
    }
    if (currentUser.password !== currentPassword.trim()) {
      setBanner({ tone: "error", message: "Current password không đúng." });
      return;
    }
    if (currentPassword.trim() === newPassword.trim()) {
      setBanner({ tone: "error", message: "Mật khẩu mới phải khác mật khẩu hiện tại." });
      return;
    }

    setUsers((items) =>
      items.map((user) =>
        user.id === currentUser.id
          ? { ...user, password: newPassword.trim(), mustChangePassword: false }
          : user,
      ),
    );
    setCurrentPassword("");
    setNewPassword("");
    setBanner({ tone: "success", message: "Đã đổi mật khẩu. Có thể đăng xuất để kiểm tra flow." });
  };

  const filteredTickets = tickets.filter((ticket) => {
    const matchesSearch =
      !search ||
      `${ticket.ticketCode} ${ticket.title} ${ticket.description} ${ticket.equipmentName}`.toLowerCase().includes(search.toLowerCase());
    const matchesStatus = ticketStatus === "All" || ticket.status === ticketStatus;
    const matchesPriority = ticketPriority === "All" || ticket.priority === ticketPriority;
    return matchesSearch && matchesStatus && matchesPriority;
  });

  const recentActivity = [
    ...tickets.flatMap((ticket) =>
      ticket.history.map((item) => ({
        kind: "status" as const,
        text: `${ticket.ticketCode} -> ${item.status}`,
        detail: item.note || "Status changed",
        time: item.changedAt,
      })),
    ),
    ...tickets.flatMap((ticket) =>
      ticket.comments.map((comment) => ({
        kind: "comment" as const,
        text: `${ticket.ticketCode} comment by ${comment.userName}`,
        detail: comment.content,
        time: comment.createdAt,
      })),
    ),
  ]
    .sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime())
    .slice(0, 6);

  const openTickets = tickets.filter((ticket) => !["Closed", "Cancelled"].includes(ticket.status)).length;
  const activeEquipment = equipment.filter((item) => item.status === "Active").length;
  const techniciansCount = users.filter((user) => user.role === "Technician").length;

  return (
    <div className="app-shell">
      {!currentUser ? (
        <main className="auth-screen">
          <section className="auth-hero panel">
            <span className="eyebrow">Internal maintenance management</span>
            <h1>Điều phối bảo trì nội bộ, gọn, rõ, dễ mở rộng.</h1>
            <p className="hero-copy">
              Đây là bản React hóa từ wireframe HTML. Mục tiêu là giữ đúng tinh thần dashboard split-view,
              đồng thời tách data, state và UI thành các lớp dễ bảo trì.
            </p>

            <div className="stats-grid">
              <StatCard label="Login" value="JWT + refresh" note="Ready for auth flow" />
              <StatCard label="Catalog" value="Departments + equipment" note="Data-driven modules" />
              <StatCard label="Workflow" value="Ticket history" note="Audit-friendly" />
              <StatCard label="Roles" value="Admin / Manager / Staff / Technician" note="RBAC first" />
            </div>

            <div className="login-note panel subtle">
              <span className="eyebrow">Quick login</span>
              <div className="quick-login-list">
                {wireframeData.quickLogins.map((item) => (
                  <button
                    key={item.email}
                    type="button"
                    className="quick-login-item"
                    onClick={() => setDraft({ email: item.email, password: item.password })}
                  >
                    <div className="quick-login-top">
                      <strong>{item.label}</strong>
                      <Badge tone={roleTone(item.role)}>{item.role}</Badge>
                    </div>
                    <span>{item.email}</span>
                    <small>{item.hint}</small>
                  </button>
                ))}
              </div>
            </div>
          </section>

          <section className="auth-card panel panel-light">
            <span className="eyebrow eyebrow-dark">Demo login</span>
            <h2>Vào khu mô phỏng</h2>
            <p className="section-lead">Chỉ giữ layout mới, không còn các block template mặc định của Vite.</p>

            <div className="auth-form">
              <label className="field">
                <span>Email</span>
                <input
                  className="input"
                  type="email"
                  value={draft.email}
                  placeholder="admin@test.com"
                  onChange={(event) => setDraft((prev) => ({ ...prev, email: event.target.value }))}
                />
              </label>

              <label className="field">
                <span>Password</span>
                <input
                  className="input"
                  type="password"
                  value={draft.password}
                  placeholder="Temp@123456"
                  onChange={(event) => setDraft((prev) => ({ ...prev, password: event.target.value }))}
                />
              </label>

              <button type="button" className="button primary" onClick={() => login(draft.email, draft.password)}>
                Đăng nhập
              </button>
            </div>

            <div className={`banner ${banner.tone}`}>{banner.message}</div>

            <div className="api-alignment panel subtle">
              <span className="eyebrow eyebrow-dark">API alignment</span>
              <div className="stack">
                <div className="mini-card">
                  <strong>Auth</strong>
                  <span>login, me, change-password, refresh-token, logout</span>
                </div>
                <div className="mini-card">
                  <strong>Tickets</strong>
                  <span>create, assign, status, comments, history</span>
                </div>
                <div className="mini-card">
                  <strong>Catalog</strong>
                  <span>departments, equipment, users</span>
                </div>
              </div>
            </div>
          </section>
        </main>
      ) : (
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
              {[
                { id: "overview", label: "Overview" },
                { id: "tickets", label: "Tickets" },
                { id: "catalog", label: "Catalog" },
                { id: "users", label: "Users" },
                { id: "api", label: "API map" },
              ].map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className={`tab ${view === item.id ? "active" : ""}`}
                  onClick={() => setView(item.id as View)}
                >
                  {item.label}
                </button>
              ))}
            </nav>

            <button type="button" className="button secondary" onClick={logout}>
              Đăng xuất
            </button>
          </header>

          <div className={`banner ${banner.tone}`}>{banner.message}</div>

          <div className="layout">
            <main className="main-panel">
              {view === "overview" ? (
                <div className="view-grid">
                  <Panel>
                    <span className="eyebrow">Overview</span>
                    <h2>Dashboard overview</h2>
                    <p className="section-lead">
                      Bản nháp này giữ đúng cấu trúc dashboard còn hành vi được viết bằng React state thay vì DOM script.
                    </p>
                    <div className="stats-grid compact">
                      <StatCard label="Open tickets" value={openTickets} />
                      <StatCard label="Active equipment" value={activeEquipment} />
                      <StatCard label="Technicians" value={techniciansCount} />
                      <StatCard label="Departments" value={departments.length} />
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
                            <Badge tone={index === 0 ? "primary" : index === wireframeData.workflow.length - 1 ? "good" : "default"}>
                              {index === 0 ? "entry" : index === wireframeData.workflow.length - 1 ? "finish" : "step"}
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

                  <Panel>
                    <span className="eyebrow">Rules</span>
                    <h2>Business guardrails</h2>
                    <div className="stack">
                      <div className="mini-card">
                        <strong>No duplicate open ticket for one asset</strong>
                        <span>The backend blocks ticket spam on active equipment.</span>
                      </div>
                      <div className="mini-card">
                        <strong>Equipment code stays immutable</strong>
                        <span>Edit form keeps the code as a protected field after creation.</span>
                      </div>
                      <div className="mini-card">
                        <strong>Closed tickets are final</strong>
                        <span>The workflow is intentionally strict so the history remains auditable.</span>
                      </div>
                    </div>
                  </Panel>
                </div>
              ) : null}

              {view === "tickets" ? (
                <div className="view-grid tickets-grid">
                  <Panel>
                    <span className="eyebrow">Tickets</span>
                    <h2>Ticket queue</h2>
                    <p className="section-lead">
                      Search, filter, select, then act on the ticket. Column phải luôn giữ vai trò chi tiết.
                    </p>

                    <div className="filter-grid">
                      <label className="field">
                        <span>Search</span>
                        <input
                          className="input"
                          value={search}
                          placeholder="Ticket code, title, description"
                          onChange={(event) => setSearch(event.target.value)}
                        />
                      </label>
                      <label className="field">
                        <span>Status</span>
                        <select className="select" value={ticketStatus} onChange={(event) => setTicketStatus(event.target.value as TicketStatus | "All")}>
                          {["All", ...wireframeData.workflow, "Cancelled"].map((status) => (
                            <option key={status} value={status}>
                              {status}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className="field">
                        <span>Priority</span>
                        <select className="select" value={ticketPriority} onChange={(event) => setTicketPriority(event.target.value as Priority | "All")}>
                          {["All", ...wireframeData.priorities].map((priority) => (
                            <option key={priority} value={priority}>
                              {priority}
                            </option>
                          ))}
                        </select>
                      </label>
                    </div>

                    <div className="list">
                      {filteredTickets.map((ticket) => (
                        <button
                          key={ticket.id}
                          type="button"
                          className={`list-item ${ticket.id === selectedTicket?.id ? "selected" : ""}`}
                          onClick={() => {
                            setSelectedTicketId(ticket.id);
                            setAssignTechnicianId(String(ticket.assignedTechnicianId ?? ""));
                            setResolutionNote(ticket.resolutionNote);
                          }}
                        >
                          <div className="list-item-header">
                            <div>
                              <strong>{ticket.ticketCode}</strong>
                              <span>{ticket.title}</span>
                            </div>
                            <div className="badge-row">
                              <Badge tone={statusTone(ticket.status)}>{ticket.status}</Badge>
                              <Badge tone={priorityTone(ticket.priority)}>{ticket.priority}</Badge>
                            </div>
                          </div>
                          <span className="muted-line">
                            {ticket.equipmentName} · {ticket.createdByUserName} · {formatDateTime(ticket.createdAt)}
                          </span>
                        </button>
                      ))}
                      {!filteredTickets.length ? (
                        <EmptyState title="Không có ticket phù hợp" description="Thử đổi từ khóa hoặc reset bộ lọc hiện tại." />
                      ) : null}
                    </div>
                  </Panel>

                  <Panel>
                    {selectedTicket ? (
                      <>
                        <span className="eyebrow">Detail</span>
                        <h2>{selectedTicket.ticketCode}</h2>
                        <p className="section-lead">{selectedTicket.title}</p>
                        <div className="badge-row">
                          <Badge tone={statusTone(selectedTicket.status)}>{selectedTicket.status}</Badge>
                          <Badge tone={priorityTone(selectedTicket.priority)}>{selectedTicket.priority}</Badge>
                          <Badge tone="default">Created by {selectedTicket.createdByUserName}</Badge>
                        </div>

                        <div className="stack spaced">
                          <div className="mini-card">
                            <strong>Description</strong>
                            <span>{selectedTicket.description}</span>
                          </div>
                          <div className="mini-card">
                            <strong>Equipment</strong>
                            <span>
                              {selectedTicket.equipmentCode} - {selectedTicket.equipmentName}
                            </span>
                          </div>
                          <div className="mini-card">
                            <strong>Current assignment</strong>
                            <span>{selectedTicket.assignedTechnicianName ?? "Unassigned"}</span>
                          </div>
                        </div>

                        <div className="control-card">
                          <label className="field">
                            <span>Assign technician</span>
                            <select
                              className="select"
                              value={assignTechnicianId}
                              onChange={(event) => setAssignTechnicianId(event.target.value)}
                            >
                              <option value="">Select technician</option>
                              {technicians.map((tech) => (
                                <option key={tech.id} value={tech.id}>
                                  {tech.fullName}
                                </option>
                              ))}
                            </select>
                          </label>
                          <label className="field">
                            <span>Assignment note</span>
                            <textarea
                              className="textarea"
                              value={assignmentNote}
                              placeholder="Optional note for the history timeline"
                              onChange={(event) => setAssignmentNote(event.target.value)}
                            />
                          </label>
                          <div className="button-row">
                            <button type="button" className="button primary" onClick={handleAssignTicket}>
                              Assign
                            </button>
                            <button type="button" className="button secondary" onClick={() => moveTicketStatus("InProgress")}>
                              Mark In Progress
                            </button>
                            <button type="button" className="button secondary" onClick={() => moveTicketStatus("Resolved")}>
                              Resolve
                            </button>
                            <button type="button" className="button secondary" onClick={() => moveTicketStatus("Closed")}>
                              Close
                            </button>
                            <button type="button" className="button danger" onClick={cancelTicket}>
                              Cancel
                            </button>
                          </div>
                          <label className="field">
                            <span>Resolution note</span>
                            <textarea
                              className="textarea"
                              value={resolutionNote}
                              placeholder="Required when moving to Resolved"
                              onChange={(event) => setResolutionNote(event.target.value)}
                            />
                          </label>
                        </div>

                        <div className="control-card">
                          <label className="field">
                            <span>Add comment</span>
                            <textarea
                              className="textarea"
                              value={commentDraft}
                              placeholder="Leave a note on the ticket"
                              onChange={(event) => setCommentDraft(event.target.value)}
                            />
                          </label>
                          <button type="button" className="button primary" onClick={addComment}>
                            Add comment
                          </button>
                        </div>

                        <div className="mini-grid">
                          <div className="mini-card">
                            <strong>Timeline</strong>
                            <div className="stack">
                              {selectedTicket.history.map((item) => (
                                <div key={item.id} className="timeline-item compact">
                                  <strong>{item.status}</strong>
                                  <span>
                                    {item.note} · {item.changedBy} · {formatDateTime(item.changedAt)}
                                  </span>
                                </div>
                              ))}
                            </div>
                          </div>
                          <div className="mini-card">
                            <strong>Comments</strong>
                            <div className="stack">
                              {selectedTicket.comments.length ? (
                                selectedTicket.comments.map((comment) => (
                                  <div key={comment.id} className="comment-item">
                                    <strong>{comment.userName}</strong>
                                    <span>{comment.content}</span>
                                    <small>{formatDateTime(comment.createdAt)}</small>
                                  </div>
                                ))
                              ) : (
                                <EmptyState title="Chưa có comment" description="Ticket này chưa có bình luận nào." />
                              )}
                            </div>
                          </div>
                        </div>
                      </>
                    ) : (
                      <EmptyState title="No ticket selected" description="Chọn một ticket từ danh sách để xem chi tiết." />
                    )}
                  </Panel>
                </div>
              ) : null}

              {view === "catalog" ? (
                <div className="view-grid">
                  <Panel>
                    <span className="eyebrow">Catalog</span>
                    <h2>Departments</h2>
                    <div className="table-wrap">
                      <table className="table">
                        <thead>
                          <tr>
                            <th>Name</th>
                            <th>Description</th>
                            <th>Created</th>
                          </tr>
                        </thead>
                        <tbody>
                          {departments.map((department) => (
                            <tr key={department.id}>
                              <td>{department.name}</td>
                              <td>{department.description}</td>
                              <td>{formatDate(department.createdAt)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <div className={`banner ${canManageCatalog ? "success" : "info"}`}>
                      {canManageCatalog
                        ? "Admin có thể tạo, sửa và xoá department trong luồng thật."
                        : "Ở bản wireframe này, chỉ Admin mới được chỉnh catalog."}
                    </div>
                  </Panel>

                  <Panel>
                    <span className="eyebrow">Catalog</span>
                    <h2>Equipment</h2>
                    <div className="table-wrap">
                      <table className="table">
                        <thead>
                          <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Department</th>
                            <th>Status</th>
                            <th>Purchased</th>
                          </tr>
                        </thead>
                        <tbody>
                          {equipment.map((item) => (
                            <tr key={item.id}>
                              <td>{item.code}</td>
                              <td>{item.name}</td>
                              <td>{item.departmentName}</td>
                              <td>
                                <Badge tone={equipmentTone(item.status)}>{item.status}</Badge>
                              </td>
                              <td>{formatDate(item.purchasedDate)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <div className={`banner ${canEditEquipment ? "success" : "info"}`}>
                      {canEditEquipment
                        ? "Admin và Manager có thể quản lý equipment trong mô hình này."
                        : "Luồng equipment hiện được khóa cho Admin/Manager."}
                    </div>
                  </Panel>
                </div>
              ) : null}

              {view === "users" ? (
                <div className="view-grid">
                  <Panel>
                    <span className="eyebrow">Users</span>
                    <h2>Permissions at a glance</h2>
                    <div className="stack">
                      <div className="mini-card">
                        <strong>Admin</strong>
                        <span>Full management: users, departments, equipment, tickets, and workflow control.</span>
                      </div>
                      <div className="mini-card">
                        <strong>Manager</strong>
                        <span>Department scope, assignment, and close/cancel permissions for tickets in the same department.</span>
                      </div>
                      <div className="mini-card">
                        <strong>Technician</strong>
                        <span>Can work on assigned tickets and push status through the execution flow.</span>
                      </div>
                      <div className="mini-card">
                        <strong>Staff</strong>
                        <span>Can create tickets and follow the progress for their own requests.</span>
                      </div>
                    </div>
                  </Panel>

                  <Panel>
                    <span className="eyebrow">Directory</span>
                    <h2>Users</h2>
                    <div className="table-wrap">
                      <table className="table">
                        <thead>
                          <tr>
                            <th>Name</th>
                            <th>Role</th>
                            <th>Department</th>
                            <th>Status</th>
                            <th>Last login</th>
                          </tr>
                        </thead>
                        <tbody>
                          {users.map((user) => (
                            <tr key={user.id}>
                              <td>
                                <strong>{user.fullName}</strong>
                                <div className="muted-line">{user.email}</div>
                              </td>
                              <td>
                                <Badge tone={roleTone(user.role)}>{user.role}</Badge>
                              </td>
                              <td>{user.departmentName}</td>
                              <td>{user.isActive ? "Active" : "Inactive"}</td>
                              <td>{formatDateTime(user.lastLoginAt)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </Panel>
                </div>
              ) : null}

              {view === "api" ? (
                <div className="view-grid">
                  <Panel>
                    <span className="eyebrow">API map</span>
                    <h2>Backend surface area</h2>
                    <p className="section-lead">
                      Screen này được giữ để team frontend đọc cùng một mental model với backend và README.
                    </p>
                  </Panel>

                  <div className="api-grid">
                    {wireframeData.apiGroups.map((group) => (
                      <Panel key={group.title}>
                        <span className="eyebrow">{group.title}</span>
                        <div className="stack">
                          {group.routes.map((route) => (
                            <div key={`${group.title}-${route.path}`} className="mini-card">
                              <div className="card-row">
                                <strong>{route.path}</strong>
                                <Badge tone={route.method === "GET" ? "good" : route.method === "POST" ? "warn" : "primary"}>
                                  {route.method}
                                </Badge>
                              </div>
                              <span>{route.note}</span>
                            </div>
                          ))}
                        </div>
                      </Panel>
                    ))}
                  </div>
                </div>
              ) : null}
            </main>

            <aside className="inspector">
              <Panel>
                <span className="eyebrow">Current persona</span>
                <h3>{currentUser.fullName}</h3>
                <p className="section-lead">{currentUser.email}</p>
                <div className="badge-row">
                  <Badge tone={roleTone(currentUser.role)}>{currentUser.role}</Badge>
                  <Badge tone="default">{currentUser.departmentName}</Badge>
                  <Badge tone={currentUser.mustChangePassword ? "warn" : "good"}>
                    {currentUser.mustChangePassword ? "Must change password" : "Password ok"}
                  </Badge>
                </div>
              </Panel>

              <Panel>
                <span className="eyebrow">Selected ticket</span>
                {selectedTicket ? (
                  <>
                    <h3>{selectedTicket.ticketCode}</h3>
                    <p className="section-lead">{selectedTicket.title}</p>
                    <div className="stack">
                      <div className="mini-card">
                        <strong>Status</strong>
                        <span>{selectedTicket.status}</span>
                      </div>
                      <div className="mini-card">
                        <strong>Equipment</strong>
                        <span>
                          {selectedTicket.equipmentCode} - {selectedTicket.equipmentName}
                        </span>
                      </div>
                      <div className="mini-card">
                        <strong>Assigned</strong>
                        <span>{selectedTicket.assignedTechnicianName ?? "Unassigned"}</span>
                      </div>
                      <div className="mini-card">
                        <strong>Created</strong>
                        <span>{formatDateTime(selectedTicket.createdAt)}</span>
                      </div>
                    </div>
                  </>
                ) : (
                  <EmptyState title="No ticket" description="Chưa chọn ticket nào." />
                )}
              </Panel>

              <Panel>
                <span className="eyebrow">Auth flow</span>
                <h3>Change password</h3>
                <div className="auth-form compact">
                  <label className="field">
                    <span>Current password</span>
                    <input
                      className="input"
                      type="password"
                      value={currentPassword}
                      onChange={(event) => setCurrentPassword(event.target.value)}
                    />
                  </label>
                  <label className="field">
                    <span>New password</span>
                    <input
                      className="input"
                      type="password"
                      value={newPassword}
                      onChange={(event) => setNewPassword(event.target.value)}
                    />
                  </label>
                  <button type="button" className="button primary" onClick={handleChangePassword}>
                    Save new password
                  </button>
                </div>
              </Panel>
            </aside>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
