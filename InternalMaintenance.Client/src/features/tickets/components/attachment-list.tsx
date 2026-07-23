/**
 * AttachmentList.tsx
 *
 * Hiển thị danh sách file đính kèm đã được lưu vào DB.
 *
 * THIẾT KẾ:
 * - Chỉ render data từ API (TicketAttachment[]) — không biết gì về quá trình upload.
 * - Click vào file → gọi API lấy Download URL (Presigned URL) rồi mở file.
 *   Lý do KHÔNG lưu URL cố định: Presigned URL có thời hạn (15-60 phút),
 *   mỗi lần xem phải lấy URL mới để đảm bảo bảo mật.
 * - Chỉ hiện nút xóa khi ticket chưa Closed/Cancelled VÀ người dùng là người upload.
 */

import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import type { TicketAttachment } from "../../../entities/ticket/model/types";
import { getAttachmentDownloadUrl } from "../../../shared/api/attachments";
import { useDeleteAttachmentMutation } from "../api/use-delete-attachment-mutation";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(dateStr));
}

function getFileIcon(fileType: TicketAttachment["fileType"]): string {
  switch (fileType) {
    case "Image":
      return "🖼️";
    case "Video":
      return "🎬";
    case "Document":
      return "📄";
    default:
      return "📄";
  }
}

type Props = {
  ticketId: number;
  attachments: TicketAttachment[];
  canDelete: boolean; // true nếu ticket chưa Closed/Cancelled
  currentUserId: number;
};

export function AttachmentList({ ticketId, attachments, canDelete, currentUserId }: Props) {
  // Track trạng thái "đang mở" của từng file (đang gọi API lấy download URL)
  const [openingId, setOpeningId] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const deleteMutation = useDeleteAttachmentMutation(ticketId);

  // Lấy Presigned Download URL và mở file trong tab mới
  // Không cache URL này vì nó có thời hạn ngắn
  const handleOpen = async (attachment: TicketAttachment) => {
    if (openingId === attachment.id) return;
    setOpeningId(attachment.id);
    try {
      const url = await getAttachmentDownloadUrl(ticketId, attachment.id);
      window.open(url, "_blank", "noopener,noreferrer");
    } catch (err) {
      const msg = axios.isAxiosError(err)
        ? ((err.response?.data?.message as string | undefined) ?? "Không thể mở file.")
        : "Không thể mở file.";
      toast.error(msg);
    } finally {
      setOpeningId(null);
    }
  };

  const handleDelete = async (attachment: TicketAttachment) => {
    if (!window.confirm(`Xóa file "${attachment.originalFileName}"?`)) return;
    setDeletingId(attachment.id);
    try {
      await deleteMutation.mutateAsync(attachment.id);
      toast.success("Đã xóa file đính kèm.");
    } catch (err) {
      const msg = axios.isAxiosError(err)
        ? ((err.response?.data?.message as string | undefined) ?? "Không thể xóa file.")
        : "Không thể xóa file.";
      toast.error(msg);
    } finally {
      setDeletingId(null);
    }
  };

  if (attachments.length === 0) {
    return <p className="attachment-empty">Chưa có file đính kèm nào.</p>;
  }

  return (
    <ul className="attachment-list">
      {attachments.map((att) => {
        const isOpening = openingId === att.id;
        const isDeleting = deletingId === att.id;
        // Chỉ người upload mới được xóa file của mình
        const canDeleteThis = canDelete && att.uploadedByUserId === currentUserId;

        return (
          <li key={att.id} className="attachment-item">
            {/* Icon + tên file (clickable để mở) */}
            <button
              className="attachment-open-btn"
              onClick={() => void handleOpen(att)}
              disabled={isOpening}
              title={`Mở "${att.originalFileName}"`}
            >
              <span className="attachment-file-icon">{getFileIcon(att.fileType)}</span>
              <span className="attachment-file-name">{att.originalFileName}</span>
              {isOpening && <span className="attachment-opening-spinner">⏳</span>}
            </button>

            {/* Meta: kích thước + người upload + ngày */}
            <div className="attachment-meta">
              <span>{formatBytes(att.fileSize)}</span>
              <span>·</span>
              <span>{att.uploadedByUserName}</span>
              <span>·</span>
              <span>{formatDate(att.createdAt)}</span>
            </div>

            {/* Nút xóa — chỉ hiện nếu có quyền */}
            {canDeleteThis && (
              <button
                className="attachment-delete-btn"
                onClick={() => void handleDelete(att)}
                disabled={isDeleting}
                title="Xóa file này"
                aria-label={`Xóa "${att.originalFileName}"`}
              >
                {isDeleting ? "..." : "🗑️"}
              </button>
            )}
          </li>
        );
      })}
    </ul>
  );
}
