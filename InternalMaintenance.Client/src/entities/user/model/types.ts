import type { RoleName } from "../../auth/model/types";

export interface User {
  id: number;
  fullName: string;
  email: string;
  roleId: number;
  roleName: RoleName;
  departmentId: number | null;
  departmentName: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UserQuery {
  keyword?: string;
  role?: RoleName;
  departmentId?: number;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
