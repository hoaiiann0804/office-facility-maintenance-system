import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import type { MaintenanceTicketDetail, TicketStatus } from "../../../entities/ticket/model/types";
import type { RoleName } from "../../../entities/auth/model/types";
import { useAuthStore } from "../../auth/model/auth-store";
import { useAssignTicketMutation } from "../api/use-assign-ticket-mutation";
import { useChangeTicketStatusMutation } from "../api/use-change-ticket-status-mutation";
import { useCreateTicketCommentMutation } from "../api/use-create-ticket-comment-mutation";
import { useUsersQuery } from "../api/use-users-query";
import { useTicketAttachmentsQuery } from "../api/use-ticket-attachments-query";
import { useUploadAttachment } from "../api/use-upload-attachment";
import { AttachmentUploadZone } from "./attachment-upload-zone";
import { AttachmentList } from "./attachment-list";
import { Spinner } from "../../../shared/ui";

function toastApiError(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const msg = error.response?.data?.message ?? error.response?.data;
    toast.error(typeof msg === "string" ? msg : fallback);
  } else {
    toast.error(fallback);
  }
}

type Props = {
  ticket: MaintenanceTicketDetail;
};

// Không còn TECH_MOCK — dữ liệu thật lấy từ API

export function TicketActionPanel({ ticket }: Props) {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const userId = session?.user.id;

  const [assignTechId, setAssignTechId] = useState<string>(
    ticket.assignedTechnicianId?.toString() ?? "",
  );
  const [assignNote, setAssignNote] = useState("");
  const [resolutionNote, setResolutionNote] = useState(ticket.resolutionNote ?? "");
  const [statusNote, setStatusNote] = useState("");
  const [commentDraft, setCommentDraft] = useState("");

  const assignMutation = useAssignTicketMutation(ticket.id);
  const statusMutation = useChangeTicketStatusMutation(ticket.id);
  const commentMutation = useCreateTicketCommentMutation(ticket.id);

  // Lấy danh sách technician thật từ API (Admin-only; non-Admin nhận danh sách rỗng)
  const { data: techPage, isLoading: isTechLoading } = useUsersQuery(
    role === "Admin" || role === "Manager"
      ? { role: "Technician", isActive: true, pageSize: 200 }
      : {},
  );
  const technicians = techPage?.items ?? [];

  const isFinalized = ticket.status === "Closed" || ticket.status === "Cancelled";
  const isAssignedTech = ticket.assignedTechnicianId === userId;

  // --- HANDLERS ---
  const handleAssign = async () => {
    if (!assignTechId) {
      toast.error("Vui lòng chọn kỹ thuật viên.");
      return;
    }
    try {
      await assignMutation.mutateAsync({
        assignedTechnicianId: Number(assignTechId),
        note: assignNote.trim() || undefined,
      });
      toast.success("Giao việc thành công!");
      setAssignNote("");
    } catch (e) {
      toastApiError(e, "Không thể giao việc.");
    }
  };

  const handleStatus = async (newStatus: string, requireResolution = false) => {
    if (requireResolution && !resolutionNote.trim()) {
      toast.error("Vui lòng điền ghi chú xử lý trước khi hoàn thành.");
      return;
    }
    try {
      await statusMutation.mutateAsync({
        status: newStatus as "InProgress" | "Resolved" | "Closed" | "Cancelled",
        resolutionNote: requireResolution ? resolutionNote.trim() : undefined,
        note: statusNote.trim() || undefined,
      });
      toast.success(`Cập nhật trạng thái thành công: ${newStatus}`);
      setStatusNote("");
    } catch (e) {
      toastApiError(e, "Không thể cập nhật trạng thái.");
    }
  };

  const handleComment = async () => {
    const content = commentDraft.trim();
    if (!content) {
      toast.error("Nội dung comment không được để trống.");
      return;
    }
    try {
      await commentMutation.mutateAsync({ content });
      setCommentDraft("");
      toast.success("Comment đã được thêm.");
    } catch (e) {
      toastApiError(e, "Không thể thêm comment.");
    }
  };

  const isWorking =
    assignMutation.isPending || statusMutation.isPending || commentMutation.isPending;

  return (
    <div className="ticket-action-panel">
      {/* ── ASSIGN (Admin / Manager) ─────────────────────────── */}
      {(role === "Admin" || role === "Manager") &&
        (ticket.status === "Pending" || ticket.status === "Assigned") && (
          <div className="control-card">
            <strong className="control-label">Phân công kỹ thuật viên</strong>
            <label className="field">
              <span>Kỹ thuật viên</span>
              <select
                className="select"
                value={assignTechId}
                onChange={(e) => setAssignTechId(e.target.value)}
                disabled={isWorking}
              >
                <option value="">
                  {isTechLoading ? "Đang tải..." : "-- Chọn kỹ thuật viên --"}
                </option>
                {technicians.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.fullName}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <span>Ghi chú phân công</span>
              <textarea
                className="textarea"
                rows={2}
                value={assignNote}
                onChange={(e) => setAssignNote(e.target.value)}
                placeholder="Ví dụ: Xử lý trước 5 giờ chiều..."
                disabled={isWorking}
              />
            </label>
            <button
              type="button"
              className="button primary"
              onClick={handleAssign}
              disabled={isWorking}
            >
              {assignMutation.isPending ? <Spinner /> : "Giao việc"}
            </button>
          </div>
        )}

      {/* ── STATUS TRANSITIONS ───────────────────────────────── */}
      {!isFinalized && (
        <div className="control-card">
          <strong className="control-label">Cập nhật trạng thái</strong>

          {/* Ghi chú trạng thái (cho mọi transition) */}
          <label className="field">
            <span>Ghi chú</span>
            <textarea
              className="textarea"
              rows={2}
              value={statusNote}
              onChange={(e) => setStatusNote(e.target.value)}
              placeholder="Ghi chú cho lần thay đổi này..."
              disabled={isWorking}
            />
          </label>

          {/* Resolution note — hiển thị khi sắp Resolve */}
          {isAssignedTech && ticket.status === "InProgress" && (
            <label className="field">
              <span>
                Kết quả xử lý <span style={{ color: "var(--danger, #fb7185)" }}>*</span>
              </span>
              <textarea
                className="textarea"
                rows={3}
                value={resolutionNote}
                onChange={(e) => setResolutionNote(e.target.value)}
                placeholder="Mô tả kết quả sửa chữa..."
                disabled={isWorking}
              />
            </label>
          )}

          <div className="button-row">
            {/* Technician được assign → InProgress */}
            {isAssignedTech && ticket.status === "Assigned" && (
              <button
                type="button"
                className="button secondary"
                onClick={() => handleStatus("InProgress")}
                disabled={isWorking}
              >
                {statusMutation.isPending ? "..." : "Bắt đầu xử lý"}
              </button>
            )}

            {/* Technician được assign → Resolved */}
            {isAssignedTech && ticket.status === "InProgress" && (
              <button
                type="button"
                className="button primary"
                onClick={() => handleStatus("Resolved", true)}
                disabled={isWorking}
              >
                {statusMutation.isPending ? "..." : "Hoàn thành"}
              </button>
            )}

            {/* Admin / Manager → Closed */}
            {(role === "Admin" || role === "Manager") && ticket.status === "Resolved" && (
              <button
                type="button"
                className="button primary"
                onClick={() => handleStatus("Closed")}
                disabled={isWorking}
              >
                {statusMutation.isPending ? "..." : "Đóng ticket"}
              </button>
            )}

            {/* Admin / Manager → Cancelled */}
            {(role === "Admin" || role === "Manager") && (
              <button
                type="button"
                className="button danger"
                onClick={() => handleStatus("Cancelled")}
                disabled={isWorking}
              >
                {statusMutation.isPending ? "..." : "Hủy ticket"}
              </button>
            )}
          </div>
        </div>
      )}

      {/* ── COMMENT ──────────────────────────────────────────── */}
      {/* ── ATTACHMENTS ───────────────────────────────────────── */}
      <AttachmentsSection
        ticketId={ticket.id}
        ticketStatus={ticket.status}
        isFinalized={isFinalized}
        currentUserId={userId ?? 0}
        role={role}
        isAssignedTech={isAssignedTech}
      />

      {!isFinalized && (
        <div className="control-card">
          <strong className="control-label">Thêm comment</strong>
          <label className="field">
            <span>Nội dung</span>
            <textarea
              className="textarea"
              rows={3}
              value={commentDraft}
              onChange={(e) => setCommentDraft(e.target.value)}
              placeholder="Nhập bình luận..."
              disabled={isWorking}
            />
          </label>
          <button
            type="button"
            className="button primary"
            onClick={handleComment}
            disabled={isWorking || !commentDraft.trim()}
          >
            {commentMutation.isPending ? "Đang gửi..." : "Gửi comment"}
          </button>
        </div>
      )}
    </div>
  );
}

