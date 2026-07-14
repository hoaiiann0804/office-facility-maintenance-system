import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { getDepartments } from "../../../shared/api/departments";
import type { DepartmentQuery } from "../../../entities/department/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useDepartmentsQuery(query: DepartmentQuery = {}) {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);

  const q = useQuery({
    queryKey: ["departments", query, session?.accessToken],
    queryFn: () => getDepartments(query),
    enabled: Boolean(session),
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (q.error && isAuthError(q.error)) {
      signOut();
    }
  }, [q.error, signOut]);

  return q;
}
