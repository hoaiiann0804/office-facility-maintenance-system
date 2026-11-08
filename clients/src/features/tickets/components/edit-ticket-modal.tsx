import { useState } from "react";
import { toast } from "sonner";
import { useUpdateTicketMutation } from "../api/use-update-ticket-mutation";
import type { MaintenanceTicket, TicketPriority } from "../../../entities/ticket/model/types";
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

type Props = {
  ticket: MaintenanceTicket | null;
  isOpen: boolean;
  onClose: () => void;
};

export function EditTicketModal({ ticket, isOpen, onClose }: Props) {
  const [title, setTitle] = useState(ticket?.title ?? "");
  const [description, setDescription] = useState(ticket?.description ?? "");
  const [priority, setPriority] = useState<TicketPriority>(ticket?.priority ?? "Medium");

  const [prevTicket, setPrevTicket] = useState(ticket);
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (ticket !== prevTicket || isOpen !== prevIsOpen) {
    setPrevTicket(ticket);
    setPrevIsOpen(isOpen);
    if (ticket && isOpen) {
      setTitle(ticket.title ?? "");
      setDescription(ticket.description ?? "");
      setPriority(ticket.priority ?? "Medium");
    } else if (!isOpen) {
      setTitle("");
      setDescription("");
      setPriority("Medium");
    }
  }

  const updateTicketMutation = useUpdateTicketMutation(ticket?.id ?? null);

  if (!ticket) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) {
      toast.error("Tiêu đề và mô tả không được để trống.");
      return;
    }

    try {
      await updateTicketMutation.mutateAsync({ title, description, priority });
      toast.success("Cập nhật ticket thành công!");
      onClose();
    } catch (error: unknown) {
      console.error("Update ticket failed:", error);
      toast.error(getFriendlyErrorMessage(error, "Không thể cập nhật ticket."));
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Chỉnh sửa Ticket</DialogTitle>
          <DialogDescription>Mã ticket: {ticket.ticketCode}</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="edit-title">
              Tiêu đề <span className="text-destructive">*</span>
            </Label>
            <Input
              id="edit-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Nhập tiêu đề ticket"
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="edit-description">
              Mô tả chi tiết <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="edit-description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả rõ tình trạng..."
              required
              rows={4}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="edit-priority">Mức độ ưu tiên</Label>
            <select
              id="edit-priority"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={priority}
              onChange={(e) => setPriority(e.target.value as TicketPriority)}
            >
              {PRIORITIES.map((p) => (
                <option key={p} value={p} className="bg-background text-foreground">
                  {p}
                </option>
              ))}
            </select>
          </div>

          <div className="rounded-lg border bg-muted/40 p-3 text-xs space-y-1">
            <p className="font-semibold text-muted-foreground">Thiết bị</p>
            <p className="font-medium text-foreground">
              {ticket.equipmentCode} — {ticket.equipmentName}
            </p>
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={updateTicketMutation.isPending}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={updateTicketMutation.isPending}>
              {updateTicketMutation.isPending ? "Đang lưu..." : "Lưu thay đổi"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
