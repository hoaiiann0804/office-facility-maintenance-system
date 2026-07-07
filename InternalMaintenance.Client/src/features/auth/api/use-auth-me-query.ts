import axios from "axios";
import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { me } from "../../../shared/api/auth";
import type { AuthUser } from "../../../entities/auth/model/types";
import { useAuthStore } from "../model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

function isSameUser(left: AuthUser, right: AuthUser) {
  return (
    left.id === right.id &&
    left.fullName === right.fullName &&
    left.email === right.email &&
    left.roleName === right.roleName &&
    left.departmentId === right.departmentId &&
    left.departmentName === right.departmentName &&
    left.isActive === right.isActive &&
    left.mustChangePassword === right.mustChangePassword
  );
}

export function useAuthMeQuery() {
  const session = useAuthStore((state) => state.session);
  const setSession = useAuthStore((state) => state.setSession);
  const signOut = useAuthStore((state) => state.signOut);

  const query = useQuery({
    queryKey: ["auth", "me", session?.accessToken],
    queryFn: me,
    enabled: Boolean(session),
    retry: false,
    staleTime: 0,
  });

  useEffect(() => {
    if (!session || !query.data) {
      return;
    }

    if (isSameUser(session.user, query.data)) {
      return;
    }

    setSession({
      ...session,
      user: query.data,
    });
  }, [query.data, session, setSession]);

  useEffect(() => {
    if (!session || !query.error) {
      return;
    }

    if (isAuthError(query.error)) {
      signOut();
    }
  }, [query.error, session, signOut]);

  return {
    isCheckingAuth: Boolean(session) && query.isPending,
  };
}
