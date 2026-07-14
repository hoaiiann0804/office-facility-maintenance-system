import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import {
  createUser,
  updateUser,
  updateUserActive,
  resetUserPassword,
} from "../../../shared/api/users";
import type {
  CreateUserRequest,
  UpdateUserRequest,
  ResetUserPasswordRequest,
} from "../../../entities/user/model/types";
import { useAuthStore } from "../../auth/model/auth-store";

function isAuthError(error: unknown) {
  return axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0);
}

export function useCreateUserMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: CreateUserRequest) => createUser(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useUpdateUserMutation(id: number) {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: UpdateUserRequest) => updateUser(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["users", id] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useUpdateUserActiveMutation() {
  const queryClient = useQueryClient();
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      updateUserActive(id, isActive),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      queryClient.invalidateQueries({ queryKey: ["users", variables.id] });
    },
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}

export function useResetUserPasswordMutation(id: number) {
  const signOut = useAuthStore((state) => state.signOut);

  return useMutation({
    mutationFn: (payload: ResetUserPasswordRequest) => resetUserPassword(id, payload),
    onError: (error) => {
      if (isAuthError(error)) signOut();
    },
  });
}
