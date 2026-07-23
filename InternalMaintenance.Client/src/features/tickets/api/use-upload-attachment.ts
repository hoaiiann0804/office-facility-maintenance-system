/**
 * use-upload-attachment.ts
 *
 * Hook quản lý toàn bộ luồng upload 3 bước (Presign → Storage → Confirm).
 *
 * THIẾT KẾ:
 * - Không dùng useMutation của React Query vì upload cần track PROGRESS (%)
 *   cho từng file riêng lẻ — React Query Mutation không hỗ trợ tốt cho việc này.
 * - Thay vào đó, dùng local state (uploadItems) để quản lý từng file,
 *   và gọi trực tiếp các API functions trong bước upload.
 * - Sau khi confirm xong, gọi queryClient.invalidateQueries() để
 *   danh sách attachments tự động refetch mà không cần reload trang.
 */

import { useState, useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { presignAttachment, uploadToStorage, confirmAttachment } from "../../../shared/api/attachments";
import { attachmentsQueryKey } from "./use-ticket-attachments-query";
import type {
  FileUploadItem,
  AttachmentFileType,
} from "../../../entities/ticket/model/types";

// Ánh xạ contentType sang FileType (mirror logic C# TicketAttachmentRules.ResolveFileType)
function resolveFileType(contentType: string): AttachmentFileType {
  if (contentType.startsWith("image/")) return "Image";
  if (contentType.startsWith("video/")) return "Video";
  return "Document";
}

export function useUploadAttachment(ticketId: number) {
  const queryClient = useQueryClient();

  // Danh sách file đang trong quá trình upload (chỉ tồn tại trong UI, không lưu DB)
  const [uploadItems, setUploadItems] = useState<FileUploadItem[]>([]);

  // Helper: cập nhật state của một file theo uid (tránh mutation trực tiếp array)
  const patchItem = useCallback(
    (uid: string, patch: Partial<FileUploadItem>) =>
      setUploadItems((prev) =>
        prev.map((item) => (item.uid === uid ? { ...item, ...patch } : item)),
      ),
    [],
  );

  // Hàm upload chính — nhận một File object và chạy qua 3 bước
  const upload = useCallback(
    async (file: File) => {
      // Bước 0: Thêm file vào danh sách với trạng thái "pending"
      const uid = crypto.randomUUID(); // ID tạm thời duy nhất trong UI
      const newItem: FileUploadItem = {
        uid,
        file,
        status: "pending",
        progress: 0,
      };
      setUploadItems((prev) => [...prev, newItem]);

      try {
        // ─────────────────────────────────────────────────────────
        // BƯỚC 1: Presign — xin phép upload từ Backend
        // ─────────────────────────────────────────────────────────
        // Backend sẽ: validate contentType, fileSize, ticket status
        // rồi trả về uploadUrl (link có chữ ký để PUT file lên Storage).
        patchItem(uid, { status: "uploading", progress: 0 });

        const presignResponse = await presignAttachment(ticketId, {
          fileName: file.name,
          contentType: file.type,
          fileSize: file.size,
        });

        // ─────────────────────────────────────────────────────────
        // BƯỚC 2: Upload file thẳng lên Cloud Storage
        // ─────────────────────────────────────────────────────────
        // Lý do bypass API server:
        // - File có thể nặng hàng chục MB, nếu qua server sẽ tốn băng thông đôi.
        // - Storage (S3/Azure) nhận PUT với Bearer trong Presigned URL nên an toàn.
        await uploadToStorage(
          presignResponse.uploadUrl,
          file,
          (percent) => patchItem(uid, { progress: percent }), // cập nhật % cho UI
        );

        // ─────────────────────────────────────────────────────────
        // BƯỚC 3: Confirm — thông báo Backend lưu record vào DB
        // ─────────────────────────────────────────────────────────
        // Nếu bỏ qua bước này:
        // - File đã nằm trên Storage nhưng hệ thống không "biết" nó tồn tại.
        // - Không ai có thể xem/tải file đó qua UI.
        patchItem(uid, { status: "confirming", progress: 100 });

        const attachment = await confirmAttachment(ticketId, {
          storageKey: presignResponse.storageKey,
          originalFileName: file.name,
          storedFileName: presignResponse.storedFileName,
          contentType: file.type,
          fileSize: file.size,
          fileType: resolveFileType(file.type),
        });

        // Upload thành công — lưu kết quả vào state
        patchItem(uid, { status: "done", result: attachment });

        // Invalidate cache để danh sách attachments tự fetch lại
        // (React Query sẽ gọi lại getAttachmentsByTicketId ngay lập tức)
        await queryClient.invalidateQueries({
          queryKey: attachmentsQueryKey(ticketId),
        });
      } catch (err) {
        // Một trong 3 bước thất bại — hiển thị lỗi trên file item đó
        const message =
          err instanceof Error ? err.message : "Upload thất bại, vui lòng thử lại.";
        patchItem(uid, { status: "error", error: message });
      }
    },
    [ticketId, patchItem, queryClient],
  );

  // Upload nhiều file cùng lúc (song song, không tuần tự)
  const uploadFiles = useCallback(
    (files: File[]) => {
      // Dùng Promise.allSettled thay vì Promise.all để:
      // một file lỗi không làm dừng các file khác đang upload
      void Promise.allSettled(files.map(upload));
    },
    [upload],
  );

  // Xóa file đã done/error khỏi danh sách hiển thị tạm thời
  const removeItem = useCallback(
    (uid: string) => setUploadItems((prev) => prev.filter((i) => i.uid !== uid)),
    [],
  );

  return {
    uploadItems,    // danh sách FileUploadItem để UI render progress
    uploadFiles,    // gọi hàm này khi user chọn file
    removeItem,     // xóa item khỏi queue hiển thị
  };
}
