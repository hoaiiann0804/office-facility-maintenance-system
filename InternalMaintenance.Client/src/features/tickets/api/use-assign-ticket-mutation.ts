import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useAuthStore } from "../../auth/model/auth-store";
import { assignTicket } from "../../../shared/api/tickets";
import type { AssignTicketRequest } from "../../../entities/ticket/model/types";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useAssignTicketMutation(ticketId: number | null) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: async (payload: AssignTicketRequest) => {
      if (ticketId === null) throw new Error("Ticket id is required");
      return assignTicket(ticketId, payload);
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
