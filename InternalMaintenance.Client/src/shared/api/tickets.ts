import { http } from "./http";
import type {
  AssignTicketRequest,
  ChangeTicketStatusRequest,
  CreateTicketCommentRequest,
  MaintenanceTicket,
  MaintenanceTicketDetail,
  PagedResponse,
  TicketComment,
  TicketHistoryItem,
  TicketQuery,
  UpdateTicketRequest,
  TicketStatusHistoryResponse,
} from "../../entities/ticket/model/types";

export async function getTickets(query: TicketQuery = {}) {
  const { data } = await http.get<PagedResponse<MaintenanceTicket>>("/tickets", { params: query });
  return data;
}

export async function getTicketById(id: number) {
  const { data } = await http.get<MaintenanceTicket>(`/tickets/${id}`);
  return data;
}

export async function getTicketDetailById(id: number) {
  const [ticket, comments, history] = await Promise.all([
    getTicketById(id),
    getTicketComments(id),
    getTicketHistory(id),
  ]);

  const detail: MaintenanceTicketDetail = {
    ...ticket,
    comments,
    history,
  };

  return detail;
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

export async function updateTicket(id: number, payload: UpdateTicketRequest) {
  const { data } = await http.put<MaintenanceTicket>(`/tickets/${id}`, payload);
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

export async function getTicketHistory(id: number): Promise<TicketHistoryItem[]> {
  const { data } = await http.get<TicketStatusHistoryResponse[]>(`/tickets/${id}/history`);
  return data.map((item) => ({
    id: item.id,
    status: item.newStatus,
    note: item.note ?? "",
    changedBy: item.changedByUserName,
    changedAt: item.changedAt,
  }));
}
