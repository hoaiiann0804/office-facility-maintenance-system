import { useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { useDepartmentsQuery } from "../../features/equipment/api/use-departments-query";
import { useDeleteDepartmentMutation } from "../../features/departments/api/use-department-mutations";
import { DepartmentModal } from "../../features/departments/components/department-modal";
import type { Department } from "../../entities/department/model/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Search,
  Plus,
  Building2,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  ShieldAlert,
  Wrench,
  Calendar,
} from "lucide-react";

import { formatDateTime } from "@/shared/lib/date-utils";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";

export function DepartmentsPage() {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const isAdmin = role === "Admin";

  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedDept, setSelectedDept] = useState<Department | null>(null);
  const [isDeptModalOpen, setIsDeptModalOpen] = useState(false);

  const {
    data: deptsPage,
    isLoading,
    isError,
  } = useDepartmentsQuery({
    keyword: search.trim() || undefined,
    page,
    pageSize,
  });

  const deleteMutation = useDeleteDepartmentMutation();

  const handleDelete = async (id: number) => {
    if (!window.confirm("Bạn có chắc chắn muốn xóa phòng ban này không?")) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success("Xóa phòng ban thành công!");
    } catch (error: unknown) {
      console.error("Failed to delete department:", error);
      toast.error(getFriendlyErrorMessage(error, "Xóa phòng ban thất bại."));
    }
  };

  const departments = deptsPage?.items ?? [];
  const totalPages = deptsPage?.totalPages ?? 1;

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

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Phòng ban
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Quản lý phòng ban
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Quản lý sơ đồ tổ chức các phòng ban và phân loại bộ phận chuyên trách bảo trì.
          </p>
        </div>
        <Button
          onClick={() => {
            setSelectedDept(null);
            setIsDeptModalOpen(true);
          }}
          className="gap-1.5 shrink-0"
        >
          <Plus className="h-4 w-4" />
          <span>Thêm Phòng Ban</span>
        </Button>
      </div>

      {/* Main Content Card */}
      <Card>
        <CardHeader className="pb-4 border-b">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="text-base font-semibold">Danh sách phòng ban tổ chức</CardTitle>
              <CardDescription className="text-xs">
                Hiển thị trang {page} / {totalPages} (Tổng cộng {deptsPage?.totalItems ?? 0} phòng
                ban)
              </CardDescription>
            </div>

            {/* Filter Bar */}
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                type="text"
                value={search}
                placeholder="Tìm tên phòng ban..."
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
                className="pl-8 text-xs h-9"
              />
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
              Đã xảy ra lỗi. Không thể tải danh sách phòng ban.
            </div>
          ) : departments.length > 0 ? (
            <>
              <table className="w-full text-left text-xs">
                <thead className="bg-muted/50 border-b text-muted-foreground uppercase text-[10px] font-semibold tracking-wider">
                  <tr>
                    <th className="p-3.5 pl-6">Tên phòng ban</th>
                    <th className="p-3.5">Phân loại</th>
                    <th className="p-3.5">Mô tả</th>
                    <th className="p-3.5">Ngày tạo</th>
                    <th className="p-3.5 pr-6 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {departments.map((dept) => (
                    <tr key={dept.id} className="hover:bg-muted/30 transition-colors">
                      <td className="p-3.5 pl-6 font-bold text-foreground">
                        <span className="flex items-center gap-1.5">
                          <Building2 className="h-3.5 w-3.5 text-primary shrink-0" />
                          {dept.name}
                        </span>
                      </td>
                      <td className="p-3.5">
                        {dept.isMaintenanceTeam ? (
                          <Badge variant="default" className="gap-1 bg-primary">
                            <Wrench className="h-3 w-3" />
                            <span>Bộ phận Bảo trì</span>
                          </Badge>
                        ) : (
                          <Badge variant="secondary">Thông thường</Badge>
                        )}
                      </td>
                      <td className="p-3.5 text-muted-foreground max-w-xs truncate">
                        {dept.description || "—"}
                      </td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Calendar className="h-3 w-3 shrink-0" />
                          {formatDateTime(dept.createdAt)}
                        </span>
                      </td>
                      <td className="p-3.5 pr-6 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => {
                              setSelectedDept(dept);
                              setIsDeptModalOpen(true);
                            }}
                            className="h-8 px-2.5 text-xs gap-1"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                            <span>Sửa</span>
                          </Button>

                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(dept.id)}
                            disabled={deleteMutation.isPending}
                            className="h-8 px-2.5 text-xs gap-1 text-destructive hover:text-destructive hover:bg-destructive/10"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                            <span>Xóa</span>
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
                    disabled={page <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="h-8 gap-1 text-xs"
                  >
                    <ChevronLeft className="h-3.5 w-3.5" />
                    <span>Trước</span>
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={page >= totalPages}
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
              <Building2 className="h-8 w-8 text-muted-foreground/50 mx-auto mb-2" />
              <p className="font-semibold text-foreground">Không tìm thấy phòng ban</p>
              <p>Hãy thử thay đổi điều kiện tìm kiếm hoặc thêm phòng ban mới.</p>
            </div>
          )}
        </CardContent>
      </Card>

      <DepartmentModal
        department={selectedDept}
        isOpen={isDeptModalOpen}
        onClose={() => {
          setIsDeptModalOpen(false);
          setSelectedDept(null);
        }}
      />
    </div>
  );
}
