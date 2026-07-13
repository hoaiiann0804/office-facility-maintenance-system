import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useState } from "react";
import { z } from "zod";
import axios from "axios";
import { toast } from "sonner";
import { useNavigate } from "react-router-dom";
import { wireframeData } from "../../../shared/mock/wireframe-data";
import { appRoutes } from "../../../shared/config/routes";
import { useLoginMutation } from "../api/use-login-mutation";
import { Badge, Panel, ThemeToggle } from "../../../shared/ui";

const loginSchema = z.object({
  email: z.string().email("Email không hợp lệ"),
  password: z.string().min(6, "Mật khẩu phải có ít nhất 6 ký tự"),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const [showPassword, setShowPassword] = useState(false);
  const navigate = useNavigate();
  const loginMutation = useLoginMutation();
  const isDev = import.meta.env.DEV;
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setValue,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: wireframeData.quickLogins[0]?.email ?? "",
      password: wireframeData.quickLogins[0]?.password ?? "",
    },
  });

  const onSubmit = async (values: LoginFormValues) => {
    try {
      const response = await loginMutation.mutateAsync(values);

      toast.success(
        `Chào mừng ${response.user.fullName}. Đang mở dashboard theo vai trò ${response.user.roleName}.`,
      );

      navigate(appRoutes.dashboard, { replace: true });
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0)) {
        toast.error("Email hoặc mật khẩu không đúng.");
        return;
      }

      toast.error("Không thể đăng nhập. Vui lòng thử lại.");
    }
  };

  return (
    <>
      <style>
        {`
          .password-input-wrapper {
            position: relative;
            display: flex;
            align-items: center;
          }

          .password-input-wrapper .input {
            padding-right: 40px;
          }

          .password-toggle {
            position: absolute;
            right: 8px;
            top: 50%;
            transform: translateY(-50%);
            background: none;
            border: none;
            cursor: pointer;
            padding: 4px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            color: #6b7280;
          }
        `}
      </style>

      <section className="auth-hero panel">
        <span className="eyebrow">Office Facility Maintenance Management System</span>
        <h1>Điều phối bảo trì nội bộ, gọn, rõ, dễ mở rộng.</h1>
        <p className="hero-copy">
          Đăng nhập để quản lý thiết bị, ticket bảo trì, phân công kỹ thuật viên và theo dõi tiến độ
          xử lý.
        </p>

        {isDev ? (
          <Panel className="subtle">
            <span className="eyebrow">Quick login</span>
            <div className="quick-login-list">
              {wireframeData.quickLogins.map((item) => (
                <button
                  key={item.email}
                  type="button"
                  className="quick-login-item"
                  onClick={() => {
                    setValue("email", item.email, { shouldValidate: true });
                    setValue("password", item.password, { shouldValidate: true });
                  }}
                >
                  <div className="quick-login-top">
                    <strong>{item.label}</strong>
                    <Badge
                      tone={
                        item.role === "Admin"
                          ? "primary"
                          : item.role === "Manager"
                            ? "warn"
                            : item.role === "Technician"
                              ? "good"
                              : "default"
                      }
                    >
                      {item.role}
                    </Badge>
                  </div>
                  <span>{item.email}</span>
                  <small>{item.hint}</small>
                </button>
              ))}
            </div>
          </Panel>
        ) : null}
      </section>

      <section className="auth-card panel">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <span className="eyebrow">Đăng nhập</span>
          <ThemeToggle />
        </div>
        <h2>Vào hệ thống</h2>
        <p className="section-lead">
          Sử dụng tài khoản được cấp để truy cập hệ thống quản lý bảo trì nội bộ.
        </p>

        <form className="auth-form" onSubmit={handleSubmit(onSubmit)}>
          <label className="field">
            <span>Email</span>
            <input className="input" type="email" {...register("email")} />
            {errors.email ? <small className="field-error">{errors.email.message}</small> : null}
          </label>

          <label className="field">
            <span>Password</span>
            <div className="password-input-wrapper">
              <input
                className="input"
                type={showPassword ? "text" : "password"}
                {...register("password")}
              />
              <button
                type="button"
                className="password-toggle"
                onClick={() => setShowPassword(!showPassword)}
                aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              >
                {showPassword ? (
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                    <line x1="1" y1="1" x2="23" y2="23"></line>
                  </svg>
                ) : (
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                    <circle cx="12" cy="12" r="3"></circle>
                  </svg>
                )}
              </button>
            </div>
            {errors.password ? (
              <small className="field-error">{errors.password.message}</small>
            ) : null}
          </label>

          <button
            type="submit"
            className="button primary"
            disabled={isSubmitting || loginMutation.isPending}
          >
            {isSubmitting || loginMutation.isPending ? "Đang đăng nhập..." : "Đăng nhập"}
          </button>
        </form>
      </section>
    </>
  );
}
