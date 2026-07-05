export type Role = "Admin" | "Manager" | "Technician" | "Staff";
export type TicketStatus = "Pending" | "Assigned" | "InProgress" | "Resolved" | "Closed" | "Cancelled";
export type Priority = "Low" | "Medium" | "High" | "Critical";
export type EquipmentStatus = "Active" | "Inactive" | "UnderMaintenance" | "Retired";

export interface QuickLogin {
  label: string;
  email: string;
  password: string;
  role: Role;
  hint: string;
}

export interface Department {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: Role;
  departmentId: number;
  departmentName: string;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string;
  password: string;
}

export interface Equipment {
  id: number;
  code: string;
  name: string;
  departmentId: number;
  departmentName: string;
  status: EquipmentStatus;
  purchasedDate: string | null;
  description: string;
}

export interface TicketComment {
  id: number;
  userName: string;
  content: string;
  createdAt: string;
}

export interface TicketHistoryItem {
  id: number;
  status: TicketStatus;
  note: string;
  changedBy: string;
  changedAt: string;
}

export interface Ticket {
  id: number;
  ticketCode: string;
  title: string;
  description: string;
  equipmentId: number;
  equipmentCode: string;
  equipmentName: string;
  createdByUserId: number;
  createdByUserName: string;
  assignedTechnicianId: number | null;
  assignedTechnicianName: string | null;
  priority: Priority;
  status: TicketStatus;
  resolutionNote: string;
  createdAt: string;
  resolvedAt: string | null;
  closedAt: string | null;
  comments: TicketComment[];
  history: TicketHistoryItem[];
}

export interface ApiRoute {
  method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  note: string;
}

export interface ApiGroup {
  title: string;
  routes: ApiRoute[];
}

export interface WireframeData {
  quickLogins: QuickLogin[];
  workflow: TicketStatus[];
  priorities: Priority[];
  equipmentStatuses: EquipmentStatus[];
  departments: Department[];
  users: User[];
  equipment: Equipment[];
  tickets: Ticket[];
  apiGroups: ApiGroup[];
}
