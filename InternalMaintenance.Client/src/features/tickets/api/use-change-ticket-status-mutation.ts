import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useAuthStore } from "../../auth/model/auth-store";
import { changeTicketStatus } from "../../../shared/api/tickets";
import type { ChangeTicketStatusRequest } from "../../../entities/ticket/model/types";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useChangeTicketStatusMutation(ticketId: number | null) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: async (payload: ChangeTicketStatusRequest) => {
      if (ticketId === null) throw new Error("Ticket id is required");
      return changeTicketStatus(ticketId, payload);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tickets"] });
      await queryClient.invalidateQueries({ queryKey: ["tickets", ticketId] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
