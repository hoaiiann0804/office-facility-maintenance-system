import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { wireframeData } from "../../shared/mock/wireframe-data";
import { Badge, EmptyState, Panel, Spinner, UserProfile } from "../../shared/ui";
import { TicketBoard } from "../../features/tickets/components/ticket-board";
import { appRoutes } from "../../shared/config/routes";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { logout } from "../../shared/api/auth";
import type {
  TicketHistoryItem,
  TicketPriority,
  TicketStatus,
} from "../../entities/ticket/model/types";
import { useTicketDetailQuery } from "../../features/tickets/api/use-ticket-detail-query";
import { useTicketsQuery } from "../../features/tickets/api/use-tickets-query";
import { CreateTicketModal } from "../../features/tickets/components/create-ticket-modal";
import { EditTicketModal } from "../../features/tickets/components/edit-ticket-modal";
import { TicketActionPanel } from "../../features/tickets/components/ticket-action-panel";
import { ChangePasswordModal } from "../../features/auth/components/change-password-modal";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
};

export function TicketsPage() {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);
  const navigate = useNavigate();

  const isAdmin = session?.user.roleName === "Admin";

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

  const [search, setSearch] = useState("");
  const [ticketStatus, setTicketStatus] = useState<"All" | TicketStatus>("All");
  const [ticketPriority, setTicketPriority] = useState<"All" | TicketPriority>("All");
  const [selectedTicketId, setSelectedTicketId] = useState<number | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isChangePasswordOpen, setIsChangePasswordOpen] = useState(false);

  const {
    data: ticketsPage,
    isLoading,
    isError,
  } = useTicketsQuery({
    // keyword: search, // API chưa hỗ trợ, tạm thời vô hiệu hóa
    status: ticketStatus === "All" ? undefined : ticketStatus,
    priority: ticketPriority === "All" ? undefined : ticketPriority,
  });

  const tickets = useMemo(() => ticketsPage?.items ?? [], [ticketsPage?.items]);
  const filteredTickets = useMemo(() => {
    const keyword = search.trim().toLowerCase();

    if (!keyword) {
      return tickets;
    }

    return tickets.filter((ticket) =>
      [
        ticket.ticketCode,
        ticket.title,
        ticket.description,
        ticket.equipmentName,
        ticket.equipmentCode,
        ticket.createdByUserName,
      ]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(keyword)),
    );
  }, [search, tickets]);

  const activeTicketId = useMemo(() => {
    if (selectedTicketId && filteredTickets.some((ticket) => ticket.id === selectedTicketId)) {
      return selectedTicketId;
    }

    return filteredTickets[0]?.id ?? null;
  }, [selectedTicketId, filteredTickets]);

  const {
    data: selectedTicket,
    isLoading: isSelectedTicketLoading,
    isError: isSelectedTicketError,
  } = useTicketDetailQuery(activeTicketId);

  return (
    <div className="dashboard">
      <header className="topbar">
        <div className="brand">
          <div className="brand-mark">IM</div>
          <div>
            <strong>Management Console</strong>
            <span>Tickets workspace</span>
          </div>
        </div>

        <nav className="tabs" aria-label="Modules">
          <Link className="tab" to={appRoutes.dashboard}>
            Dashboard
          </Link>
          <Link className="tab active" to={appRoutes.tickets}>
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
          <div className="view-grid tickets-grid">
            <Panel>
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "flex-start",
                }}
              >
                <div>
                  <span className="eyebrow">Tickets</span>
                  <h2>Ticket queue</h2>
                </div>
                <button
                  type="button"
                  className="button primary"
                  onClick={() => setIsCreateModalOpen(true)}
                >
                  + Create Ticket
                </button>
              </div>
              <p className="section-lead">
                Chỗ này đã tách thành page riêng để sau này gắn react-query, filter, table,
                pagination dễ hơn.
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
                  <select
                    className="select"
                    value={ticketStatus}
                    onChange={(event) =>
                      setTicketStatus(event.target.value as TicketStatus | "All")
                    }
                  >
                    {["All", ...wireframeData.workflow, "Cancelled"].map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="field">
                  <span>Priority</span>
                  <select
                    className="select"
                    value={ticketPriority}
                    onChange={(event) =>
                      setTicketPriority(event.target.value as TicketPriority | "All")
                    }
                  >
                    {["All", ...wireframeData.priorities].map((priority) => (
                      <option key={priority} value={priority}>
                        {priority}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="list">
                {isLoading ? (
                  <div className="centered-content">
                    <Spinner />
                  </div>
                ) : isError ? (
                  <EmptyState
                    title="Đã có lỗi xảy ra"
                    description="Không thể tải danh sách ticket. Vui lòng thử lại sau."
                  />
                ) : filteredTickets.length > 0 ? (
                  filteredTickets.map((ticket) => (
                    <button
                      key={ticket.id}
                      type="button"
                      className={`list-item ${ticket.id === activeTicketId ? "selected" : ""}`}
                      onClick={() => setSelectedTicketId(ticket.id)}
                    >
                      <div className="list-item-header">
                        <div>
                          <strong>{ticket.ticketCode}</strong>
                          <span>{ticket.title}</span>
                        </div>
                        <div className="badge-row">
                          <Badge
                            tone={
                              ticket.status === "Resolved" || ticket.status === "Closed"
                                ? "good"
                                : ticket.status === "Cancelled"
                                  ? "bad"
                                  : "warn"
                            }
                          >
                            {ticket.status}
                          </Badge>
                          <Badge
                            tone={
                              ticket.priority === "Critical"
                                ? "bad"
                                : ticket.priority === "High"
                                  ? "warn"
                                  : ticket.priority === "Medium"
                                    ? "primary"
                                    : "default"
                            }
                          >
                            {ticket.priority}
                          </Badge>
                        </div>
                      </div>
                      <span className="muted-line">
                        {ticket.equipmentName} · {ticket.createdByUserName} ·{" "}
                        {formatDateTime(ticket.createdAt)}
                      </span>
                    </button>
                  ))
                ) : (
                  <EmptyState
                    title="Không có ticket phù hợp"
                    description="Thử đổi từ khóa hoặc reset bộ lọc hiện tại."
                  />
                )}
              </div>
            </Panel>

            <Panel>
              {isSelectedTicketLoading ? (
                <div className="centered-content">
                  <Spinner />
                </div>
              ) : isSelectedTicketError ? (
                <EmptyState
                  title="Không thể tải chi tiết"
                  description="Đã có lỗi xảy ra khi tải dữ liệu ticket."
                />
              ) : selectedTicket ? (
                <>
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "flex-start",
                    }}
                  >
                    <div>
                      <span className="eyebrow">Detail</span>
                      <h2>{selectedTicket.ticketCode}</h2>
                      <p className="section-lead">{selectedTicket.title}</p>
                    </div>
                    {(selectedTicket.status === "Pending" ||
                      selectedTicket.status === "Assigned") && (
                      <button
                        type="button"
                        className="button secondary"
                        onClick={() => setIsEditModalOpen(true)}
                      >
                        Chỉnh sửa
                      </button>
                    )}
                  </div>
                  <div className="badge-row">
                    <Badge
                      tone={
                        selectedTicket.status === "Resolved" || selectedTicket.status === "Closed"
                          ? "good"
                          : selectedTicket.status === "Cancelled"
                            ? "bad"
                            : "warn"
                      }
                    >
                      {selectedTicket.status}
                    </Badge>
                    <Badge
                      tone={
                        selectedTicket.priority === "Critical"
                          ? "bad"
                          : selectedTicket.priority === "High"
                            ? "warn"
                            : selectedTicket.priority === "Medium"
                              ? "primary"
                              : "default"
                      }
                    >
                      {selectedTicket.priority}
                    </Badge>
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

                  <TicketActionPanel ticket={selectedTicket} />

                  <div className="mini-grid">
                    <div className="mini-card">
                      <strong>Timeline</strong>
                      <div className="stack">
                        {selectedTicket.history.map((item: TicketHistoryItem) => (
                          <div key={item.id} className="timeline-item compact">
                            <strong>{item.status}</strong>
                            <span>
                              {item.note} · {item.changedBy} · {formatDateTime(item.changedAt)}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  </div>
                </>
              ) : (
                <EmptyState
                  title="No ticket selected"
                  description="Chọn một ticket từ danh sách để xem chi tiết."
                />
              )}
            </Panel>
          </div>
        </main>
      </div>

      <TicketBoard
        tickets={filteredTickets}
        selectedTicketId={activeTicketId}
        onSelectTicket={setSelectedTicketId}
      />

      <CreateTicketModal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} />
      {isEditModalOpen && selectedTicket && (
        <EditTicketModal
          key={selectedTicket.id}
          ticket={selectedTicket}
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
        />
      )}

      <ChangePasswordModal
        isOpen={isChangePasswordOpen}
        onClose={() => setIsChangePasswordOpen(false)}
      />
    </div>
  );
}
