import { http } from "./http";
import type { User, UserQuery } from "../../entities/user/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

// Note: GET /api/users is [Authorize(Roles = "Admin")] only.
// Manager/Technician/Staff will receive 403 — handle that in the hook layer.
export async function getUsers(query: UserQuery = {}) {
  const { data } = await http.get<PagedResponse<User>>("/users", { params: query });
  return data;
}
