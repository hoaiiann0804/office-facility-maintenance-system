import { toast } from "sonner";
import { ShieldCheck, Briefcase, Wrench, User, Sparkles } from "lucide-react";
import { useAuthStore } from "../model/auth-store";
import { useSwitchRoleMutation } from "../api/use-switch-role-mutation";
import { cn } from "@/lib/utils";

const ROLES = [
  {
    name: "Admin",
    label: "Admin",
    icon: ShieldCheck,
    color: "text-rose-500",
    bgActive: "bg-rose-500/15 border-rose-500/40 text-rose-600 dark:text-rose-400",
    desc: "Toàn quyền quản trị & Báo cáo",
  },
  {
    name: "Manager",
    label: "Manager",
    icon: Briefcase,
    color: "text-amber-500",
    bgActive: "bg-amber-500/15 border-amber-500/40 text-amber-600 dark:text-amber-400",
    desc: "Giao việc, điều phối & SLA",
  },
  {
    name: "Technician",
    label: "Kỹ thuật viên",
    icon: Wrench,
    color: "text-emerald-500",
    bgActive: "bg-emerald-500/15 border-emerald-500/40 text-emerald-600 dark:text-emerald-400",
    desc: "Xử lý sự cố & Sửa chữa",
  },
  {
    name: "Staff",
    label: "Nhân viên",
    icon: User,
    color: "text-blue-500",
    bgActive: "bg-blue-500/15 border-blue-500/40 text-blue-600 dark:text-blue-400",
    desc: "Báo hỏng & Quét mã QR",
  },
];

export function DemoRoleSwitcher() {
  const session = useAuthStore((state) => state.session);
  const currentRole = session?.user.roleName ?? "Staff";
  const switchMutation = useSwitchRoleMutation();

  const handleRoleChange = async (roleName: string) => {
    if (roleName === currentRole || switchMutation.isPending) return;

    try {
      await switchMutation.mutateAsync(roleName);
      toast.success(`Đã chuyển sang vai trò: ${roleName}`, {
        description: ROLES.find((r) => r.name === roleName)?.desc,
      });
    } catch (error) {
      console.error("Role switch failed:", error);
      toast.error("Không thể chuyển vai trò. Vui lòng thử lại.");
    }
  };

  return (
    <div className="flex items-center gap-1.5 bg-muted/60 p-1 rounded-xl border border-border/80 shadow-xs">
      <div className="hidden xl:flex items-center gap-1 pl-1.5 pr-1 text-[11px] font-semibold text-muted-foreground uppercase tracking-wider select-none">
        <Sparkles className="h-3.5 w-3.5 text-primary animate-pulse" />
        <span>Demo:</span>
      </div>

      <div className="flex items-center gap-1">
        {ROLES.map((role) => {
          const Icon = role.icon;
          const isActive = currentRole === role.name;

          return (
            <button
              key={role.name}
              type="button"
              onClick={() => handleRoleChange(role.name)}
              disabled={switchMutation.isPending}
              title={`Chuyển sang vai trò ${role.label} - ${role.desc}`}
              className={cn(
                "flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium transition-all cursor-pointer border select-none",
                isActive
                  ? `${role.bgActive} font-bold shadow-xs scale-[1.02]`
                  : "border-transparent text-muted-foreground hover:text-foreground hover:bg-background/80",
                switchMutation.isPending && "opacity-60 cursor-wait",
              )}
            >
              <Icon className={cn("h-3.5 w-3.5", isActive ? "" : role.color)} />
              <span className="hidden md:inline">{role.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
