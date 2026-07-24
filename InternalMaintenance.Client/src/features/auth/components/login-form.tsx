import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useState } from "react";
import { z } from "zod";
import axios from "axios";
import { toast } from "sonner";
import { useNavigate } from "react-router-dom";
import { Eye, EyeOff, Shield, Wrench, Bell, BarChart3 } from "lucide-react";
import { wireframeData } from "../../../shared/mock/wireframe-data";
import { appRoutes } from "../../../shared/config/routes";
import { useLoginMutation } from "../api/use-login-mutation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

const loginSchema = z.object({
  email: z.string().email("Email không hợp lệ"),
  password: z.string().min(6, "Mật khẩu phải có ít nhất 6 ký tự"),
});

type LoginFormValues = z.infer<typeof loginSchema>;

const ROLE_BADGE: Record<string, "default" | "secondary" | "destructive" | "outline" | "success" | "warning"> = {
  Admin: "destructive",
  Manager: "warning",
  Technician: "success",
  Employee: "secondary",
};

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
      toast.success(`Chào mừng ${response.user.fullName}! Đang mở dashboard.`);
      navigate(appRoutes.dashboard, { replace: true });
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && [401, 403].includes(error.response?.status ?? 0)) {
        toast.error("Email hoặc mật khẩu không đúng.");
        return;
      }
      toast.error("Không thể đăng nhập. Vui lòng thử lại.");
    }
  };

  const features = [
    { icon: Wrench, title: "Quản lý Ticket", desc: "Tạo và theo dõi yêu cầu bảo trì theo thời gian thực" },
    { icon: Shield, title: "Phân quyền vai trò", desc: "Admin, Manager, Technician, Employee đầy đủ" },
    { icon: Bell, title: "Thông báo & Lịch sử", desc: "Comment và lịch sử thay đổi trạng thái ticket" },
    { icon: BarChart3, title: "Dashboard phân tích", desc: "Biểu đồ trực quan về tình trạng thiết bị & ticket" },
  ];

  return (
    <div className="min-h-screen w-full flex">
      {/* Left panel - Hero */}
      <div className="hidden lg:flex lg:w-3/5 flex-col justify-between bg-gradient-to-br from-slate-900 via-blue-950 to-slate-900 p-12 relative overflow-hidden">
        {/* Background decorations */}
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-blue-600/20 via-transparent to-transparent" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-blue-500/5 rounded-full blur-3xl" />

        {/* Brand */}
        <div className="relative z-10">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center font-bold text-white text-sm">
              IM
            </div>
            <div>
              <p className="text-white font-semibold text-sm">Internal Maintenance</p>
              <p className="text-blue-300 text-xs">Management System</p>
            </div>
          </div>
        </div>

        {/* Headline */}
        <div className="relative z-10 space-y-6">
          <div>
            <p className="text-blue-400 text-xs font-semibold tracking-widest uppercase mb-3">
              Office Facility Maintenance
            </p>
            <h1 className="text-4xl font-bold text-white leading-tight">
              Điều phối bảo trì
              <br />
              <span className="text-blue-400">nội bộ, gọn, rõ,</span>
              <br />
              dễ mở rộng.
            </h1>
          </div>
          <p className="text-slate-400 text-sm leading-relaxed max-w-md">
            Quản lý thiết bị, phân công kỹ thuật viên, và theo dõi tiến độ xử lý — tất cả trong một
            hệ thống thống nhất.
          </p>

          {/* Feature grid */}
          <div className="grid grid-cols-2 gap-3">
            {features.map(({ icon: Icon, title, desc }) => (
              <div
                key={title}
                className="bg-white/5 border border-white/10 rounded-xl p-4 backdrop-blur-sm hover:bg-white/8 transition-colors"
              >
                <Icon className="h-4 w-4 text-blue-400 mb-2" />
                <p className="text-white text-xs font-semibold">{title}</p>
                <p className="text-slate-400 text-xs mt-1 leading-relaxed">{desc}</p>
              </div>
            ))}
          </div>
        </div>

        <p className="relative z-10 text-slate-600 text-xs">© 2025 Internal Maintenance System</p>
      </div>

      {/* Right panel - Login form */}
      <div className="flex-1 flex flex-col items-center justify-center p-8 bg-background">
        <div className="w-full max-w-md space-y-6">
          {/* Mobile logo */}
          <div className="flex lg:hidden items-center gap-2 mb-2">
            <div className="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center font-bold text-white text-xs">
              IM
            </div>
            <span className="font-semibold text-sm">Internal Maintenance</span>
          </div>

          {/* Form header */}
          <div>
            <h2 className="text-2xl font-bold tracking-tight">Đăng nhập</h2>
            <p className="text-muted-foreground text-sm mt-1">
              Sử dụng tài khoản được cấp để truy cập hệ thống.
            </p>
          </div>

          {/* Quick logins — dev only */}
          {isDev && (
            <Card className="border-dashed">
              <CardHeader className="pb-3">
                <CardTitle className="text-xs font-semibold text-muted-foreground tracking-widest uppercase">
                  Quick Login (Dev mode)
                </CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-2 gap-2 pt-0">
                {wireframeData.quickLogins.map((item) => (
                  <button
                    key={item.email}
                    type="button"
                    onClick={() => {
                      setValue("email", item.email, { shouldValidate: true });
                      setValue("password", item.password, { shouldValidate: true });
                    }}
                    className="text-left p-2.5 rounded-lg border hover:bg-accent hover:border-primary/30 transition-all group"
                  >
                    <div className="flex items-center justify-between gap-1 mb-1">
                      <span className="text-xs font-semibold group-hover:text-primary transition-colors truncate">
                        {item.label}
                      </span>
                      <Badge variant={ROLE_BADGE[item.role] ?? "secondary"} className="text-[10px] px-1.5 shrink-0">
                        {item.role}
                      </Badge>
                    </div>
                    <p className="text-[11px] text-muted-foreground truncate">{item.email}</p>
                  </button>
                ))}
              </CardContent>
            </Card>
          )}

          {/* Login form */}
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" placeholder="example@company.com" {...register("email")} />
              {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="password">Mật khẩu</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  placeholder="••••••••"
                  className="pr-10"
                  {...register("password")}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
            </div>

            <Button
              type="submit"
              className={cn("w-full", (isSubmitting || loginMutation.isPending) && "opacity-80")}
              disabled={isSubmitting || loginMutation.isPending}
            >
              {isSubmitting || loginMutation.isPending ? (
                <span className="flex items-center gap-2">
                  <span className="h-4 w-4 rounded-full border-2 border-white/30 border-t-white animate-spin" />
                  Đang đăng nhập...
                </span>
              ) : (
                "Đăng nhập"
              )}
            </Button>
          </form>

          <p className="text-center text-xs text-muted-foreground">
            Bạn cần hỗ trợ? Liên hệ{" "}
            <span className="text-primary font-medium">bộ phận IT</span> để được cấp tài khoản.
          </p>
        </div>
      </div>
    </div>
  );
}
