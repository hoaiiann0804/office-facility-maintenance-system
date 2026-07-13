import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import { useUpdateTicketMutation } from "../api/use-update-ticket-mutation";
import type { MaintenanceTicket, TicketPriority } from "../../../entities/ticket/model/types";

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

  const updateTicketMutation = useUpdateTicketMutation(ticket?.id ?? null);

  if (!isOpen || !ticket) return null;

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
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Không thể cập nhật ticket.");
      } else {
        toast.error("Không thể cập nhật ticket.");
      }
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <div className="modal-header">
          <div>
            <span className="eyebrow">Chỉnh sửa</span>
            <h2>{ticket.ticketCode}</h2>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Tiêu đề</span>
            <input
              type="text"
              className="input"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Nhập tiêu đề ticket"
              required
            />
          </label>

          <label className="field">
            <span>Mô tả chi tiết</span>
            <textarea
              className="textarea"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả rõ tình trạng..."
              required
              rows={4}
            />
          </label>

          <label className="field">
            <span>Mức độ ưu tiên</span>
            <select
              className="select"
              value={priority}
              onChange={(e) => setPriority(e.target.value as TicketPriority)}
            >
              {PRIORITIES.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>

          {/* Thông tin không thể chỉnh sửa — hiển thị để tham khảo */}
          <div className="mini-card">
            <strong>Thiết bị</strong>
            <span>
              {ticket.equipmentCode} — {ticket.equipmentName}
            </span>
          </div>

          <div className="button-row spaced">
            <button
              type="submit"
              className="button primary"
              disabled={updateTicketMutation.isPending}
            >
              {updateTicketMutation.isPending ? "Đang lưu..." : "Lưu thay đổi"}
            </button>
            <button
              type="button"
              className="button secondary"
              onClick={onClose}
              disabled={updateTicketMutation.isPending}
            >
              Hủy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
