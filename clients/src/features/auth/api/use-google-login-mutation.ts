import { useMutation } from "@tanstack/react-query";
import { loginWithGoogle } from "../../../shared/api/auth";
import { useAuthStore } from "../model/auth-store";

export function useGoogleLoginMutation() {
  const setSession = useAuthStore((state) => state.setSession);

  return useMutation({
    mutationFn: (idToken: string) => loginWithGoogle(idToken),
    onSuccess: (response) => {
      setSession({
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        user: response.user,
      });
    },
  });
}
