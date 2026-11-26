import { http } from "@/shared/api/http";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

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

// [BƯỚC 3B - F-02]: DTO biểu diễn bản ghi thông báo email SLA bị lỗi trong bảng SlaNotificationOutboxMessages
export interface SlaOutboxFailureItem {
  outboxId: number;
  maintenanceTicketId: number;
  ticketCode: string;
  notificationType: number | string;
  queuedAt: string;
  failedAt: string;
  attemptCount: number;
  error: string | null;
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

// [BƯỚC 3B - F-02]: Lấy danh sách các thông báo email SLA bị lỗi (GET /api/maintenance/sla-notifications/failed)
export function useFailedSlaNotificationsQuery(limit = 50, enabled = true) {
  return useQuery<SlaOutboxFailureItem[]>({
    queryKey: ["reports", "sla-outbox-failures", limit],
    queryFn: async () => {
      const { data } = await http.get("/maintenance/sla-notifications/failed", {
        params: { limit },
      });
      return data;
    },
    enabled,
  });
}

// [BƯỚC 3B - F-02]: Đưa thông báo SLA bị lỗi vào lại hàng đợi để SlaMonitorWorker gửi lại (POST .../retry)
export function useRetrySlaNotificationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (outboxId: number) => {
      const { data } = await http.post(`/maintenance/sla-notifications/failed/${outboxId}/retry`);
      return data as { message: string };
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["reports", "sla-outbox-failures"] });
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
