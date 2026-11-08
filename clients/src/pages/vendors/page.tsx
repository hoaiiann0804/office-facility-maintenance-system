import { useState } from "react";
import { toast } from "sonner";
import { useAuthStore } from "@/features/auth/model/auth-store";
import { useVendors, useToggleVendorActiveMutation } from "@/features/vendors/api/vendors-api";
import { VendorModal } from "@/features/vendors/components/vendor-modal";
import type { Vendor } from "@/entities/vendor/model/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Search,
  Plus,
  Store,
  Pencil,
  Power,
  ShieldAlert,
  Phone,
  Mail,
  MapPin,
  User,
  Calendar,
} from "lucide-react";
import { formatDateTime } from "@/shared/lib/date-utils";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";

export function VendorsPage() {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const canManage = role === "Admin" || role === "Manager";

  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<"All" | "Active" | "Inactive">("All");

  const [selectedVendor, setSelectedVendor] = useState<Vendor | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const { data: vendors = [], isLoading, isError } = useVendors();
  const toggleActiveMutation = useToggleVendorActiveMutation();

  const handleToggleActive = async (vendor: Vendor) => {
    const action = vendor.isActive ? "tắt (ngưng dùng)" : "bật (kích hoạt lại)";
    const actionText = vendor.isActive ? "tắt" : "bật";
    if (!window.confirm(`Bạn có chắc muốn ${action} đối tác "${vendor.name}" không?`)) return;

    try {
      await toggleActiveMutation.mutateAsync(vendor.id);
      toast.success(`Đã ${actionText} đối tác thành công!`);
    } catch (error: unknown) {
      console.error("Failed to toggle vendor active state:", error);
      toast.error(getFriendlyErrorMessage(error, `Thao tác ${actionText} đối tác thất bại.`));
    }
  };

  const filteredVendors = vendors.filter((v) => {
    const matchesSearch =
      !search.trim() ||
      v.name.toLowerCase().includes(search.toLowerCase()) ||
      (v.contactPerson && v.contactPerson.toLowerCase().includes(search.toLowerCase())) ||
      (v.phone && v.phone.includes(search)) ||
      (v.email && v.email.toLowerCase().includes(search.toLowerCase()));

    const matchesStatus =
      activeFilter === "All" ||
      (activeFilter === "Active" && v.isActive) ||
      (activeFilter === "Inactive" && !v.isActive);

    return matchesSearch && matchesStatus;
  });

  if (!canManage) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] p-8 text-center">
        <ShieldAlert className="h-12 w-12 text-destructive mb-3" />
        <h2 className="text-lg font-bold text-foreground">Không có quyền truy cập</h2>
        <p className="text-xs text-muted-foreground mt-1 max-w-sm">
          Trang web này chỉ dành cho người quản trị (Admin) và quản lý (Manager).
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
            Danh bạ đối tác
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Quản lý đối tác bảo hành ngoài (Vendors)
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Danh bạ các trung tâm bảo hành, hãng sản xuất và đơn vị sửa chữa dịch vụ bên ngoài.
          </p>
        </div>
        <Button
          onClick={() => {
            setSelectedVendor(null);
            setIsModalOpen(true);
          }}
          className="gap-1.5 shrink-0"
        >
          <Plus className="h-4 w-4" />
          <span>Thêm Đối Tác</span>
        </Button>
      </div>

      {/* Main Content Card */}
      <Card>
        <CardHeader className="pb-4 border-b">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <CardTitle className="text-base font-semibold">
                Danh sách đơn vị xử lý ngoài
              </CardTitle>
              <CardDescription className="text-xs">
                Hiển thị {filteredVendors.length} / {vendors.length} đối tác
              </CardDescription>
            </div>

            {/* Search & Filter Bar */}
            <div className="flex items-center gap-3 w-full sm:w-auto">
              <div className="relative flex-1 sm:w-64">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
                <Input
                  type="text"
                  value={search}
                  placeholder="Tìm tên, SĐT, người liên hệ..."
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8 text-xs h-9"
                />
              </div>

              <select
                className="flex h-9 rounded-md border border-input bg-background px-3 text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                value={activeFilter}
                onChange={(e) => setActiveFilter(e.target.value as "All" | "Active" | "Inactive")}
              >
                <option value="All">Tất cả trạng thái</option>
                <option value="Active">Đang hoạt động</option>
                <option value="Inactive">Ngưng hoạt động</option>
              </select>
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
              Đã xảy ra lỗi. Không thể tải danh sách đối tác.
            </div>
          ) : filteredVendors.length > 0 ? (
            <table className="w-full text-left text-xs">
              <thead className="bg-muted/50 border-b text-muted-foreground uppercase text-[10px] font-semibold tracking-wider">
                <tr>
                  <th className="p-3.5 pl-6">Tên đơn vị đối tác</th>
                  <th className="p-3.5">Người liên hệ & SĐT</th>
                  <th className="p-3.5">Email & Địa chỉ</th>
                  <th className="p-3.5">Trạng thái</th>
                  <th className="p-3.5">Ngày tạo</th>
                  <th className="p-3.5 pr-6 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {filteredVendors.map((v) => (
                  <tr key={v.id} className="hover:bg-muted/30 transition-colors">
                    <td className="p-3.5 pl-6 font-bold text-foreground">
                      <span className="flex items-center gap-2">
                        <Store className="h-4 w-4 text-primary shrink-0" />
                        <span>{v.name}</span>
                      </span>
                    </td>

                    <td className="p-3.5 space-y-0.5">
                      {v.contactPerson && (
                        <div className="flex items-center gap-1 font-medium text-foreground">
                          <User className="h-3 w-3 text-muted-foreground shrink-0" />
                          <span>{v.contactPerson}</span>
                        </div>
                      )}
                      {v.phone ? (
                        <div className="flex items-center gap-1 text-muted-foreground">
                          <Phone className="h-3 w-3 text-muted-foreground shrink-0" />
                          <a
                            href={`tel:${v.phone}`}
                            className="hover:underline text-primary font-mono"
                          >
                            {v.phone}
                          </a>
                        </div>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </td>

                    <td className="p-3.5 space-y-0.5 max-w-xs">
                      {v.email && (
                        <div className="flex items-center gap-1 text-muted-foreground truncate">
                          <Mail className="h-3 w-3 text-muted-foreground shrink-0" />
                          <span>{v.email}</span>
                        </div>
                      )}
                      {v.address ? (
                        <div className="flex items-center gap-1 text-muted-foreground truncate">
                          <MapPin className="h-3 w-3 text-muted-foreground shrink-0" />
                          <span className="truncate">{v.address}</span>
                        </div>
                      ) : (
                        <span className="text-muted-foreground">—</span>
                      )}
                    </td>

                    <td className="p-3.5">
                      {v.isActive ? (
                        <Badge
                          variant="success"
                          className="bg-emerald-500/15 text-emerald-700 dark:text-emerald-400 border-emerald-500/30"
                        >
                          Hoạt động
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="text-muted-foreground">
                          Ngưng hoạt động
                        </Badge>
                      )}
                    </td>

                    <td className="p-3.5 text-muted-foreground">
                      <span className="flex items-center gap-1 text-[11px]">
                        <Calendar className="h-3 w-3 shrink-0" />
                        {formatDateTime(v.createdAt)}
                      </span>
                    </td>

                    <td className="p-3.5 pr-6 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setSelectedVendor(v);
                            setIsModalOpen(true);
                          }}
                          className="h-8 px-2.5 text-xs gap-1"
                        >
                          <Pencil className="h-3.5 w-3.5" />
                          <span>Sửa</span>
                        </Button>

                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleToggleActive(v)}
                          disabled={toggleActiveMutation.isPending}
                          className={`h-8 px-2.5 text-xs gap-1 ${
                            v.isActive
                              ? "text-amber-600 hover:text-amber-700 hover:bg-amber-500/10 dark:text-amber-400"
                              : "text-emerald-600 hover:text-emerald-700 hover:bg-emerald-500/10 dark:text-emerald-400"
                          }`}
                        >
                          <Power className="h-3.5 w-3.5" />
                          <span>{v.isActive ? "Tắt" : "Bật"}</span>
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="py-16 text-center text-xs text-muted-foreground space-y-1">
              <Store className="h-8 w-8 text-muted-foreground/50 mx-auto mb-2" />
              <p className="font-semibold text-foreground">Không tìm thấy đối tác nào</p>
              <p>Hãy thử tìm kiếm với từ khóa khác hoặc thêm mới đối tác bảo hành.</p>
            </div>
          )}
        </CardContent>
      </Card>

      <VendorModal
        vendor={selectedVendor}
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setSelectedVendor(null);
        }}
      />
    </div>
  );
}
