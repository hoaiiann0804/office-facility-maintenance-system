import { useMutation } from "@tanstack/react-query";
import { login } from "../../../shared/api/auth";
import { useAuthStore } from "../model/auth-store";

export function useLoginMutation() {
  const setSession = useAuthStore((state) => state.setSession);

  return useMutation({
    mutationFn: login,
    onSuccess: (response) => {
      setSession({
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        user: response.user,
      });
    },
  });
}
