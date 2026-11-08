import { useState } from "react";
import { toast } from "sonner";
import {
  useCreateEquipmentMutation,
  useUpdateEquipmentMutation,
} from "../api/use-equipment-mutations";
import { useDepartmentsQuery } from "../api/use-departments-query";
import type { Equipment, EquipmentStatus } from "../../../entities/equipment/model/types";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";

type Props = {
  equipment: Equipment | null; // null if creating
  isOpen: boolean;
  onClose: () => void;
};

const getTodayDateString = () => {
  const d = new Date();
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

export function EquipmentModal({ equipment, isOpen, onClose }: Props) {
  const isEdit = !!equipment;
  const [code, setCode] = useState(equipment?.code ?? "");
  const [name, setName] = useState(equipment?.name ?? "");
  const [departmentId, setDepartmentId] = useState<number | "">(equipment?.departmentId ?? "");
  const [maintenanceDepartmentId, setMaintenanceDepartmentId] = useState<number | "">(
    equipment?.maintenanceDepartmentId ?? "",
  );
  const [status, setStatus] = useState<EquipmentStatus>(equipment?.status ?? "Active");
  const [purchasedDate, setPurchasedDate] = useState(
    equipment?.purchasedDate ? equipment.purchasedDate.split("T")[0] : getTodayDateString(),
  );
  const [description, setDescription] = useState(equipment?.description ?? "");

  const [prevEquipment, setPrevEquipment] = useState(equipment);
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (equipment !== prevEquipment || isOpen !== prevIsOpen) {
    setPrevEquipment(equipment);
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setCode(equipment?.code ?? "");
      setName(equipment?.name ?? "");
      setDepartmentId(equipment?.departmentId ?? "");
      setMaintenanceDepartmentId(equipment?.maintenanceDepartmentId ?? "");
      setStatus(equipment?.status ?? "Active");
      setPurchasedDate(
        equipment?.purchasedDate ? equipment.purchasedDate.split("T")[0] : getTodayDateString(),
      );
      setDescription(equipment?.description ?? "");
    }
  }

  const { data: deptsPage, isLoading: isDeptsLoading } = useDepartmentsQuery({ pageSize: 100 });
  const { data: maintenanceDeptsPage, isLoading: isMaintenanceDeptsLoading } = useDepartmentsQuery({
    pageSize: 100,
    isMaintenanceTeam: true,
  });

  const createMutation = useCreateEquipmentMutation();
  const updateMutation = useUpdateEquipmentMutation(equipment?.id ?? 0);

  const departments = deptsPage?.items ?? [];
  const maintenanceDepartments = maintenanceDeptsPage?.items ?? [];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim() || departmentId === "") {
      toast.error("Vui lòng nhập đầy đủ mã, tên thiết bị và chọn phòng ban.");
      return;
    }

    const payload = {
      code: code.trim(),
      name: name.trim(),
      departmentId: Number(departmentId),
      maintenanceDepartmentId: maintenanceDepartmentId
        ? Number(maintenanceDepartmentId)
        : undefined,
      status,
      purchasedDate: purchasedDate || null,
      description: description.trim() || null,
    };

    try {
      if (isEdit) {
        await updateMutation.mutateAsync(payload);
        toast.success("Cập nhật thiết bị thành công!");
      } else {
        await createMutation.mutateAsync(payload);
        toast.success("Thêm thiết bị thành công!");
      }
      onClose();
    } catch (error: unknown) {
      console.error("Failed to save equipment:", error);
      toast.error(getFriendlyErrorMessage(error, "Lưu thông tin thiết bị thất bại."));
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? `Chỉnh sửa thiết bị: ${equipment.code}` : "Thêm Thiết Bị Mới"}
          </DialogTitle>
          <DialogDescription>
            Nhập thông tin cơ bản của thiết bị để quản lý và tạo ticket bảo trì.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="eq-code">
              Mã thiết bị <span className="text-destructive">*</span>
            </Label>
            <Input
              id="eq-code"
              type="text"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="Ví dụ: LAP-IT-001"
              required
              disabled={isEdit || isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-name">
              Tên thiết bị <span className="text-destructive">*</span>
            </Label>
            <Input
              id="eq-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ví dụ: Laptop Dell Vostro 5402"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-dept">
              Phòng ban sở hữu <span className="text-destructive">*</span>
            </Label>
            {/* Leave blank to mean "self-maintained" / no dedicated maintenance department */}
            <select
              id="eq-dept"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value ? Number(e.target.value) : "")}
              required
              disabled={isDeptsLoading || isPending}
            >
              <option value="" className="bg-background text-foreground">
                -- Chọn phòng ban --
              </option>
              {departments.map((d) => (
                <option key={d.id} value={d.id} className="bg-background text-foreground">
                  {d.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-maint-dept">Phòng ban bảo trì (Tùy chọn)</Label>
            <select
              id="eq-maint-dept"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={maintenanceDepartmentId}
              onChange={(e) =>
                setMaintenanceDepartmentId(e.target.value ? Number(e.target.value) : "")
              }
              disabled={isMaintenanceDeptsLoading || isPending}
            >
              <option value="" className="bg-background text-foreground">
                -- Mặc định (Tự bảo trì) --
              </option>
              {maintenanceDepartments.map((d) => (
                <option key={d.id} value={d.id} className="bg-background text-foreground">
                  {d.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-status">Trạng thái</Label>
            <select
              id="eq-status"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={status}
              onChange={(e) => setStatus(e.target.value as EquipmentStatus)}
              disabled={isPending}
            >
              <option value="Active" className="bg-background text-foreground">
                Hoạt động (Active)
              </option>
              <option value="Inactive" className="bg-background text-foreground">
                Ngưng hoạt động (Inactive)
              </option>
              {isEdit && (
                <option value="Retired" className="bg-background text-foreground">
                  Thanh lý (Retired)
                </option>
              )}
              {isEdit && equipment?.status === "UnderMaintenance" && (
                <option value="UnderMaintenance" className="bg-background text-foreground" disabled>
                  Đang bảo trì (UnderMaintenance - Chỉ đổi qua Ticket)
                </option>
              )}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-date">Ngày mua</Label>
            <Input
              id="eq-date"
              type="date"
              value={purchasedDate}
              onChange={(e) => setPurchasedDate(e.target.value)}
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="eq-desc">Mô tả chi tiết</Label>
            <Textarea
              id="eq-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả cấu hình, vị trí đặt, v.v."
              rows={3}
              disabled={isPending}
            />
          </div>

          <DialogFooter className="pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
              Hủy
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Đang lưu..." : isEdit ? "Lưu thay đổi" : "Tạo mới"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
