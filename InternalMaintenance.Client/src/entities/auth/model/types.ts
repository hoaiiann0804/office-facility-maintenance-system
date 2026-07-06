export type RoleName = "Admin" | "Manager" | "Technician" | "Staff";

export interface AuthUser {
  id: number;
  fullName: string;
  email: string;
  roleName: RoleName;
  departmentId: number;
  departmentName?: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresInMinutes: number;
  refreshToken: string;
  mustChangePassword: boolean;
  user: AuthUser;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresInMinutes: number;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}
