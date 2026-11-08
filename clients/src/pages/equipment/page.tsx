import { useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../../features/auth/model/auth-store";
import { useEquipmentQuery } from "../../features/tickets/api/use-equipment-query";
import { useDepartmentsQuery } from "../../features/equipment/api/use-departments-query";
import { useDeleteEquipmentMutation } from "../../features/equipment/api/use-equipment-mutations";
import { EquipmentModal } from "../../features/equipment/components/equipment-modal";
import type { Equipment, EquipmentStatus } from "../../entities/equipment/model/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Search,
  Plus,
  Wrench,
  Pencil,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Calendar,
  Building2,
} from "lucide-react";

import { formatDateOnly } from "@/shared/lib/date-utils";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";

export function EquipmentPage() {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const isAdmin = role === "Admin";
  const canEdit = isAdmin;

  const [search, setSearch] = useState("");
  const [deptFilter, setDeptFilter] = useState<number | "">("");
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const [selectedEq, setSelectedEq] = useState<Equipment | null>(null);
  const [isEqModalOpen, setIsEqModalOpen] = useState(false);

  const { data: deptsPage } = useDepartmentsQuery({ pageSize: 100 });
  const {
    data: eqPage,
    isLoading,
    isError,
  } = useEquipmentQuery({
    keyword: search.trim() || undefined,
    departmentId: deptFilter === "" ? undefined : deptFilter,
    status: (statusFilter === "" ? undefined : statusFilter) as EquipmentStatus | undefined,
    page,
    pageSize,
  });

  const deleteMutation = useDeleteEquipmentMutation();

  const handleDelete = async (id: number) => {
    if (!window.confirm("Bạn có chắc chắn muốn xóa thiết bị này không?")) return;
    try {
      await deleteMutation.mutateAsync(id);
      toast.success("Xóa thiết bị thành công!");
    } catch (error: unknown) {
      console.error("Failed to delete equipment:", error);
      toast.error(getFriendlyErrorMessage(error, "Xóa thiết bị thất bại."));
    }
  };

  const departments = deptsPage?.items ?? [];
  const equipmentList = eqPage?.items ?? [];
  const totalPages = eqPage?.totalPages ?? 1;

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Active":
        return <Badge variant="success">Hoạt động</Badge>;
      case "UnderMaintenance":
        return <Badge variant="warning">Đang bảo trì</Badge>;
      case "Retired":
        return <Badge variant="destructive">Thanh lý</Badge>;
      case "Inactive":
      default:
        return <Badge variant="secondary">Ngưng hoạt động</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Thiết bị
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Danh sách thiết bị
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Quản lý thông tin thiết bị văn phòng, vị trí phòng ban và lịch sử tình trạng hoạt động.
          </p>
        </div>
        {canEdit && (
          <Button
            onClick={() => {
              setSelectedEq(null);
              setIsEqModalOpen(true);
            }}
            className="gap-1.5 shrink-0"
          >
            <Plus className="h-4 w-4" />
            <span>Thêm Thiết Bị</span>
          </Button>
        )}
      </div>

      {/* Main Content Card */}
      <Card>
        <CardHeader className="pb-4 border-b">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <CardTitle className="text-base font-semibold">Danh sách thiết bị hiện có</CardTitle>
              <CardDescription className="text-xs">
                Hiển thị trang {page} / {totalPages} (Tổng cộng {eqPage?.totalItems ?? 0} thiết bị)
              </CardDescription>
            </div>

            {/* Filter Bar */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 min-w-0 md:w-auto">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  type="text"
                  value={search}
                  placeholder="Mã, tên thiết bị..."
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
                  value={deptFilter}
                  onChange={(e) => {
                    setDeptFilter(e.target.value ? Number(e.target.value) : "");
                    setPage(1);
                  }}
                >
                  <option value="">Tất cả phòng ban</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id} className="bg-background text-foreground">
                      {d.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <select
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  value={statusFilter}
                  onChange={(e) => {
                    setStatusFilter(e.target.value);
                    setPage(1);
                  }}
                >
                  <option value="" className="bg-background text-foreground">
                    Tất cả trạng thái
                  </option>
                  <option value="Active" className="bg-background text-foreground">
                    Hoạt động (Active)
                  </option>
                  <option value="Inactive" className="bg-background text-foreground">
                    Ngưng hoạt động (Inactive)
                  </option>
                  <option value="UnderMaintenance" className="bg-background text-foreground">
                    Đang bảo trì (UnderMaintenance)
                  </option>
                  <option value="Retired" className="bg-background text-foreground">
                    Thanh lý (Retired)
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
              Đã xảy ra lỗi. Không thể tải danh sách thiết bị.
            </div>
          ) : equipmentList.length > 0 ? (
            <>
              <table className="w-full text-left text-xs">
                <thead className="bg-muted/50 border-b text-muted-foreground uppercase text-[10px] font-semibold tracking-wider">
                  <tr>
                    <th className="p-3.5 pl-6">Mã thiết bị</th>
                    <th className="p-3.5">Tên thiết bị</th>
                    <th className="p-3.5">Phòng ban</th>
                    <th className="p-3.5">Trạng thái</th>
                    <th className="p-3.5">Ngày mua</th>
                    <th className="p-3.5">Mô tả</th>
                    {canEdit && <th className="p-3.5 pr-6 text-right">Thao tác</th>}
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {equipmentList.map((eq) => (
                    <tr key={eq.id} className="hover:bg-muted/30 transition-colors">
                      <td className="p-3.5 pl-6 font-mono font-bold text-primary">{eq.code}</td>
                      <td className="p-3.5 font-semibold text-foreground">{eq.name}</td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Building2 className="h-3 w-3 text-muted-foreground shrink-0" />
                          {eq.departmentName}
                        </span>
                      </td>
                      <td className="p-3.5">{getStatusBadge(eq.status)}</td>
                      <td className="p-3.5 text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Calendar className="h-3 w-3 text-muted-foreground shrink-0" />
                          {formatDateOnly(eq.purchasedDate)}
                        </span>
                      </td>
                      <td className="p-3.5 text-muted-foreground max-w-xs truncate">
                        {eq.description || "N/A"}
                      </td>
                      {canEdit && (
                        <td className="p-3.5 pr-6 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                setSelectedEq(eq);
                                setIsEqModalOpen(true);
                              }}
                              className="h-8 px-2.5 text-xs gap-1"
                            >
                              <Pencil className="h-3.5 w-3.5" />
                              <span>Sửa</span>
                            </Button>

                            {isAdmin && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleDelete(eq.id)}
                                disabled={deleteMutation.isPending}
                                className="h-8 px-2.5 text-xs gap-1 text-destructive hover:text-destructive hover:bg-destructive/10"
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                                <span>Xóa</span>
                              </Button>
                            )}
                          </div>
                        </td>
                      )}
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
              <Wrench className="h-8 w-8 text-muted-foreground/50 mx-auto mb-2" />
              <p className="font-semibold text-foreground">Không tìm thấy thiết bị</p>
              <p>Hãy thử thay đổi điều kiện tìm kiếm hoặc thêm thiết bị mới.</p>
            </div>
          )}
        </CardContent>
      </Card>

      <EquipmentModal
        equipment={selectedEq}
        isOpen={isEqModalOpen}
        onClose={() => {
          setIsEqModalOpen(false);
          setSelectedEq(null);
        }}
      />
    </div>
  );
}
