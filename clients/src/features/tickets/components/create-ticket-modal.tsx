import { useState } from "react";
import { toast } from "sonner";
import { useCreateTicketMutation } from "../api/use-create-ticket-mutation";
import { useEquipmentQuery } from "../api/use-equipment-query";
import type { TicketPriority } from "../../../entities/ticket/model/types";
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

const PRIORITIES: TicketPriority[] = ["Low", "Medium", "High", "Critical"];

export function CreateTicketModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [equipmentId, setEquipmentId] = useState<number | "">("");
  const [priority, setPriority] = useState<TicketPriority | "">("");

  const createTicketMutation = useCreateTicketMutation();
  // Chỉ fetch khi modal đang mở để tránh gọi API thừa
  const { data: equipmentPage, isLoading: isEquipmentLoading } = useEquipmentQuery(
    isOpen ? { pageSize: 500 } : {},
  );

  // Only show equipment that is active and has a dedicated maintenance department.
  // Self-managed equipment (maintenanceDepartmentId = null) is intentionally excluded.
  const availableEquipment = (equipmentPage?.items ?? []).filter(
    (eq) => eq.status !== "Retired" && eq.maintenanceDepartmentId !== null,
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim() || equipmentId === "") {
      toast.error("Vui lòng điền đầy đủ tiêu đề, mô tả và chọn thiết bị.");
      return;
    }

    try {
      await createTicketMutation.mutateAsync({
        title,
        description,
        equipmentId: Number(equipmentId),
        priority: priority === "" ? undefined : priority,
      });
      toast.success("Tạo ticket thành công!");

      // Reset form and close
      setTitle("");
      setDescription("");
      setEquipmentId("");
      setPriority("");
      onClose();
    } catch (error: unknown) {
      console.error("Create ticket failed:", error);
      toast.error(getFriendlyErrorMessage(error, "Không thể tạo ticket."));
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Tạo Ticket Mới</DialogTitle>
          <DialogDescription>
            Điền thông tin để báo cáo sự cố hoặc yêu cầu bảo trì thiết bị.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="create-title">
              Tiêu đề <span className="text-destructive">*</span>
            </Label>
            <Input
              id="create-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ví dụ: Máy in kẹt giấy"
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-description">
              Mô tả chi tiết <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="create-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả rõ tình trạng sự cố..."
              required
              rows={3}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-equipment">
              Thiết bị <span className="text-destructive">*</span>
            </Label>
            {/* Hide self-managed equipment to keep ticket creation aligned with business rules. */}
            <select
              id="create-equipment"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={equipmentId}
              onChange={(e) => setEquipmentId(e.target.value === "" ? "" : Number(e.target.value))}
              required
              disabled={isEquipmentLoading}
            >
              <option value="" className="bg-background text-foreground">
                {isEquipmentLoading ? "Đang tải danh sách..." : "-- Chọn thiết bị --"}
              </option>
              {availableEquipment.map((eq) => (
                <option key={eq.id} value={eq.id} className="bg-background text-foreground">
                  {eq.code} - {eq.name} ({eq.departmentName})
                </option>
              ))}
            </select>
            <p className="text-[11px] text-muted-foreground">
              Thiết bị tự quản lý không hiển thị ở đây vì không áp dụng tạo phiếu bảo trì.
            </p>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-priority">Mức độ ưu tiên (Tùy chọn)</Label>
            <select
              id="create-priority"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={priority}
              onChange={(e) => setPriority(e.target.value as TicketPriority | "")}
            >
              <option value="" className="bg-background text-foreground">
                -- Mặc định (Trung bình) --
              </option>
              {PRIORITIES.map((p) => (
                <option key={p} value={p} className="bg-background text-foreground">
                  {p}
                </option>
              ))}
            </select>
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={createTicketMutation.isPending}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={createTicketMutation.isPending || isEquipmentLoading}>
              {createTicketMutation.isPending ? "Đang xử lý..." : "Tạo mới"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
