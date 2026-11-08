/**
 * use-delete-attachment-mutation.ts
 *
 * Mutation để xóa một attachment.
 *
 * Dùng useMutation của React Query (không cần track progress,
 * chỉ cần biết loading/error/success).
 *
 * Sau khi xóa thành công → invalidate cache để list tự cập nhật.
 */

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { deleteAttachment } from "../../../shared/api/attachments";
import { attachmentsQueryKey } from "./use-ticket-attachments-query";

export function useDeleteAttachmentMutation(ticketId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (attachmentId: number) => deleteAttachment(ticketId, attachmentId),

    onSuccess: async () => {
      // Sau khi xóa thành công, invalidate cache của ticket này
      // → danh sách sẽ được fetch lại tự động
      await queryClient.invalidateQueries({
        queryKey: attachmentsQueryKey(ticketId),
      });
    },
  });
}
