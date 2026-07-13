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
