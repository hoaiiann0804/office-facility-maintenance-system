import type { AuthUser } from "../../../entities/auth/model/types";

export interface QuickLogin {
  label: string;
  email: string;
  password: string;
  role: AuthUser["roleName"];
  hint: string;
}

export const DEV_QUICK_LOGINS: QuickLogin[] = [
  {
    label: "Admin",
    email: "admin@test.com",
    password: "Temp@123456",
    role: "Admin",
    hint: "Full access and configuration",
  },
  {
    label: "Manager",
    email: "manager@test.com",
    password: "Temp@123456",
    role: "Manager",
    hint: "Department scope and assignment",
  },
  {
    label: "Technician",
    email: "technician@test.com",
    password: "Temp@123456",
    role: "Technician",
    hint: "Execution and status updates",
  },
  {
    label: "Staff",
    email: "staff@test.com",
    password: "Temp@123456",
    role: "Staff",
    hint: "Create tickets and track progress",
  },
];

// Bộ tài khoản Quick Login riêng biệt dành cho môi trường Production (khớp với IdentitySeedData khi isProduction = true)
export const PROD_QUICK_LOGINS: QuickLogin[] = [
  {
    label: "Demo Admin",
    email: "demo.admin@office.vn",
    password: "Demo@Prod2026!",
    role: "Admin",
    hint: "Toàn quyền quản trị & Báo cáo (Production Demo)",
  },
  {
    label: "Demo Manager",
    email: "demo.manager@office.vn",
    password: "Demo@Prod2026!",
    role: "Manager",
    hint: "Điều phối & Giám sát SLA (Production Demo)",
  },
  {
    label: "Demo Kỹ thuật",
    email: "demo.tech@office.vn",
    password: "Demo@Prod2026!",
    role: "Technician",
    hint: "Xử lý sự cố & Nghiệm thu (Production Demo)",
  },
  {
    label: "Demo Nhân viên",
    email: "demo.staff@office.vn",
    password: "Demo@Prod2026!",
    role: "Staff",
    hint: "Báo hỏng & Theo dõi phiếu (Production Demo)",
  },
];
