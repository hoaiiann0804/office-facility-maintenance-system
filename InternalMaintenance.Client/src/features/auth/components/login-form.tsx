import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { wireframeData } from "../../../shared/mock/wireframe-data";
import { Badge, Panel } from "../../../shared/ui";
import { useAuthStore } from "../model/auth-store";
import { useNavigate } from "react-router-dom";
import { appRoutes } from "../../../shared/config/routes";

const loginSchema = z.object({
  email: z.string().email("Email không hợp lệ"),
  password: z.string().min(6, "Mật khẩu phải có ít nhất 6 ký tự"),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export function LoginForm() {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setValue,
    setError,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: wireframeData.quickLogins[0]?.email ?? "",
      password: wireframeData.quickLogins[0]?.password ?? "",
    },
  });

  const onSubmit = handleSubmit(async (values: LoginFormValues) => {
    const quickUser = wireframeData.users.find(
      (user) => user.email.toLowerCase() === values.email.toLowerCase(),
    );

    if (!quickUser || quickUser.password !== values.password) {
      setError("root", { type: "manual", message: "Email hoặc mật khẩu không đúng." });
      return;
    }

    setSession({
      accessToken: "demo-access-token",
      refreshToken: "demo-refresh-token",
      user: {
        id: quickUser.id,
        fullName: quickUser.fullName,
        email: quickUser.email,
        roleName: quickUser.roleName,
        departmentId: quickUser.departmentId,
        departmentName: quickUser.departmentName,
        isActive: quickUser.isActive,
        mustChangePassword: quickUser.mustChangePassword,
      },
    });

    navigate(appRoutes.dashboard);
  });

  return (
    <>
      <section className="auth-hero panel">
        <span className="eyebrow">Internal maintenance management</span>
        <h1>Điều phối bảo trì nội bộ, gọn, rõ, dễ mở rộng.</h1>
        <p className="hero-copy">
          Bản React này tách đúng các lớp app, shared, entities, features và pages để sau này nối
          API thật không bị dồn hết vào một component.
        </p>

        <div className="stats-grid">
          <div className="stat-card">
            <div className="stat-card-label">Login</div>
            <div className="stat-card-value">JWT + refresh</div>
            <div className="stat-card-note">Ready for auth flow</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-label">Catalog</div>
            <div className="stat-card-value">Departments + equipment</div>
            <div className="stat-card-note">Data-driven modules</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-label">Workflow</div>
            <div className="stat-card-value">Ticket history</div>
            <div className="stat-card-note">Audit-friendly</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-label">Roles</div>
            <div className="stat-card-value">Admin / Manager / Staff / Technician</div>
            <div className="stat-card-note">RBAC first</div>
          </div>
        </div>

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
      </section>

      <section className="auth-card panel panel-light">
        <span className="eyebrow eyebrow-dark">Demo login</span>
        <h2>Vào khu mô phỏng</h2>
        <p className="section-lead">
          Quick login vẫn chạy local để demo, còn lớp API thật đã được chuẩn bị sẵn bên dưới.
        </p>

        <form className="auth-form" onSubmit={onSubmit}>
          <label className="field">
            <span>Email</span>
            <input className="input" type="email" {...register("email")} />
            {errors.email ? <small className="field-error">{errors.email.message}</small> : null}
          </label>

          <label className="field">
            <span>Password</span>
            <input className="input" type="password" {...register("password")} />
            {errors.password ? (
              <small className="field-error">{errors.password.message}</small>
            ) : null}
          </label>

          {errors.root ? <div className="banner error">{errors.root.message}</div> : null}

          <button type="submit" className="button primary" disabled={isSubmitting}>
            {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
          </button>
        </form>

        <div className="api-alignment panel subtle">
          <span className="eyebrow eyebrow-dark">API alignment</span>
          <div className="stack">
            <div className="mini-card">
              <strong>Auth</strong>
              <span>login, me, change-password, refresh-token, logout</span>
            </div>
            <div className="mini-card">
              <strong>Tickets</strong>
              <span>create, assign, status, comments, history</span>
            </div>
            <div className="mini-card">
              <strong>Catalog</strong>
              <span>departments, equipment, users</span>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}
