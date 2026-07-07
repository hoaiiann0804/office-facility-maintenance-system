import axios from "axios";
import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTicketDetailById } from "../../../shared/api/tickets";
import type { MaintenanceTicketDetail } from "../../../entities/ticket/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useTicketDetailQuery(ticketId: number | null) {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);

  const query = useQuery<MaintenanceTicketDetail>({
    queryKey: ["tickets", ticketId, session?.accessToken],
    queryFn: () => {
      if (ticketId === null) {
        throw new Error("Ticket id is required");
      }

      return getTicketDetailById(ticketId);
    },
    enabled: Boolean(session) && typeof ticketId === "number",
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (query.error && isAuthError(query.error)) {
      signOut();
    }
  }, [query.error, signOut]);

  return query;
}
