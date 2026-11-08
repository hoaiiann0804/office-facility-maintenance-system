import { toast } from "sonner";
import {
  KeyRound,
  LogOut,
  ShieldCheck,
  Briefcase,
  Wrench,
  User,
  Check,
  Sparkles,
} from "lucide-react";
import { ThemeToggle } from "./theme-toggle";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useSwitchRoleMutation } from "@/features/auth/api/use-switch-role-mutation";
import { cn } from "@/lib/utils";

type UserProfileProps = {
  fullName: string | undefined;
  roleName: string | undefined;
  onLogout: () => void;
  onChangePassword: () => void;
};

const DEMO_ROLES = [
  {
    name: "Admin",
    label: "Admin",
    icon: ShieldCheck,
    color: "text-rose-500",
    desc: "Toàn quyền quản trị",
  },
  {
    name: "Manager",
    label: "Manager",
    icon: Briefcase,
    color: "text-amber-500",
    desc: "Trưởng phòng / Điều phối",
  },
  {
    name: "Technician",
    label: "Kỹ thuật viên",
    icon: Wrench,
    color: "text-emerald-500",
    desc: "Xử lý & Sửa chữa",
  },
  {
    name: "Staff",
    label: "Nhân viên",
    icon: User,
    color: "text-blue-500",
    desc: "Báo hỏng thiết bị",
  },
];

function getInitials(name: string | undefined) {
  if (!name) return "?";
  const parts = name.trim().split(" ");
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return name.charAt(0).toUpperCase();
}

export function UserProfile({ fullName, roleName, onLogout, onChangePassword }: UserProfileProps) {
  const switchMutation = useSwitchRoleMutation();

  const handleRoleChange = async (targetRole: string) => {
    if (targetRole === roleName || switchMutation.isPending) return;
    try {
      await switchMutation.mutateAsync(targetRole);
      toast.success(`Đã chuyển sang vai trò: ${targetRole}`);
    } catch (error) {
      console.error("Role switch failed:", error);
      toast.error("Không thể chuyển vai trò. Vui lòng thử lại.");
    }
  };

  return (
    <div className="flex items-center gap-2 sm:gap-3">
      <ThemeToggle />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            className="relative flex items-center gap-2.5 px-2 py-1.5 h-auto hover:bg-accent rounded-full sm:rounded-xl border border-transparent hover:border-border transition-all cursor-pointer"
          >
            <Avatar className="h-8 w-8 bg-primary/10 text-primary border border-primary/20 shadow-2xs">
              <AvatarFallback className="font-semibold text-xs text-primary bg-primary/10">
                {getInitials(fullName)}
              </AvatarFallback>
            </Avatar>
            <div className="hidden sm:flex flex-col text-left">
              <span className="text-xs font-semibold text-foreground leading-tight truncate max-w-[140px]">
                {fullName ?? "Guest"}
              </span>
              <span className="text-[10px] text-muted-foreground leading-tight flex items-center gap-1 mt-0.5">
                <span className="inline-block w-1.5 h-1.5 rounded-full bg-emerald-500" />
                {roleName ?? "User"}
              </span>
            </div>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-64 p-1.5 shadow-lg">
          <DropdownMenuLabel className="font-normal px-2 py-1.5">
            <div className="flex flex-col space-y-0.5">
              <p className="text-sm font-semibold leading-none text-foreground">{fullName}</p>
              <p className="text-xs text-muted-foreground">{roleName}</p>
            </div>
          </DropdownMenuLabel>

          <DropdownMenuSeparator />

          {/* Demo Role Switcher Section */}
          <div className="px-1 py-1">
            <div className="flex items-center gap-1.5 px-1.5 py-1 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider mb-1">
              <Sparkles className="h-3 w-3 text-primary" />
              <span>Chuyển vai trò (Demo)</span>
            </div>
            <div className="grid grid-cols-1 gap-0.5">
              {DEMO_ROLES.map((role) => {
                const Icon = role.icon;
                const isActive = roleName === role.name;

                return (
                  <DropdownMenuItem
                    key={role.name}
                    onClick={() => handleRoleChange(role.name)}
                    disabled={switchMutation.isPending}
                    className={cn(
                      "flex items-center justify-between px-2.5 py-1.5 rounded-lg text-xs cursor-pointer transition-colors",
                      isActive
                        ? "bg-primary/10 text-primary font-semibold"
                        : "hover:bg-accent text-foreground",
                    )}
                  >
                    <div className="flex items-center gap-2">
                      <Icon className={cn("h-4 w-4", role.color)} />
                      <span>{role.label}</span>
                    </div>
                    {isActive && <Check className="h-3.5 w-3.5 text-primary stroke-[2.5]" />}
                  </DropdownMenuItem>
                );
              })}
            </div>
          </div>

          <DropdownMenuSeparator />

          <DropdownMenuItem
            onClick={onChangePassword}
            className="cursor-pointer px-2.5 py-1.5 rounded-lg text-xs"
          >
            <KeyRound className="mr-2 h-4 w-4 text-muted-foreground" />
            <span>Đổi mật khẩu</span>
          </DropdownMenuItem>

          <DropdownMenuSeparator />

          <DropdownMenuItem
            onClick={onLogout}
            className="cursor-pointer px-2.5 py-1.5 rounded-lg text-xs text-destructive focus:text-destructive focus:bg-destructive/10"
          >
            <LogOut className="mr-2 h-4 w-4" />
            <span>Đăng xuất</span>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
