import { useQuery } from "@tanstack/react-query";
import { getTickets } from "../../../shared/api/tickets";
import type { TicketQuery } from "../../../entities/ticket/model/types";
import { useAuthStore } from "../../auth/model/auth-store";
import { useEffect } from "react";
import axios from "axios";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useTicketsQuery(ticketQuery: TicketQuery = {}) {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);

  const query = useQuery({
    queryKey: ["tickets", ticketQuery, session?.accessToken],
    queryFn: () => getTickets(ticketQuery), // API `getTickets` cần xử lý `keyword`
    enabled: Boolean(session),
    retry: false, // Nếu API bị lỗi đừng cố gắng gọi lại
    staleTime: 5 * 60 * 1000, // Coi dữ liệu là mới trong 5 phút để tránh fetch lại ko cần thiết
  });

  useEffect(() => {
    if (!query.error) {
      return;
    }
    if (isAuthError(query.error)) {
      signOut();
    }
  }, [query.error, signOut]);

  return query;
}
