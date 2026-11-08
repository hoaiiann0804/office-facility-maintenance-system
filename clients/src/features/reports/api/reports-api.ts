import { http } from "@/shared/api/http";
import { useQuery } from "@tanstack/react-query";

export interface ReportFilterQuery {
  fromDate?: string;
  toDate?: string;
  departmentId?: number;
  technicianId?: number;
}

export interface TechnicianPerformanceItem {
  technicianId: number;
  fullName: string;
  email: string;
  departmentName: string;
  totalAssigned: number;
  inProgressCount: number;
  waitingForVendorCount: number;
  totalResolved: number;
  totalClosed: number;
  metSlaCount: number;
  missedSlaCount: number;
  slaComplianceRate: number;
  avgResolutionHours: number;
}

export interface SlaPriorityItem {
  priority: string;
  totalTickets: number;
  metSlaCount: number;
  missedSlaCount: number;
  complianceRate: number;
}

export interface MonthlySlaTrendItem {
  month: string;
  totalResolved: number;
  metSlaCount: number;
  missedSlaCount: number;
  complianceRate: number;
}

export interface SlaComplianceResponse {
  totalTickets: number;
  inSlaActiveCount: number;
  nearBreachCount: number;
  breachedActiveCount: number;
  pausedCount: number;
  metSlaCount: number;
  missedSlaCount: number;
  overallComplianceRate: number;
  byPriority: SlaPriorityItem[];
  monthlyTrends: MonthlySlaTrendItem[];
}

export interface MonthlyCostItem {
  month: string;
  totalCost: number;
  ticketCount: number;
}

export interface DepartmentCostItem {
  departmentId: number;
  departmentName: string;
  totalCost: number;
  ticketCount: number;
}

export interface VendorCostItem {
  vendorId: number;
  vendorName: string;
  totalCost: number;
  dispatchCount: number;
}

export interface CostlyEquipmentItem {
  equipmentId: number;
  equipmentCode: string;
  equipmentName: string;
  departmentName: string;
  totalRepairCost: number;
  maintenanceCount: number;
}

export interface MaintenanceCostResponse {
  totalMaintenanceCost: number;
  totalVendorDispatches: number;
  avgCostPerTicket: number;
  monthlyCosts: MonthlyCostItem[];
  departmentCosts: DepartmentCostItem[];
  vendorCosts: VendorCostItem[];
  topCostlyEquipment: CostlyEquipmentItem[];
}

export function useTechnicianPerformanceQuery(filter: ReportFilterQuery) {
  return useQuery<TechnicianPerformanceItem[]>({
    queryKey: ["reports", "technician-performance", filter],
    queryFn: async () => {
      const { data } = await http.get("/reports/technician-performance", { params: filter });
      return data;
    },
  });
}

export function useSlaComplianceQuery(filter: ReportFilterQuery) {
  return useQuery<SlaComplianceResponse>({
    queryKey: ["reports", "sla-compliance", filter],
    queryFn: async () => {
      const { data } = await http.get("/reports/sla-compliance", { params: filter });
      return data;
    },
  });
}

export function useMaintenanceCostQuery(filter: ReportFilterQuery) {
  return useQuery<MaintenanceCostResponse>({
    queryKey: ["reports", "maintenance-cost", filter],
    queryFn: async () => {
      const { data } = await http.get("/reports/maintenance-cost", { params: filter });
      return data;
    },
  });
}

export async function downloadExcelReport(filter: ReportFilterQuery) {
  const response = await http.get("/reports/export/excel", {
    params: filter,
    responseType: "blob",
  });

  const blob = new Blob([response.data], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  const dateStr = new Date().toISOString().slice(0, 10);
  link.setAttribute("download", `BaoCao_BaoTri_${dateStr}.xlsx`);
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
