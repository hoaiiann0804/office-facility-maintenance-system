import axios from "axios";
import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { me } from "../../../shared/api/auth";
import type { AuthUser } from "../../../entities/auth/model/types";
import { useAuthStore } from "../model/auth-store";

function isAuthError(error: unknown) {
  return (
    axios.isAxiosError(error) && (error.response?.status === 401 || error.response?.status === 403)
  );
}

function isSameUser(left: AuthUser, right: AuthUser) {
  return (
    left.id === right.id &&
    left.fullName === right.fullName &&
    left.email === right.email &&
    left.roleName === right.roleName &&
    left.departmentId === right.departmentId &&
    left.departmentName === right.departmentName &&
    left.departmentIsMaintenanceTeam === right.departmentIsMaintenanceTeam &&
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

    if (!query.data.isActive) {
      toast.error("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.");
      signOut();
      return;
    }

    if (isSameUser(session.user, query.data)) {
      return;
    }

    setSession({
      ...session,
      user: query.data,
    });
  }, [query.data, session, setSession, signOut]);

  useEffect(() => {
    if (!session || !query.error) {
      return;
    }

    if (isAuthError(query.error)) {
      console.error("Session authorization check failed:", query.error);
      if (axios.isAxiosError(query.error) && query.error.response?.status === 403) {
        toast.error("Tài khoản của bạn đã bị khóa hoặc vô hiệu hóa.");
      }
      signOut();
    }
  }, [query.error, session, signOut]);

  return {
    isCheckingAuth: Boolean(session) && query.isPending,
  };
}
