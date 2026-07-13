import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import { wireframeData } from "../../../shared/mock/wireframe-data";
import { useCreateTicketMutation } from "../api/use-create-ticket-mutation";
import type { TicketPriority } from "../../../entities/ticket/model/types";

export function CreateTicketModal({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [equipmentId, setEquipmentId] = useState<number | "">("");
  const [priority, setPriority] = useState<TicketPriority | "">("");

  const createTicketMutation = useCreateTicketMutation();

  if (!isOpen) return null;

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
      if (axios.isAxiosError(error)) {
        toast.error(error.response?.data?.message || "Không thể tạo ticket.");
      } else {
        toast.error("Không thể tạo ticket.");
      }
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>Tạo Ticket Mới</h2>
        <p className="section-lead">
          Điền thông tin để báo cáo sự cố hoặc yêu cầu bảo trì thiết bị.
        </p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Tiêu đề</span>
            <input
              type="text"
              className="input"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ví dụ: Máy in kẹt giấy"
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
              rows={3}
            />
          </label>

          <label className="field">
            <span>Thiết bị</span>
            <select
              className="select"
              value={equipmentId}
              onChange={(e) => setEquipmentId(e.target.value === "" ? "" : Number(e.target.value))}
              required
            >
              <option value="">-- Chọn thiết bị --</option>
              {wireframeData.equipment
                .filter((eq) => eq.status !== "Retired")
                .map((eq) => (
                  <option key={eq.id} value={eq.id}>
                    {eq.code} - {eq.name} ({eq.departmentName})
                  </option>
                ))}
            </select>
          </label>

          <label className="field">
            <span>Mức độ ưu tiên (Tùy chọn)</span>
            <select
              className="select"
              value={priority}
              onChange={(e) => setPriority(e.target.value as TicketPriority | "")}
            >
              <option value="">-- Mặc định (Medium) --</option>
              {wireframeData.priorities.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>

          <div className="button-row spaced">
            <button
              type="submit"
              className="button primary"
              disabled={createTicketMutation.isPending}
            >
              {createTicketMutation.isPending ? "Đang xử lý..." : "Tạo mới"}
            </button>
            <button
              type="button"
              className="button secondary"
              onClick={onClose}
              disabled={createTicketMutation.isPending}
            >
              Hủy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
