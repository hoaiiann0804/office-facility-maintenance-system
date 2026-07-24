export interface Department {
  id: number;
  name: string;
  description: string | null;
  isMaintenanceTeam: boolean;
  createdAt: string;
}

export interface DepartmentQuery {
  keyword?: string;
  isMaintenanceTeam?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string | null;
  isMaintenanceTeam: boolean;
}

export interface UpdateDepartmentRequest {
  name: string;
  description?: string | null;
  isMaintenanceTeam: boolean;
}
