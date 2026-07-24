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
  userId: number;
  userName: string;
  content: string;
  createdAt: string;
}

export interface TicketHistoryItem {
  id: number;
  status: TicketStatus;
  note: string;
  changedBy: string;
  changedAt: string;
}

export interface TicketStatusHistoryResponse {
  id: number;
  maintenanceTicketId: number;
  oldStatus: TicketStatus;
  newStatus: TicketStatus;
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

// ─── Ticket Attachments ───────────────────────────────────────────────────────

// Nhãn nghiệp vụ mà Backend dùng để phân loại file (xem TicketAttachmentRules.ResolveFileType)
export type AttachmentFileType = "Image" | "Video" | "Document";

// Mirror C# TicketAttachmentResponse — metadata của file đã được confirm vào DB
export interface TicketAttachment {
  id: number;
  maintenanceTicketId: number;
  uploadedByUserId: number;
  uploadedByUserName: string;
  originalFileName: string;
  storedFileName: string;
  contentType: string;
  fileSize: number;
  storageKey: string;
  fileType: AttachmentFileType;
  createdAt: string;
  isDeleted: boolean;
}

// Mirror C# PresignAttachmentRequest — Frontend gửi lên để xin Presigned URL
export interface PresignAttachmentRequest {
  fileName: string;
  contentType: string;
  fileSize: number;
}

// Mirror C# PresignAttachmentResponse — Backend trả về URL tạm thời để upload lên Storage
export interface PresignAttachmentResponse {
  uploadUrl: string;
  storageKey: string;
  storedFileName: string;
}

// Mirror C# ConfirmAttachmentRequest — Frontend gửi lên sau khi upload Storage xong
export interface ConfirmAttachmentRequest {
  storageKey: string;
  originalFileName: string;
  storedFileName: string;
  contentType: string;
  fileSize: number;
  fileType: AttachmentFileType;
  fileHash?: string;
}

// Trạng thái upload của từng file trong UI (không liên quan đến Backend, chỉ là UI state)
export type UploadStatus = "pending" | "uploading" | "confirming" | "done" | "error";

export interface FileUploadItem {
  // id tạm thời chỉ dùng trong UI để track từng file
  uid: string;
  file: File;
  status: UploadStatus;
  progress: number;
  error?: string;
  // Kết quả sau khi confirm thành công — undefined khi chưa xong
  result?: TicketAttachment;
}
