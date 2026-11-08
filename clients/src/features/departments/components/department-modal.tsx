import { useState } from "react";
import { toast } from "sonner";
import {
  useCreateDepartmentMutation,
  useUpdateDepartmentMutation,
} from "../api/use-department-mutations";
import type { Department } from "../../../entities/department/model/types";
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
  department: Department | null; // null if creating
  isOpen: boolean;
  onClose: () => void;
};

export function DepartmentModal({ department, isOpen, onClose }: Props) {
  const isEdit = !!department;
  const [name, setName] = useState(department?.name ?? "");
  const [description, setDescription] = useState(department?.description ?? "");
  const [isMaintenanceTeam, setIsMaintenanceTeam] = useState(
    department?.isMaintenanceTeam ?? false,
  );

  const [prevDepartment, setPrevDepartment] = useState(department);
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (department !== prevDepartment || isOpen !== prevIsOpen) {
    setPrevDepartment(department);
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setName(department?.name ?? "");
      setDescription(department?.description ?? "");
      setIsMaintenanceTeam(department?.isMaintenanceTeam ?? false);
    }
  }

  const createMutation = useCreateDepartmentMutation();
  const updateMutation = useUpdateDepartmentMutation(department?.id ?? 0);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      toast.error("Vui lòng nhập tên phòng ban.");
      return;
    }

    const payload = {
      name: name.trim(),
      description: description.trim() || null,
      isMaintenanceTeam,
    };

    try {
      if (isEdit) {
        await updateMutation.mutateAsync(payload);
        toast.success("Cập nhật phòng ban thành công!");
      } else {
        await createMutation.mutateAsync(payload);
        toast.success("Thêm phòng ban thành công!");
      }
      onClose();
    } catch (error: unknown) {
      console.error("Failed to save department:", error);
      toast.error(getFriendlyErrorMessage(error, "Lưu thông tin phòng ban thất bại."));
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? `Chỉnh sửa: ${department.name}` : "Thêm Phòng Ban Mới"}
          </DialogTitle>
          <DialogDescription>Nhập thông tin cơ bản của phòng ban trong tổ chức.</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="dept-name">
              Tên phòng ban <span className="text-destructive">*</span>
            </Label>
            <Input
              id="dept-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ví dụ: Phòng Kỹ Thuật"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="dept-desc">Mô tả</Label>
            <Textarea
              id="dept-desc"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả chức năng, nhiệm vụ của phòng ban..."
              rows={3}
              disabled={isPending}
            />
          </div>

          <div className="flex items-center space-x-2.5 pt-1">
            <input
              id="dept-maint-team"
              type="checkbox"
              checked={isMaintenanceTeam}
              onChange={(e) => setIsMaintenanceTeam(e.target.checked)}
              disabled={isPending}
              className="h-4 w-4 rounded border-input text-primary focus:ring-primary accent-primary cursor-pointer"
            />
            <Label
              htmlFor="dept-maint-team"
              className="text-xs font-semibold cursor-pointer leading-tight"
            >
              Là bộ phận bảo trì (Sẽ xuất hiện trong danh sách Phòng ban bảo trì thiết bị)
            </Label>
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
