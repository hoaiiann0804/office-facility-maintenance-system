import type { MaintenanceTicket, TicketPriority } from "../../../entities/ticket/model/types";
import { parseApiDate, formatDateTime } from "../../../shared/lib/date-utils";

// Hạn chót tính theo giờ dựa trên Mức độ ưu tiên
export const SLA_HOURS_MAP: Record<TicketPriority, number> = {
  Critical: 2, // 2 giờ
  High: 8, // 8 giờ
  Medium: 24, // 24 giờ (1 ngày)
  Low: 48, // 48 giờ (2 ngày)
};

export type SlaStatusType =
  "MetSLA" | "MissedSLA" | "InSLA" | "NearBreach" | "Breached" | "Cancelled";

export interface SlaInfo {
  statusType: SlaStatusType;
  deadline: Date;
  deadlineFormatted: string;
  badgeLabel: string;
  badgeVariant: "success" | "warning" | "destructive" | "default" | "secondary" | "outline";
  remainingText: string;
  isBreached: boolean;
}

export function getSlaDeadline(createdAtStr: string, priority: TicketPriority): Date {
  const created = parseApiDate(createdAtStr);
  const slaHours = SLA_HOURS_MAP[priority] ?? 24;
  return new Date(created.getTime() + slaHours * 60 * 60 * 1000);
}

export function formatSlaDuration(ms: number): string {
  const absMs = Math.abs(ms);
  const totalMinutes = Math.floor(absMs / (1000 * 60));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  const days = Math.floor(hours / 24);
  const remHours = hours % 24;

  if (days > 0) {
    return `${days}d ${remHours}h`;
  }
  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }
  return `${minutes}m`;
}

export function calculateSlaInfo(ticket: MaintenanceTicket): SlaInfo {
  const deadline = ticket.dueAt
    ? parseApiDate(ticket.dueAt)
    : getSlaDeadline(ticket.createdAt, ticket.priority);
  const deadlineFormatted = formatDateTime(deadline);

  const isFinalized =
    ticket.status === "Resolved" || ticket.status === "Closed" || ticket.status === "Cancelled";

  if (ticket.status === "Cancelled") {
    return {
      statusType: "Cancelled",
      deadline,
      deadlineFormatted,
      badgeLabel: "Đã hủy",
      badgeVariant: "secondary",
      remainingText: "Ticket đã bị hủy",
      isBreached: false,
    };
  }

  // Đã giải quyết / Đã đóng -> tính điểm kết thúc
  if (isFinalized) {
    if (ticket.slaStatus === "MetSLA") {
      return {
        statusType: "MetSLA",
        deadline,
        deadlineFormatted,
        badgeLabel: "Đạt SLA",
        badgeVariant: "success",
        remainingText: "Hoàn tất đúng hạn SLA",
        isBreached: false,
      };
    }
    if (ticket.slaStatus === "MissedSLA") {
      return {
        statusType: "MissedSLA",
        deadline,
        deadlineFormatted,
        badgeLabel: "Trễ SLA",
        badgeVariant: "destructive",
        remainingText: "Đã trễ hạn SLA",
        isBreached: true,
      };
    }

    const endTimestamp = ticket.resolvedAt || ticket.closedAt || ticket.createdAt;
    const endDate = parseApiDate(endTimestamp);
    const diffMs = deadline.getTime() - endDate.getTime();

    if (diffMs >= 0) {
      return {
        statusType: "MetSLA",
        deadline,
        deadlineFormatted,
        badgeLabel: "Đạt SLA",
        badgeVariant: "success",
        remainingText: `Hoàn tất đúng hạn SLA (${formatSlaDuration(diffMs)})`,
        isBreached: false,
      };
    } else {
      return {
        statusType: "MissedSLA",
        deadline,
        deadlineFormatted,
        badgeLabel: "Trễ SLA",
        badgeVariant: "destructive",
        remainingText: `Trễ SLA ${formatSlaDuration(diffMs)}`,
        isBreached: true,
      };
    }
  }

  // Ticket đang xử lý / chờ xử lý -> tính so với thời gian hiện tại
  const now = new Date();
  const diffMs = deadline.getTime() - now.getTime();

  if (diffMs < 0) {
    return {
      statusType: "Breached",
      deadline,
      deadlineFormatted,
      badgeLabel: `Quá hạn ${formatSlaDuration(diffMs)}`,
      badgeVariant: "destructive",
      remainingText: `Quá hạn SLA ${formatSlaDuration(diffMs)}`,
      isBreached: true,
    };
  }

  const ONE_HOUR_MS = 60 * 60 * 1000;
  if (diffMs <= ONE_HOUR_MS) {
    return {
      statusType: "NearBreach",
      deadline,
      deadlineFormatted,
      badgeLabel: `Còn ${formatSlaDuration(diffMs)}`,
      badgeVariant: "warning",
      remainingText: `Sắp hết hạn SLA (${formatSlaDuration(diffMs)})`,
      isBreached: false,
    };
  }

  return {
    statusType: "InSLA",
    deadline,
    deadlineFormatted,
    badgeLabel: `Còn ${formatSlaDuration(diffMs)}`,
    badgeVariant: "default",
    remainingText: `Trong thời hạn SLA (${formatSlaDuration(diffMs)})`,
    isBreached: false,
  };
}
