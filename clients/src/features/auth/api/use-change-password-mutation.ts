import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { changePassword } from "../../../shared/api/auth";
import type { AuthUser, ChangePasswordRequest } from "../../../entities/auth/model/types";
import { useAuthStore } from "../model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && error.response?.status === 401;
}

export function useChangePasswordMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: ChangePasswordRequest) => changePassword(payload),
    onSuccess: () => {
      // [FIX G-11 (Mục 3.1.3) - CHỐNG BỊ useAuthMeQuery ĐÈ LẠI STATE CŨ]:
      // Ở main.tsx, AuthBootstrap luôn chạy useAuthMeQuery() với useEffect theo dõi [query.data, session].
      // Nếu chỉ cập nhật useAuthStore mà không cập nhật cache ["auth", "me", accessToken] của React Query,
      // useAuthMeQuery sẽ thấy session.user khác với query.data cũ (mustChangePassword = true) và ghi đè ngược lại!
      // Vì vậy phải cập nhật đồng bộ cả cache ["auth", "me"] lẫn useAuthStore.
      const { session, setSession } = useAuthStore.getState();
      if (session) {
        const updatedUser: AuthUser = {
          ...session.user,
          mustChangePassword: false,
        };
        queryClient.setQueryData<AuthUser>(["auth", "me", session.accessToken], updatedUser);
        setSession({
          ...session,
          user: updatedUser,
        });
      }
      void queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
