import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import {
  createDepartment,
  updateDepartment,
  deleteDepartment,
} from "../../../shared/api/departments";
import type {
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from "../../../entities/department/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useCreateDepartmentMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: CreateDepartmentRequest) => createDepartment(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["departments"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useUpdateDepartmentMutation(id: number) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: UpdateDepartmentRequest) => updateDepartment(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["departments"] });
      queryClient.invalidateQueries({ queryKey: ["departments", id] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useDeleteDepartmentMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (id: number) => deleteDepartment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["departments"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
