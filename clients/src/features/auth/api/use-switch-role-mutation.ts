import { useMutation, useQueryClient } from "@tanstack/react-query";
import { switchRole } from "../../../shared/api/auth";
import { useAuthStore } from "../model/auth-store";

export function useSwitchRoleMutation() {
  const setSession = useAuthStore((state) => state.setSession);
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roleName: string) => switchRole(roleName),
    onSuccess: (response) => {
      setSession({
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        user: response.user,
      });
      // Invalidate all query caches to refresh data with the new role scope
      queryClient.invalidateQueries();
    },
  });
}
