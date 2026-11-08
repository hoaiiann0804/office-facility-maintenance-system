/**
 * shared/api/attachments.ts
 *
 * Pure HTTP functions cho Ticket Attachments.
 *
 * WORKFLOW (3 bước — Presigned URL pattern):
 *
 *  [Frontend]                    [Backend API]            [Cloud Storage]
 *     │                              │                          │
 *     │──POST /presign──────────────>│                          │
 *     │  { fileName, contentType,    │ validate + sinh URL      │
 *     │    fileSize }                │──────────────────────────│
 *     │<─{ uploadUrl, storageKey,    │                          │
 *     │    storedFileName }──────────│                          │
 *     │                              │                          │
 *     │──PUT uploadUrl (raw file)───────────────────────────────>
 *     │  (KHÔNG qua Backend, không Bearer token!)               │
 *     │<─ 200 OK ───────────────────────────────────────────────│
 *     │                              │                          │
 *     │──POST /confirm──────────────>│                          │
 *     │  { storageKey, ... }         │ lưu vào DB               │
 *     │<─ 201 Created ───────────────│                          │
 */

import axios from "axios";
import type {
  ConfirmAttachmentRequest,
  PresignAttachmentRequest,
  PresignAttachmentResponse,
  TicketAttachment,
} from "../../entities/ticket/model/types";
import { http } from "./http";

// ─── Bước 1: Xin Presigned URL ───────────────────────────────────────────────
// Gửi metadata file lên Backend để Backend validate (size, contentType, ticket status)
// và sinh ra một URL tạm thời có chữ ký (Presigned URL) để upload thẳng lên Storage.
export async function presignAttachment(
  ticketId: number,
  payload: PresignAttachmentRequest,
): Promise<PresignAttachmentResponse> {
  const { data } = await http.post<PresignAttachmentResponse>(
    `/tickets/${ticketId}/attachments/presign`,
    payload,
  );
  return data;
}

// ─── Bước 2: Upload file trực tiếp lên Cloud Storage ─────────────────────────
// Dùng axios thuần (KHÔNG phải `http` client của project) vì:
// - `http` tự gắn "Authorization: Bearer <token>" vào mọi request.
// - Cloud Storage (R2/S3/Azure Blob) dùng cơ chế xác thực riêng qua chữ ký trong URL.
// - Nếu gắn thêm Bearer token, Storage sẽ từ chối với lỗi 403 (header lạ).
//
// LƯU Ý QUAN TRỌNG — TẠI SAO KHÔNG GẮN Content-Type:
// Backend ký Presigned URL với SignedHeaders = "host" (không có content-type).
// Nếu Frontend gắn "Content-Type: application/pdf" vào request, trình duyệt sẽ
// trigger CORS Preflight (OPTIONS request) trước khi PUT. Preflight không có
// Content-Type trong signed headers → R2 trả về 403 SignatureDoesNotMatch → "network error".
// Giải pháp: KHÔNG gắn Content-Type header — R2 đọc loại file từ URL signature.
export async function uploadToStorage(
  uploadUrl: string,
  file: File,
  onProgress?: (percent: number) => void,
): Promise<void> {
  await axios.put(uploadUrl, file, {
    // Không gắn Content-Type header ở đây!
    // Lý do: Content-Type trigger CORS preflight → R2 từ chối vì chữ ký không khớp.
    // File object được axios gửi dưới dạng binary blob — R2 vẫn lưu đúng định dạng.
    headers: {},
    onUploadProgress: (event) => {
      if (onProgress && event.total) {
        const percent = Math.round((event.loaded * 100) / event.total);
        onProgress(percent);
      }
    },
  });
}

// ─── Bước 3: Xác nhận với Backend sau khi upload Storage xong ────────────────
// Backend sẽ lưu metadata vào DB và trả về record TicketAttachment đã được persist.
// Nếu bỏ qua bước này, file sẽ "lơ lửng" trên Storage mà DB không biết.
export async function confirmAttachment(
  ticketId: number,
  payload: ConfirmAttachmentRequest,
): Promise<TicketAttachment> {
  const { data } = await http.post<TicketAttachment>(
    `/tickets/${ticketId}/attachments/confirm`,
    payload,
  );
  return data;
}

// ─── GET: Lấy danh sách attachments của một ticket ───────────────────────────
export async function getAttachmentsByTicketId(ticketId: number): Promise<TicketAttachment[]> {
  const { data } = await http.get<TicketAttachment[]>(`/tickets/${ticketId}/attachments`);
  return data;
}

// ─── GET: Lấy Presigned Download URL để xem/tải file ────────────────────────
// Không trả về URL vĩnh viễn mà trả về URL tạm (thường hết hạn sau 15-60 phút)
// để bảo vệ file khỏi bị truy cập trái phép.
export async function getAttachmentDownloadUrl(
  ticketId: number,
  attachmentId: number,
): Promise<string> {
  const { data } = await http.get<string>(
    `/tickets/${ticketId}/attachments/${attachmentId}/download-url`,
  );
  return data;
}

// ─── DELETE: Xóa attachment ───────────────────────────────────────────────────
export async function deleteAttachment(ticketId: number, attachmentId: number): Promise<void> {
  await http.delete(`/tickets/${ticketId}/attachments/${attachmentId}`);
}