// ─── AttachmentsSection ──────────────────────────────────────────────────
//
// NGHIỆP VỤ UPLOAD FILE:
// - Admin / Manager : được upload ở mọi trạng thái chưa Closed/Cancelled
// - Staff           : được upload khi ticket của họ ở trạng thái Pending/Assigned
//                    (khi ticket đưa vào InProgress/Resolved, staff tạm không upload thêm)
// - Technician      : được upload khi ticket được assign cho họ và đang InProgress/Resolved
//                    (để attach bằng chứng xử lý, kết quả sửa chữa)
//
// XEM FILE: Tất cả roles có quyền truy cập ticket đều thấy danh sách file.
// Nếu API trả 403 (ticket ngoài quyền hạn) → ẩn upload zone, hiện danh sách rỗng.
type AttachmentsSectionProps = {
  ticketId: number;
  ticketStatus: TicketStatus;
  isFinalized: boolean;
  currentUserId: number;
  role: RoleName | undefined;
  isAssignedTech: boolean;
};

// Kiểm tra role này có được phép upload theo nghiệp vụ không
function canUploadByRole(
  role: RoleName | undefined,
  ticketStatus: TicketStatus,
  isFinalized: boolean,
  isAssignedTech: boolean,
): boolean {
  if (isFinalized) return false;

  if (role === "Admin" || role === "Manager") return true;

  if (role === "Staff") {
    // Staff upload khi ticket còn ở giai đoạn đầu
    return ticketStatus === "Pending" || ticketStatus === "Assigned";
  }

  if (role === "Technician") {
    // Technician upload khi được assign và đang làm việc
    return isAssignedTech && (ticketStatus === "InProgress" || ticketStatus === "Resolved");
  }

  return false;
}

