import { useMutation } from "@tanstack/react-query";
import axios from "axios";
import { changePassword } from "../../../shared/api/auth";
import type { ChangePasswordRequest } from "../../../entities/auth/model/types";
import { useAuthStore } from "../model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useChangePasswordMutation() {
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: ChangePasswordRequest) => changePassword(payload),
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
