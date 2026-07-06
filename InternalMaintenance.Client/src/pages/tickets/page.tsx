import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { wireframeData } from "../../shared/mock/wireframe-data";
import { Badge, EmptyState, Panel } from "../../shared/ui";
import { TicketBoard } from "../../features/tickets/components/ticket-board";
import { appRoutes } from "../../shared/config/routes";
import type { TicketPriority, TicketStatus } from "../../entities/ticket/model/types";

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
};

export function TicketsPage() {
  const [search, setSearch] = useState("");
  const [ticketStatus, setTicketStatus] = useState<"All" | TicketStatus>("All");
  const [ticketPriority, setTicketPriority] = useState<"All" | TicketPriority>("All");
  const [selectedTicketId, setSelectedTicketId] = useState<number>(
    wireframeData.tickets[0]?.id ?? 0,
  );
  const [assignTechnicianId, setAssignTechnicianId] = useState<string>("");
  const [assignmentNote, setAssignmentNote] = useState("");
  const [resolutionNote, setResolutionNote] = useState("");
  const [commentDraft, setCommentDraft] = useState("");

  const selectedTicket =
    wireframeData.tickets.find((ticket) => ticket.id === selectedTicketId) ?? null;

  const filteredTickets = useMemo(
    () =>
      wireframeData.tickets.filter((ticket) => {
        const matchesSearch =
          !search ||
          `${ticket.ticketCode} ${ticket.title} ${ticket.description} ${ticket.equipmentName}`
            .toLowerCase()
            .includes(search.toLowerCase());
        const matchesStatus = ticketStatus === "All" || ticket.status === ticketStatus;
        const matchesPriority = ticketPriority === "All" || ticket.priority === ticketPriority;
        return matchesSearch && matchesStatus && matchesPriority;
      }),
    [search, ticketPriority, ticketStatus],
  );

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
        </nav>
      </header>

      <div className="layout">
        <main className="main-panel">
          <div className="view-grid tickets-grid">
            <Panel>
              <span className="eyebrow">Tickets</span>
              <h2>Ticket queue</h2>
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
                {filteredTickets.map((ticket) => (
                  <button
                    key={ticket.id}
                    type="button"
                    className={`list-item ${ticket.id === selectedTicket?.id ? "selected" : ""}`}
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
                ))}
                {!filteredTickets.length ? (
                  <EmptyState
                    title="Không có ticket phù hợp"
                    description="Thử đổi từ khóa hoặc reset bộ lọc hiện tại."
                  />
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

                  <div className="control-card">
                    <label className="field">
                      <span>Assign technician</span>
                      <select
                        className="select"
                        value={assignTechnicianId}
                        onChange={(event) => setAssignTechnicianId(event.target.value)}
                      >
                        <option value="">Select technician</option>
                        {wireframeData.users
                          .filter((user) => user.roleName === "Technician" && user.isActive)
                          .map((tech) => (
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
                        onChange={(event) => setAssignmentNote(event.target.value)}
                      />
                    </label>
                    <div className="button-row">
                      <button type="button" className="button primary">
                        Assign
                      </button>
                      <button type="button" className="button secondary">
                        Mark In Progress
                      </button>
                      <button type="button" className="button secondary">
                        Resolve
                      </button>
                      <button type="button" className="button secondary">
                        Close
                      </button>
                      <button type="button" className="button danger">
                        Cancel
                      </button>
                    </div>
                    <label className="field">
                      <span>Resolution note</span>
                      <textarea
                        className="textarea"
                        value={resolutionNote}
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
                        onChange={(event) => setCommentDraft(event.target.value)}
                      />
                    </label>
                    <button type="button" className="button primary">
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
                          <EmptyState
                            title="Chưa có comment"
                            description="Ticket này chưa có bình luận nào."
                          />
                        )}
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

        <aside className="inspector">
          <Panel>
            <span className="eyebrow">Prepared API</span>
            <h3>Backend layer ready</h3>
            <p className="section-lead">
              Hook `shared/api/*` đã sẵn sàng cho endpoint `auth`, `tickets`, `comments`, `history`.
            </p>
          </Panel>
        </aside>
      </div>

      <TicketBoard
        tickets={filteredTickets}
        selectedTicketId={selectedTicketId}
        onSelectTicket={setSelectedTicketId}
      />
    </div>
  );
}