function AttachmentsSection({
  ticketId,
  ticketStatus,
  isFinalized,
  currentUserId,
  role,
  isAssignedTech,
}: AttachmentsSectionProps) {
  const { data: attachments = [], isLoading, error } = useTicketAttachmentsQuery(ticketId);
  const { uploadItems, uploadFiles, removeItem } = useUploadAttachment(ticketId);

  const canUpload = canUploadByRole(role, ticketStatus, isFinalized, isAssignedTech);

  // 403 = token hợp lệ nhưng không có quyền xem attachment của ticket này
  // (ví dụ: Technician xem ticket chưa assign cho họ, hoặc Staff xem ticket người khác)
  const isForbidden = axios.isAxiosError(error) && error.response?.status === 403;

  return (
    <div className="control-card">
      <strong className="control-label">File đính kèm</strong>
      <div className="attachment-section">
        {/* Upload zone — chỉ hiện nếu role có quyền upload theo nghiệp vụ */}
        {canUpload && !isForbidden && (
          <AttachmentUploadZone
            uploadItems={uploadItems}
            onFilesSelected={uploadFiles}
            onRemoveItem={removeItem}
            disabled={false}
          />
        )}

        {/* Danh sách file đã lưu trong DB */}
        {isLoading ? (
          <p style={{ fontSize: "0.82rem", color: "var(--muted)", margin: 0 }}>Đang tải...</p>
        ) : isForbidden ? (
          <p style={{ fontSize: "0.82rem", color: "var(--muted)", margin: 0 }}>
            Không có quyền xem file đính kèm của ticket này.
          </p>
        ) : (
          <AttachmentList
            ticketId={ticketId}
            attachments={attachments}
            canDelete={!isFinalized}
            currentUserId={currentUserId}
          />
        )}
      </div>
    </div>
  );
}
