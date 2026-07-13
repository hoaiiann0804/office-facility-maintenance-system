import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import axios from "axios";
import { getEquipment } from "../../../shared/api/equipment";
import type { EquipmentQuery } from "../../../entities/equipment/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useEquipmentQuery(equipmentQuery: EquipmentQuery = {}) {
  const session = useAuthStore((state) => state.session);
  const signOut = useAuthStore((state) => state.signOut);

  const query = useQuery({
    queryKey: ["equipment", equipmentQuery, session?.accessToken],
    queryFn: () => getEquipment(equipmentQuery),
    enabled: Boolean(session),
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    if (!query.error) return;
    if (isAuthError(query.error)) {
      signOut();
    }
  }, [query.error, signOut]);

  return query;
}
