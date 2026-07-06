import { useQuery } from "@tanstack/react-query";
import { getTickets } from "../../../shared/api/tickets";
import type { TicketQuery } from "../../../entities/ticket/model/types";

export function useTicketsQuery(query: TicketQuery = {}) {
  return useQuery({
    queryKey: ["tickets", query],
    queryFn: () => getTickets(query),
  });
}
