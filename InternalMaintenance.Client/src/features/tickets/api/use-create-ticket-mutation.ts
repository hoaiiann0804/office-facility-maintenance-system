import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useAuthStore } from "../../auth/model/auth-store";
import { createTicket } from "../../../shared/api/tickets";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useCreateTicketMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: async (payload: Parameters<typeof createTicket>[0]) => {
      return createTicket(payload);
    },
    onSuccess: async () => {
      //  Sử dụng parameters để tự động lấy type của payload
      await queryClient.invalidateQueries({
        queryKey: ["tickets"],
      });
    },
    onError: (error) => {
      if (isAuthError(error)) {
        signOut();
      }
    },
  });
}
