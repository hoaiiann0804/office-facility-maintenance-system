import { http } from "./http";
import type {
  Department,
  DepartmentQuery,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from "../../entities/department/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

export async function getDepartments(query: DepartmentQuery = {}) {
  const { data } = await http.get<PagedResponse<Department>>("/departments", { params: query });
  return data;
}

export async function getDepartmentById(id: number) {
  const { data } = await http.get<Department>(`/departments/${id}`);
  return data;
}

export async function createDepartment(payload: CreateDepartmentRequest) {
  const { data } = await http.post<Department>("/departments", payload);
  return data;
}

export async function updateDepartment(id: number, payload: UpdateDepartmentRequest) {
  const { data } = await http.put<Department>(`/departments/${id}`, payload);
  return data;
}

export async function deleteDepartment(id: number) {
  await http.delete(`/departments/${id}`);
}
