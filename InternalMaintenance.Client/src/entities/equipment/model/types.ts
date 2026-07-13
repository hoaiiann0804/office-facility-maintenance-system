export type EquipmentStatus = "Active" | "Inactive" | "UnderMaintenance" | "Retired";

export interface Equipment {
  id: number;
  code: string;
  name: string;
  departmentId: number;
  departmentName: string;
  status: EquipmentStatus;
  purchasedDate: string | null;
  description: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface EquipmentQuery {
  keyword?: string;
  departmentId?: number;
  status?: EquipmentStatus;
  page?: number;
  pageSize?: number;
}
