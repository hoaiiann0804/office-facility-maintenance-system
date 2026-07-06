import { http } from "./http";
import type {
  AssignTicketRequest,
  ChangeTicketStatusRequest,
  CreateTicketCommentRequest,
  MaintenanceTicket,
  PagedResponse,
  TicketComment,
  TicketHistoryItem,
  TicketQuery,
} from "../../entities/ticket/model/types";

export async function getTickets(query: TicketQuery = {}) {
  const { data } = await http.get<PagedResponse<MaintenanceTicket>>("/tickets", { params: query });
  return data;
}

export async function getTicketById(id: number) {
  const { data } = await http.get<MaintenanceTicket>(`/tickets/${id}`);
  return data;
}

export async function createTicket(payload: {
  title: string;
  description: string;
  equipmentId: number;
  priority?: string;
}) {
  const { data } = await http.post<MaintenanceTicket>("/tickets", payload);
  return data;
}

export async function assignTicket(id: number, payload: AssignTicketRequest) {
  const { data } = await http.patch<MaintenanceTicket>(`/tickets/${id}/assign`, payload);
  return data;
}

export async function changeTicketStatus(id: number, payload: ChangeTicketStatusRequest) {
  const { data } = await http.patch<MaintenanceTicket>(`/tickets/${id}/status`, payload);
  return data;
}

export async function createTicketComment(id: number, payload: CreateTicketCommentRequest) {
  const { data } = await http.post<TicketComment>(`/tickets/${id}/comments`, payload);
  return data;
}

export async function getTicketComments(id: number) {
  const { data } = await http.get<TicketComment[]>(`/tickets/${id}/comments`);
  return data;
}

export async function getTicketHistory(id: number) {
  const { data } = await http.get<TicketHistoryItem[]>(`/tickets/${id}/history`);
  return data;
}
