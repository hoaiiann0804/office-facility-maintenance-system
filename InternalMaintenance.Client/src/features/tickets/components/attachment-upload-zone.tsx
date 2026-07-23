/**
 * AttachmentUploadZone.tsx
 *
 * Component Drag-and-Drop để upload file đính kèm.
 *
 * THIẾT KẾ:
 * - Không dùng thư viện ngoài (react-dropzone). Dùng HTML5 Drag Events
 *   + input[type=file] thuần để tránh thêm dependency.
 * - Validate file trước khi upload (contentType, fileSize) — mirror logic Backend.
 * - Hiển thị danh sách file đang upload kèm progress bar.
 * - Sau khi file "done", item này chỉ tồn tại tạm thời trong queue upload
 *   (không phải trong AttachmentList — list chỉ hiện file đã lưu vào DB).
 */

import { useRef, useState, useCallback, type DragEvent } from "react";
import { toast } from "sonner";
import type { FileUploadItem } from "../../../entities/ticket/model/types";

// Các hằng số này mirror TicketAttachmentRules.cs từ Backend
const MAX_SIZE_BYTES = 100 * 1024 * 1024; // 100MB
const ALLOWED_TYPES = [
  "image/jpeg", "image/jpg", "image/png", "image/webp",
  "video/mp4", "video/webm",
  "application/pdf",
];

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getStatusLabel(status: FileUploadItem["status"]): string {
  switch (status) {
    case "pending":    return "Đang chờ...";
    case "uploading":  return "Đang tải lên...";
    case "confirming": return "Đang xác nhận...";
    case "done":       return "Hoàn thành ✓";
    case "error":      return "Lỗi";
  }
}

function getFileIcon(contentType: string): string {
  if (contentType.startsWith("image/")) return "🖼️";
  if (contentType.startsWith("video/")) return "🎬";
  return "📄";
}

type Props = {
  uploadItems: FileUploadItem[];
  onFilesSelected: (files: File[]) => void;
  onRemoveItem: (uid: string) => void;
  disabled?: boolean;
};

export function AttachmentUploadZone({ uploadItems, onFilesSelected, onRemoveItem, disabled }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  // Validate và lọc file trước khi chuyển cho hook upload
  const processFiles = useCallback(
    (rawFiles: FileList | File[]) => {
      const files = Array.from(rawFiles);
      const valid: File[] = [];

      for (const file of files) {
        if (!ALLOWED_TYPES.includes(file.type)) {
          toast.error(`"${file.name}" — định dạng không được hỗ trợ.`);
          continue;
        }
        if (file.size > MAX_SIZE_BYTES) {
          toast.error(`"${file.name}" — vượt quá giới hạn 100MB.`);
          continue;
        }
        if (file.size === 0) {
          toast.error(`"${file.name}" — file rỗng, không hợp lệ.`);
          continue;
        }
        valid.push(file);
      }

      if (valid.length > 0) {
        onFilesSelected(valid);
      }
    },
    [onFilesSelected],
  );

  // ─── Drag Events ─────────────────────────────────────────────────────────
  // preventDefault() để ngăn browser mở file khi thả vào.
  const onDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (!disabled) setIsDragging(true);
  };
  const onDragLeave = () => setIsDragging(false);
  const onDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    if (!disabled) processFiles(e.dataTransfer.files);
  };

  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) processFiles(e.target.files);
    // Reset input để user có thể chọn lại cùng file
    e.target.value = "";
  };

  const hasActiveUploads = uploadItems.some(
    (i) => i.status === "uploading" || i.status === "confirming",
  );

  return (
    <div className="upload-zone-wrapper">
      {/* ── Vùng Drag & Drop ─────────────────────────────────── */}
      <div
        className={`upload-dropzone${isDragging ? " drag-over" : ""}${disabled ? " disabled" : ""}`}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
        onClick={() => !disabled && inputRef.current?.click()}
        role="button"
        aria-label="Vùng upload file"
        aria-disabled={disabled}
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept={ALLOWED_TYPES.join(",")}
          className="upload-input-hidden"
          onChange={onInputChange}
          disabled={disabled}
        />
        <span className="upload-icon">📎</span>
        <p className="upload-hint-primary">
          {disabled ? "Ticket đã đóng — không thể đính kèm file" : "Kéo thả file vào đây hoặc click để chọn"}
        </p>
        <p className="upload-hint-secondary">
          Hỗ trợ: JPG, PNG, WEBP, MP4, WEBM, PDF · Tối đa 100MB mỗi file
        </p>
      </div>

      {/* ── Danh sách file đang upload ────────────────────────── */}
      {uploadItems.length > 0 && (
        <ul className="upload-queue">
          {uploadItems.map((item) => (
            <li key={item.uid} className={`upload-queue-item status-${item.status}`}>
              <span className="upload-file-icon">{getFileIcon(item.file.type)}</span>

              <div className="upload-file-info">
                <span className="upload-file-name" title={item.file.name}>
                  {item.file.name}
                </span>
                <span className="upload-file-meta">
                  {formatBytes(item.file.size)} · {getStatusLabel(item.status)}
                  {item.status === "error" && item.error && (
                    <span className="upload-error-msg"> — {item.error}</span>
                  )}
                </span>

                {/* Progress bar — chỉ hiện khi đang uploading */}
                {(item.status === "uploading" || item.status === "confirming") && (
                  <div className="upload-progress-track">
                    <div
                      className="upload-progress-fill"
                      style={{ width: `${item.progress}%` }}
                    />
                  </div>
                )}
              </div>

              {/* Nút xóa khỏi queue — chỉ khi done/error */}
              {(item.status === "done" || item.status === "error") && (
                <button
                  className="upload-queue-dismiss"
                  onClick={() => onRemoveItem(item.uid)}
                  aria-label="Xóa khỏi danh sách"
                  title="Xóa khỏi danh sách"
                >
                  ×
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      {hasActiveUploads && (
        <p className="upload-active-notice">Đang upload... Vui lòng không đóng trang.</p>
      )}
    </div>
  );
}
