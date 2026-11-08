/**
 * use-ticket-attachments-query.ts
 *
 * Lấy danh sách file đính kèm của một ticket.
 *
 * Đây là Query (Read) — React Query sẽ cache kết quả và tự động
 * refetch khi queryKey thay đổi hoặc khi có mutation invalidate cache.
 */

import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { getAttachmentsByTicketId } from "../../../shared/api/attachments";
import { useAuthStore } from "../../auth/model/auth-store";
import type { TicketAttachment } from "../../../entities/ticket/model/types";

// Query key factory — đặt ticketId vào key để mỗi ticket có cache riêng.
// Khi ticketId thay đổi, React Query sẽ tự fetch lại data mới.
export const attachmentsQueryKey = (ticketId: number) => ["tickets", ticketId, "attachments"];

export function useTicketAttachmentsQuery(ticketId: number | null) {
  const session = useAuthStore((s) => s.session);
  const signOut = useAuthStore((s) => s.signOut);

  const query = useQuery<TicketAttachment[]>({
    queryKey: attachmentsQueryKey(ticketId ?? 0),
    queryFn: () => {
      if (ticketId === null) throw new Error("ticketId is required");
      return getAttachmentsByTicketId(ticketId);
    },
    enabled: Boolean(session) && typeof ticketId === "number",
    staleTime: 2 * 60 * 1000, // 2 phút — attachments ít thay đổi
  });

  // Chỉ sign out khi token thực sự hết hạn (401 Unauthorized).
  // 403 Forbidden có nghĩa là token vẫn hợp lệ, nhưng user không có quyền truy cập
  // resource cụ thể này (ví dụ: Technician xem ticket không được assign cho họ).
  // Không nên sign out trong trường hợp 403 — chỉ hiển thị danh sách rỗng.
  useEffect(() => {
    if (query.error && axios.isAxiosError(query.error)) {
      const status = query.error.response?.status ?? 0;
      if (status === 401) {
        signOut();
      }
      // 403: không có quyền xem attachment của ticket này → im lặng, trả [] (default)
    }
  }, [query.error, signOut]);

  return query;
}
