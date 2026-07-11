import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { CreateTicketCommentRequest } from "../../../entities/ticket/model/types";
import { useAuthStore } from "../../auth/model/auth-store";
import { createTicketComment } from "../../../shared/api/tickets";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}
export function useCreateTicketCommentMutation(tickectId: number | null) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: async (payload: CreateTicketCommentRequest) => {
      if (tickectId === null) {
        throw new Error("Ticket id is required");
      }
      return createTicketComment(tickectId, payload);
    },

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["tickets", tickectId],
      });
    },
    onError: (error) => {
      if (isAuthError(error)) {
        signOut();
      }
    },
  });
}
