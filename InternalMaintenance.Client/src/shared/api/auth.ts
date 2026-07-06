import { http } from "./http";
import type {
  AuthUser,
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
} from "../../entities/auth/model/types";

export async function login(request: LoginRequest) {
  const { data } = await http.post<LoginResponse>("/auth/login", request);
  return data;
}

export async function me() {
  const { data } = await http.get<AuthUser>("/auth/me");
  return data;
}

export async function changePassword(request: ChangePasswordRequest) {
  const { data } = await http.post<{ message: string }>("/auth/change-password", request);
  return data;
}

export async function refreshToken(refreshToken: string) {
  const { data } = await http.post<RefreshTokenResponse>("/auth/refresh-token", {
    refreshToken,
  });
  return data;
}

export async function logout(refreshToken: string) {
  const { data } = await http.post<void>("/auth/logout", { refreshToken });
  return data;
}
