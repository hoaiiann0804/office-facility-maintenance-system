import { http } from "./http";

export interface DashboardSummaryResponse {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  closedTickets: number;
  totalEquipment: number;
  activeEquipment: number;
  underMaintenanceEquipment: number;
  totalTechnicians: number;
  totalDepartments: number;
}

export interface ChartItem {
  name: string;
  value: number;
}

export interface ChartDataResponse {
  ticketsByStatus: ChartItem[];
  ticketsByPriority: ChartItem[];
  equipmentByDepartment: ChartItem[];
}

export const getDashboardSummary = async (): Promise<DashboardSummaryResponse> => {
  const { data } = await http.get<DashboardSummaryResponse>("/Dashboard/summary");
  return data;
};

export const getDashboardCharts = async (): Promise<ChartDataResponse> => {
  const { data } = await http.get<ChartDataResponse>("/Dashboard/charts");
  return data;
};
