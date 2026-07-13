import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { UpdateTicketRequest } from "../../../entities/ticket/model/types";
import { useAuthStore } from "../../auth/model/auth-store";
import { updateTicket } from "../../../shared/api/tickets";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useUpdateTicketMutation(ticketId: number | null) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: async (payload: UpdateTicketRequest) => {
      if (ticketId === null) {
        throw new Error("Ticket id is required");
      }
      return updateTicket(ticketId, payload);
    },
    onSuccess: async () => {
      // Refresh cả danh sách lẫn chi tiết của ticket vừa sửa
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
      await queryClient.invalidateQueries({ queryKey: ["tickets", ticketId] });
    },
    onError: (error) => {
      if (isAuthError(error)) {
        signOut();
      }
    },
  });
}
