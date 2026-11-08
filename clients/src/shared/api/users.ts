import { http } from "./http";
import type {
  User,
  UserQuery,
  CreateUserRequest,
  UpdateUserRequest,
  ResetUserPasswordRequest,
} from "../../entities/user/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

// Note: GET /api/users is [Authorize(Roles = "Admin")] only.
// Manager/Technician/Staff will receive 403 — handle that in the hook layer.
export async function getUsers(query: UserQuery = {}) {
  const { data } = await http.get<PagedResponse<User>>("/users", { params: query });
  return data;
}

export async function getUserById(id: number) {
  const { data } = await http.get<User>(`/users/${id}`);
  return data;
}

export async function createUser(payload: CreateUserRequest) {
  const { data } = await http.post<User>("/users", payload);
  return data;
}

export async function updateUser(id: number, payload: UpdateUserRequest) {
  const { data } = await http.put<User>(`/users/${id}`, payload);
  return data;
}

export async function updateUserActive(id: number, isActive: boolean) {
  const { data } = await http.patch<User>(`/users/${id}/status`, { isActive });
  return data;
}

export async function resetUserPassword(id: number, payload: ResetUserPasswordRequest) {
  const { data } = await http.patch<{ message: string }>(`/users/${id}/reset-password`, payload);
  return data;
}
