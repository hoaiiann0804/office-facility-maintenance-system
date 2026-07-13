import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { getUsers } from "../../../shared/api/users";
import type { UserQuery } from "../../../entities/user/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

// Backend: GET /api/users is Admin-only.
// For non-Admin users the query returns empty list gracefully (403 → fallback).
export function useUsersQuery(query: UserQuery = {}) {
  const session = useAuthStore((state) => state.session);

  return useQuery({
    queryKey: ["users", query, session?.accessToken],
    queryFn: async () => {
      try {
        return await getUsers(query);
      } catch (error) {
        // 403 = role không đủ quyền → trả về danh sách rỗng thay vì throw
        if (axios.isAxiosError(error) && error.response?.status === 403) {
          return {
            items: [],
            page: 1,
            pageSize: query.pageSize ?? 20,
            totalItems: 0,
            totalPages: 0,
          };
        }
        throw error;
      }
    },
    enabled: Boolean(session),
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
}
