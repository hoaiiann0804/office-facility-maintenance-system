import { http } from "./http";
import type { Department, DepartmentQuery } from "../../entities/department/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

export async function getDepartments(query: DepartmentQuery = {}) {
  const { data } = await http.get<PagedResponse<Department>>("/departments", { params: query });
  return data;
}

export async function getDepartmentById(id: number) {
  const { data } = await http.get<Department>(`/departments/${id}`);
  return data;
}
