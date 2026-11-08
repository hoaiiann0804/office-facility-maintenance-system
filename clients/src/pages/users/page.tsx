import { useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { useUsersQuery } from "../../features/tickets/api/use-users-query";
import { useUpdateUserActiveMutation } from "../../features/users/api/use-user-mutations";
import { UserModal } from "../../features/users/components/user-modal";
import { ResetPasswordModal } from "../../features/users/components/reset-password-modal";
import type { User } from "../../entities/user/model/types";
import type { RoleName } from "../../entities/auth/model/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  Search,
  UserPlus,
  Pencil,
  KeyRound,
  Lock,
  Unlock,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
  Building2,
  Clock,
  Mail,
} from "lucide-react";

import { formatDateTime } from "@/shared/lib/date-utils";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";

function getInitials(name: string | undefined) {
  if (!name) return "?";
  const parts = name.trim().split(" ");
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return name.charAt(0).toUpperCase();
}

export function UsersPage() {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const isAdmin = role === "Admin";

  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [isUserModalOpen, setIsUserModalOpen] = useState(false);
  const [isResetOpen, setIsResetOpen] = useState(false);

  const {
    data: usersPage,
    isLoading,
    isError,
  } = useUsersQuery({
    keyword: search.trim() || undefined,
    role: (roleFilter === "" ? undefined : roleFilter) as RoleName | undefined,
    page,
    pageSize,
  });

  const toggleActiveMutation = useUpdateUserActiveMutation();

  const handleToggleStatus = async (id: number, currentActive: boolean) => {
    const actionText = currentActive ? "khóa" : "kích hoạt";
    if (!window.confirm(`Bạn có chắc chắn muốn ${actionText} tài khoản này không?`)) return;

    try {
      await toggleActiveMutation.mutateAsync({ id, isActive: !currentActive });
      toast.success(`Đã ${actionText} tài khoản thành công!`);
    } catch (error: unknown) {
      console.error("Failed to toggle user active status:", error);
      toast.error(getFriendlyErrorMessage(error, `Thao tác ${actionText} tài khoản thất bại.`));
    }
  };

  const usersList = usersPage?.items ?? [];
  const totalPages = usersPage?.totalPages ?? 1;

  if (!isAdmin) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] p-8 text-center">
        <ShieldAlert className="h-12 w-12 text-destructive mb-3" />
        <h2 className="text-lg font-bold text-foreground">Không có quyền truy cập</h2>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm">
          Trang web này chỉ dành cho người quản trị hệ thống (Admin).
        </p>
      </div>
    );
  }

  const getRoleBadge = (roleName: string) => {
    switch (roleName) {
      case "Admin":
        return <Badge variant="default">{roleName}</Badge>;
      case "Manager":
        return <Badge variant="warning">{roleName}</Badge>;
      case "Technician":
        return <Badge variant="success">{roleName}</Badge>;
      default:
        return <Badge variant="secondary">{roleName}</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Nhân sự
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Quản lý nhân viên
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Quản lý danh sách tài khoản, phân quyền vai trò và phòng ban trong hệ thống.
          </p>
        </div>
        <Button
          onClick={() => {
            setSelectedUser(null);
            setIsUserModalOpen(true);
          }}
          className="gap-1.5 shrink-0"
        >
          <UserPlus className="h-4 w-4" />
          <span>Thêm Nhân Viên</span>
        </Button>
      </div>

      {/* Main Content Card */}
      <Card>
        <CardHeader className="pb-4 border-b">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <CardTitle className="text-base font-semibold">
                Danh sách tài khoản hệ thống
              </CardTitle>
              <CardDescription className="text-xs">
                Hiển thị trang {page} / {totalPages} (Tổng cộng {usersPage?.totalItems ?? 0} nhân
                viên)
              </CardDescription>
            </div>

            {/* Filter Bar */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 min-w-0 md:w-auto">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  type="text"
                  value={search}
                  placeholder="Tìm tên hoặc email..."
                  onChange={(e) => {
                    setSearch(e.target.value);
                    setPage(1);
                  }}
                  className="pl-8 text-xs h-9"
                />
              </div>

              <div>
                <select
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  value={roleFilter}
                  onChange={(e) => {
                    setRoleFilter(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="" className="bg-background text-foreground">
                    Tất cả vai trò
                  </option>
                  <option value="Admin" className="bg-background text-foreground">
                    Admin
                  </option>
                  <option value="Manager" className="bg-background text-foreground">
                    Manager
                  </option>
                  <option value="Staff" className="bg-background text-foreground">
                    Staff
                  </option>
                  <option value="Technician" className="bg-background text-foreground">
                    Technician
                  </option>
                </select>
              </div>
            </div>
          </div>
        </CardHeader>

        <CardContent className="p-0 overflow-x-auto">
          {isLoading ? (
            <div className="py-16 flex justify-center">
              <div className="h-7 w-7 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
            </div>
          ) : isError ? (
            <div className="p-12 text-center text-xs text-destructive">
              Đã xảy ra lỗi. Không thể tải danh sách nhân viên.
            </div>
          ) : usersList.length > 0 ? (
            <>
              <table className="w-full text-left text-xs">
                <thead className="bg-muted/50 border-b text-muted-foreground uppercase text-[10px] font-semibold tracking-wider">
                  <tr>
                    <th className="p-3.5 pl-6">Họ và tên</th>
                    <th className="p-3.5">Email</th>
                    <th className="p-3.5">Vai trò</th>
                    <th className="p-3.5">Phòng ban</th>
                    <th className="p-3.5">Trạng thái</th>
                    <th className="p-3.5">Lần đăng nhập cuối</th>
                    <th className="p-3.5 pr-6 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {usersList.map((u) => (
                    <tr key={u.id} className="hover:bg-muted/30 transition-colors">
                      <td className="p-3.5 pl-6">
                        <div className="flex items-center gap-2.5">
                          <Avatar className="h-8 w-8 text-xs shrink-0 border">
                            <AvatarFallback className="font-semibold text-xs bg-primary/10 text-primary">
                              {getInitials(u.fullName)}
                            </AvatarFallback>
                          </Avatar>
                          <span className="font-semibold text-foreground">{u.fullName}</span>
                        </div>
                      </td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Mail className="h-3 w-3 shrink-0" />
                          {u.email}
                        </span>
                      </td>
                      <td className="p-3.5">{getRoleBadge(u.roleName)}</td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Building2 className="h-3 w-3 shrink-0" />
                          {u.departmentName || "Hệ thống"}
                        </span>
                      </td>
                      <td className="p-3.5">
                        <Badge variant={u.isActive ? "success" : "destructive"}>
                          {u.isActive ? "Đang hoạt động" : "Bị khóa"}
                        </Badge>
                      </td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3 shrink-0" />
                          {formatDateTime(u.lastLoginAt)}
                        </span>
                      </td>
                      <td className="p-3.5 pr-6 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              setSelectedUser(u);
                              setIsUserModalOpen(true);
                            }}
                            className="h-8 px-2 text-xs gap-1"
                            title="Sửa thông tin"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                            <span className="hidden sm:inline">Sửa</span>
                          </Button>

                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              setSelectedUser(u);
                              setIsResetOpen(true);
                            }}
                            className="h-8 px-2 text-xs gap-1"
                            title="Đặt lại mật khẩu"
                          >
                            <KeyRound className="h-3.5 w-3.5 text-amber-500" />
                            <span className="hidden sm:inline">Đặt lại</span>
                          </Button>

                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleToggleStatus(u.id, u.isActive)}
                            disabled={toggleActiveMutation.isPending}
                            className={`h-8 px-2 text-xs gap-1 ${
                              u.isActive
                                ? "text-destructive hover:text-destructive hover:bg-destructive/10"
                                : "text-emerald-600 hover:text-emerald-600 hover:bg-emerald-500/10"
                            }`}
                            title={u.isActive ? "Khóa tài khoản" : "Kích hoạt tài khoản"}
                          >
                            {u.isActive ? (
                              <Lock className="h-3.5 w-3.5" />
                            ) : (
                              <Unlock className="h-3.5 w-3.5" />
                            )}
                            <span className="hidden sm:inline">{u.isActive ? "Khóa" : "Mở"}</span>
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Pagination Controls */}
              <div className="flex items-center justify-between p-4 border-t">
                <p className="text-xs text-muted-foreground">
                  Trang <strong className="text-foreground">{page}</strong> trên{" "}
                  <strong className="text-foreground">{totalPages}</strong>
                </p>

                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page === 1}
                    onClick={() => setPage((p) => p - 1)}
                    className="h-8 gap-1 text-xs"
                  >
                    <ChevronLeft className="h-3.5 w-3.5" />
                    <span>Trước</span>
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page === totalPages}
                    onClick={() => setPage((p) => p + 1)}
                    className="h-8 gap-1 text-xs"
                  >
                    <span>Sau</span>
                    <ChevronRight className="h-3.5 w-3.5" />
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <div className="py-16 text-center text-xs text-muted-foreground space-y-1">
              <p className="font-semibold text-foreground">Không tìm thấy nhân viên</p>
              <p>Hãy thử thay đổi điều kiện tìm kiếm hoặc thêm nhân viên mới.</p>
            </div>
          )}
        </CardContent>
      </Card>

      <UserModal
        user={selectedUser}
        isOpen={isUserModalOpen}
        onClose={() => {
          setIsUserModalOpen(false);
          setSelectedUser(null);
        }}
      />

      <ResetPasswordModal
        user={selectedUser}
        isOpen={isResetOpen}
        onClose={() => {
          setIsResetOpen(false);
          setSelectedUser(null);
        }}
      />
    </div>
  );
}
