import type { TicketPriority, TicketStatus } from "./types";

export const TICKET_WORKFLOW_STATUSES: TicketStatus[] = [
  "Pending",
  "Assigned",
  "InProgress",
  "Resolved",
  "Closed",
];

export const TICKET_PRIORITIES: TicketPriority[] = ["Low", "Medium", "High", "Critical"];
