import { useMemo, useState } from "react";
import { wireframeData } from "../../shared/mock/wireframe-data";
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
import {
  TicketSlaBadge,
  TicketSlaDetailBar,
} from "../../features/tickets/components/ticket-sla-badge";
import { calculateSlaInfo } from "../../features/tickets/lib/sla-utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Search, Plus, Wrench, User, Calendar, Edit3, History } from "lucide-react";

import { formatDateTime } from "@/shared/lib/date-utils";

type SlaFilterType = "All" | "InSLA" | "NearBreach" | "Breached" | "MetSLA";

export function TicketsPage() {
  const [search, setSearch] = useState("");
  const [ticketStatus, setTicketStatus] = useState<"All" | TicketStatus>("All");
  const [ticketPriority, setTicketPriority] = useState<"All" | TicketPriority>("All");
  const [slaFilter, setSlaFilter] = useState<SlaFilterType>("All");
  const [selectedTicketId, setSelectedTicketId] = useState<number | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const {
    data: ticketsPage,
    isLoading,
    isError,
  } = useTicketsQuery({
    status: ticketStatus === "All" ? undefined : ticketStatus,
    priority: ticketPriority === "All" ? undefined : ticketPriority,
  });

  const tickets = useMemo(() => ticketsPage?.items ?? [], [ticketsPage?.items]);
  const filteredTickets = useMemo(() => {
    let result = tickets;

    const keyword = search.trim().toLowerCase();
    if (keyword) {
      result = result.filter((ticket) =>
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
    }

    if (slaFilter !== "All") {
      result = result.filter((ticket) => {
        const sla = calculateSlaInfo(ticket);
        if (slaFilter === "Breached") return sla.isBreached || sla.statusType === "MissedSLA";
        if (slaFilter === "NearBreach") return sla.statusType === "NearBreach";
        if (slaFilter === "InSLA") return sla.statusType === "InSLA";
        if (slaFilter === "MetSLA") return sla.statusType === "MetSLA";
        return true;
      });
    }

    return result;
  }, [tickets, search, slaFilter]);

  const activeTicketId = selectedTicketId ?? filteredTickets[0]?.id ?? null;

  const {
    data: selectedTicket,
    isLoading: isSelectedTicketLoading,
    isError: isSelectedTicketError,
  } = useTicketDetailQuery(activeTicketId);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Resolved":
        return <Badge variant="success">Đã xử lý</Badge>;
      case "Closed":
        return <Badge variant="success">Đã đóng</Badge>;
      case "InProgress":
        return <Badge variant="default">Đang xử lý</Badge>;
      case "Assigned":
        return <Badge variant="warning">Đã phân công</Badge>;
      case "Cancelled":
        return <Badge variant="outline">Đã hủy</Badge>;
      case "WaitingForVendor":
        return <Badge variant="destructive">Chờ đối tác</Badge>;
      default:
        return <Badge variant="secondary">Chờ tiếp nhận</Badge>;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case "Critical":
        return <Badge variant="destructive">Khẩn cấp</Badge>;
      case "High":
        return <Badge variant="destructive">Cao</Badge>;
      case "Medium":
        return <Badge variant="warning">Trung bình</Badge>;
      default:
        return <Badge variant="outline">Thấp</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Quản lý Phiếu yêu cầu
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Danh sách yêu cầu bảo trì
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Theo dõi tiến độ xử lý sự cố, phân công kỹ thuật viên và giám sát thời gian SLA.
          </p>
        </div>
        <Button onClick={() => setIsCreateModalOpen(true)} className="gap-1.5 shrink-0">
          <Plus className="h-4 w-4" />
          <span>Tạo Ticket Mới</span>
        </Button>
      </div>

      {/* Main Grid: Left Master List (5 cols) & Right Detail (7 cols) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Master Tickets List (5 cols) */}
        <Card className="lg:col-span-5 flex flex-col h-[calc(100vh-16rem)]">
          <CardHeader className="pb-3 border-b space-y-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base font-semibold">Tickets</CardTitle>
              <Badge variant="secondary" className="text-xs">
                {filteredTickets.length} / {tickets.length}
              </Badge>
            </div>

            {/* Filter controls */}
            <div className="space-y-2">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  type="text"
                  value={search}
                  placeholder="Tìm mã ticket, tiêu đề, thiết bị..."
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8 text-xs h-8"
                />
              </div>

              <div className="grid grid-cols-3 gap-1.5">
                <div className="space-y-1">
                  <Label className="text-[10px] text-muted-foreground">Trạng thái</Label>
                  <select
                    className="flex h-8 w-full rounded-md border border-input bg-transparent px-2 text-[11px] shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={ticketStatus}
                    onChange={(e) => setTicketStatus(e.target.value as TicketStatus | "All")}
                  >
                    {["All", ...wireframeData.workflow, "Cancelled"].map((status) => (
                      <option key={status} value={status} className="bg-background text-foreground">
                        {status}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <Label className="text-[10px] text-muted-foreground">Ưu tiên</Label>
                  <select
                    className="flex h-8 w-full rounded-md border border-input bg-transparent px-2 text-[11px] shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    value={ticketPriority}
                    onChange={(e) => setTicketPriority(e.target.value as TicketPriority | "All")}
                  >
                    {["All", ...wireframeData.priorities].map((p) => (
                      <option key={p} value={p} className="bg-background text-foreground">
                        {p}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1">
                  <Label className="text-[10px] text-muted-foreground">SLA</Label>
                  <select
                    className="flex h-8 w-full rounded-md border border-input bg-transparent px-2 text-[11px] shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring font-semibold text-primary"
                    value={slaFilter}
                    onChange={(e) => setSlaFilter(e.target.value as SlaFilterType)}
                  >
                    <option value="All" className="bg-background text-foreground">
                      Tất cả SLA
                    </option>
                    <option value="InSLA" className="bg-background text-foreground">
                      Trong SLA
                    </option>
                    <option value="NearBreach" className="bg-background text-foreground">
                      Sắp hết hạn
                    </option>
                    <option value="Breached" className="bg-background text-foreground">
                      Quá hạn (Breached)
                    </option>
                    <option value="MetSLA" className="bg-background text-foreground">
                      Đạt SLA
                    </option>
                  </select>
                </div>
              </div>
            </div>
          </CardHeader>

          <CardContent className="p-2 overflow-y-auto flex-1 divide-y">
            {isLoading ? (
              <div className="py-12 flex justify-center">
                <div className="h-6 w-6 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </div>
            ) : isError ? (
              <div className="p-6 text-center text-xs text-destructive">
                Không thể tải danh sách ticket. Vui lòng thử lại sau.
              </div>
            ) : filteredTickets.length > 0 ? (
              filteredTickets.map((ticket) => {
                const isSelected = ticket.id === activeTicketId;
                return (
                  <button
                    key={ticket.id}
                    type="button"
                    onClick={() => setSelectedTicketId(ticket.id)}
                    className={`w-full text-left p-3 rounded-lg transition-all space-y-1.5 ${
                      isSelected
                        ? "bg-primary/10 border-l-4 border-l-primary"
                        : "hover:bg-accent/60"
                    }`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-mono font-bold text-primary">
                        {ticket.ticketCode}
                      </span>
                      <div className="flex items-center gap-1.5 flex-wrap justify-end">
                        {getPriorityBadge(ticket.priority)}
                        {getStatusBadge(ticket.status)}
                      </div>
                    </div>

                    <h4 className="text-xs font-semibold text-foreground line-clamp-1">
                      {ticket.title}
                    </h4>

                    <div className="flex items-center justify-between text-[11px] text-muted-foreground pt-0.5">
                      <span className="truncate max-w-[140px] flex items-center gap-1">
                        <Wrench className="h-3 w-3 shrink-0" />
                        {ticket.equipmentName}
                      </span>
                      <TicketSlaBadge ticket={ticket} />
                    </div>
                  </button>
                );
              })
            ) : (
              <div className="p-8 text-center text-xs text-muted-foreground">
                Không tìm thấy ticket nào phù hợp.
              </div>
            )}
          </CardContent>
        </Card>

        {/* Right Column: Ticket Detail View (7 cols) */}
        <div className="lg:col-span-7 space-y-6">
          {isSelectedTicketLoading ? (
            <Card>
              <CardContent className="py-16 flex justify-center">
                <div className="h-7 w-7 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </CardContent>
            </Card>
          ) : isSelectedTicketError ? (
            <Card className="border-destructive/30">
              <CardContent className="py-12 text-center text-xs text-destructive">
                Không thể tải chi tiết ticket này.
              </CardContent>
            </Card>
          ) : selectedTicket ? (
            <>
              {/* Ticket SLA Countdown / Status Bar */}
              <TicketSlaDetailBar ticket={selectedTicket} />

              {/* Ticket Main Info Card */}
              <Card>
                <CardHeader className="pb-3 border-b">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-mono font-bold text-primary">
                          {selectedTicket.ticketCode}
                        </span>
                        {getPriorityBadge(selectedTicket.priority)}
                        {getStatusBadge(selectedTicket.status)}
                      </div>
                      <CardTitle className="text-lg font-bold text-foreground">
                        {selectedTicket.title}
                      </CardTitle>
                    </div>

                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setIsEditModalOpen(true)}
                      className="gap-1.5 text-xs shrink-0 self-start sm:self-auto"
                    >
                      <Edit3 className="h-3.5 w-3.5" />
                      <span>Chỉnh sửa</span>
                    </Button>
                  </div>
                </CardHeader>

                <CardContent className="p-5 space-y-4">
                  {/* Meta Details Grid */}
                  <div className="grid grid-cols-2 sm:grid-cols-5 gap-3 p-3 rounded-xl bg-muted/30 border text-xs">
                    <div>
                      <p className="text-[10px] uppercase font-semibold text-muted-foreground">
                        Thiết bị
                      </p>
                      <p className="font-semibold text-foreground mt-0.5 truncate">
                        {selectedTicket.equipmentCode} — {selectedTicket.equipmentName}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] uppercase font-semibold text-muted-foreground">
                        Người yêu cầu
                      </p>
                      <p className="font-medium text-foreground mt-0.5 flex items-center gap-1">
                        <User className="h-3 w-3 text-muted-foreground shrink-0" />
                        {selectedTicket.createdByUserName}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] uppercase font-semibold text-muted-foreground">
                        Kỹ thuật viên chính
                      </p>
                      <p className="font-medium text-foreground mt-0.5 flex items-center gap-1">
                        <User className="h-3 w-3 text-muted-foreground shrink-0" />
                        {selectedTicket.assignedTechnicianName || "Chưa phân công"}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] uppercase font-semibold text-muted-foreground">
                        Kỹ thuật viên hỗ trợ
                      </p>
                      <p className="font-medium text-foreground mt-0.5 flex items-center gap-1">
                        <User className="h-3 w-3 text-muted-foreground shrink-0" />
                        {selectedTicket.supportTechnicianName || "Không có"}
                      </p>
                    </div>
                    <div>
                      <p className="text-[10px] uppercase font-semibold text-muted-foreground">
                        Thời gian tạo
                      </p>
                      <p className="font-medium text-foreground mt-0.5 flex items-center gap-1">
                        <Calendar className="h-3 w-3 text-muted-foreground shrink-0" />
                        {formatDateTime(selectedTicket.createdAt)}
                      </p>
                    </div>
                  </div>

                  {/* Ticket Description */}
                  <div className="space-y-1.5">
                    <Label className="text-xs font-semibold text-muted-foreground">
                      Mô tả sự cố
                    </Label>
                    <div className="p-3.5 rounded-xl border bg-card text-xs text-foreground leading-relaxed whitespace-pre-wrap">
                      {selectedTicket.description}
                    </div>
                  </div>

                  {/* Resolution note if any */}
                  {selectedTicket.resolutionNote && (
                    <div className="space-y-1.5 p-3 rounded-xl border bg-emerald-500/5 border-emerald-500/20 text-xs">
                      <Label className="font-semibold text-emerald-700 dark:text-emerald-400">
                        Ghi chú kết quả xử lý
                      </Label>
                      <p className="text-foreground leading-relaxed">
                        {selectedTicket.resolutionNote}
                      </p>
                    </div>
                  )}

                  {/* Cancellation reason if any */}
                  {selectedTicket.cancellationReason && (
                    <div className="space-y-1.5 p-3 rounded-xl border bg-destructive/5 border-destructive/20 text-xs">
                      <Label className="font-semibold text-destructive">Lý do hủy ticket</Label>
                      <p className="text-foreground leading-relaxed">
                        {selectedTicket.cancellationReason}
                      </p>
                    </div>
                  )}

                  {/* History audit log */}
                  {selectedTicket.history && selectedTicket.history.length > 0 && (
                    <div className="space-y-2 pt-2 border-t">
                      <Label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                        <History className="h-3.5 w-3.5 text-primary" />
                        <span>Lịch sử thay đổi trạng thái</span>
                      </Label>
                      <div className="space-y-2 max-h-40 overflow-y-auto pr-1">
                        {selectedTicket.history.map((h: TicketHistoryItem) => (
                          <div
                            key={h.id}
                            className="text-[11px] p-2 rounded-lg border bg-muted/20 flex items-center justify-between gap-2"
                          >
                            <div>
                              <span className="font-semibold text-foreground">{h.changedBy}</span>
                              <span className="text-muted-foreground"> chuyển sang </span>
                              {getStatusBadge(h.status)}
                              {h.note && (
                                <p className="text-muted-foreground italic mt-0.5">"{h.note}"</p>
                              )}
                            </div>
                            <span className="text-[10px] text-muted-foreground shrink-0">
                              {formatDateTime(h.changedAt)}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Vendor Logs */}
                  {selectedTicket.vendorLogs && selectedTicket.vendorLogs.length > 0 && (
                    <div className="space-y-2 pt-2 border-t">
                      <Label className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5">
                        <History className="h-3.5 w-3.5 text-primary" />
                        <span>Lịch sử chuyển đơn vị ngoài</span>
                      </Label>
                      <div className="space-y-2 max-h-40 overflow-y-auto pr-1">
                        {selectedTicket.vendorLogs.map((vl) => (
                          <div
                            key={vl.id}
                            className="text-[11px] p-2 rounded-lg border bg-muted/20 flex flex-col gap-1"
                          >
                            <div className="flex items-center justify-between">
                              <span className="font-semibold text-foreground">
                                Chuyển cho {vl.vendorName}
                              </span>
                              <span className="text-[10px] text-muted-foreground shrink-0">
                                {formatDateTime(vl.dispatchedAt)}
                              </span>
                            </div>
                            <div className="text-muted-foreground">
                              Dự kiến trả:{" "}
                              <span className="font-medium text-foreground">
                                {new Date(vl.estimatedReturnDate).toLocaleDateString()}
                              </span>
                              {vl.actualReturnDate && (
                                <>
                                  {" "}
                                  | Thực tế:{" "}
                                  <span className="font-medium text-foreground">
                                    {new Date(vl.actualReturnDate).toLocaleDateString()}
                                  </span>
                                </>
                              )}
                            </div>
                            {vl.pausedMinutes > 0 && (
                              <div className="text-muted-foreground">
                                Đã tạm dừng SLA:{" "}
                                <span className="font-medium text-foreground">
                                  {Math.floor(vl.pausedMinutes / 60)}h {vl.pausedMinutes % 60}m
                                </span>
                              </div>
                            )}
                            {vl.note && (
                              <p className="text-muted-foreground italic mt-0.5">"{vl.note}"</p>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Ticket Action Panel (Assign, Status Change, Attachments, Comments) */}
              <TicketActionPanel ticket={selectedTicket} />
            </>
          ) : (
            <Card>
              <CardContent className="py-16 text-center text-xs text-muted-foreground">
                Chọn một ticket từ danh sách bên trái để xem chi tiết.
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Modals */}
      <CreateTicketModal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} />
      <EditTicketModal
        ticket={selectedTicket ?? null}
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
      />
    </div>
  );
}
