import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { createEquipment, updateEquipment, deleteEquipment } from "../../../shared/api/equipment";
import type {
  CreateEquipmentRequest,
  UpdateEquipmentRequest,
} from "../../../entities/equipment/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useCreateEquipmentMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: CreateEquipmentRequest) => createEquipment(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useUpdateEquipmentMutation(id: number) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: UpdateEquipmentRequest) => updateEquipment(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      queryClient.invalidateQueries({ queryKey: ["equipment", id] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useDeleteEquipmentMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (id: number) => deleteEquipment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
