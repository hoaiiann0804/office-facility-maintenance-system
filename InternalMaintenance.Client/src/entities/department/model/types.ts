export interface Department {
  id: number;
  name: string;
  description: string | null;
  createdAt: string;
}

export interface DepartmentQuery {
  keyword?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string | null;
}

export interface UpdateDepartmentRequest {
  name: string;
  description?: string | null;
}
