import type { RoleName } from "../../auth/model/types";

export type TicketStatus =
  "Pending" | "Assigned" | "InProgress" | "Resolved" | "Closed" | "Cancelled";
export type TicketPriority = "Low" | "Medium" | "High" | "Critical";
export type EquipmentStatus = "Active" | "Inactive" | "UnderMaintenance" | "Retired";

export interface MaintenanceTicket {
  id: number;
  ticketCode: string;
  title: string;
  description: string;
  equipmentId: number;
  equipmentCode: string;
  equipmentName: string;
  createdByUserId: number;
  createdByUserName: string;
  assignedTechnicianId: number | null;
  assignedTechnicianName: string | null;
  priority: TicketPriority;
  status: TicketStatus;
  resolutionNote: string;
  createdAt: string;
  resolvedAt: string | null;
  closedAt: string | null;
}

export interface MaintenanceTicketDetail extends MaintenanceTicket {
  comments: TicketComment[];
  history: TicketHistoryItem[];
}

export interface TicketComment {
  id: number;
  userName: string;
  content: string;
  createdAt: string;
}

export interface TicketHistoryItem {
  id: number;
  maintenanceTicketId: number;
  oldStatus: string;
  newStatus: string;
  changedByUserId: number;
  changedByUserName: string;
  changedAt: string;
  note: string | null;
}

export interface TicketQuery {
  status?: TicketStatus;
  priority?: TicketPriority;
  equipmentId?: number;
  page?: number;
  pageSize?: number;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface AssignTicketRequest {
  assignedTechnicianId: number;
  note?: string;
}

export interface ChangeTicketStatusRequest {
  status: TicketStatus;
  resolutionNote?: string;
  note?: string;
}

export interface CreateTicketCommentRequest {
  content: string;
}

export interface UpdateTicketRequest {
  title: string;
  description: string;
  priority: string;
}

export interface TicketQuickSummary {
  status: TicketStatus;
  priority: TicketPriority;
  roleName?: RoleName;
}
