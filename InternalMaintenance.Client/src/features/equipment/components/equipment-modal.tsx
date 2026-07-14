import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import {
  useCreateEquipmentMutation,
  useUpdateEquipmentMutation,
} from "../api/use-equipment-mutations";
import { useDepartmentsQuery } from "../api/use-departments-query";
import type { Equipment } from "../../../entities/equipment/model/types";

type Props = {
  equipment: Equipment | null; // null if creating
  isOpen: boolean;
  onClose: () => void;
};

export function EquipmentModal({ equipment, isOpen, onClose }: Props) {
  const isEdit = !!equipment;
  const [code, setCode] = useState(equipment?.code ?? "");
  const [name, setName] = useState(equipment?.name ?? "");
  const [departmentId, setDepartmentId] = useState<number | "">(equipment?.departmentId ?? "");
  const [status, setStatus] = useState(equipment?.status ?? "Active");
  const [purchasedDate, setPurchasedDate] = useState(
    equipment?.purchasedDate ? equipment.purchasedDate.split("T")[0] : "",
  );
  const [description, setDescription] = useState(equipment?.description ?? "");

  const { data: deptsPage, isLoading: isDeptsLoading } = useDepartmentsQuery({ pageSize: 100 });
  const createMutation = useCreateEquipmentMutation();
  const updateMutation = useUpdateEquipmentMutation(equipment?.id ?? 0);

  if (!isOpen) return null;

  const departments = deptsPage?.items ?? [];

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
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Thao tác thất bại.");
      } else {
        toast.error("Thao tác thất bại.");
      }
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>{isEdit ? `Chỉnh sửa thiết bị: ${equipment.code}` : "Thêm Thiết Bị Mới"}</h2>
        <p className="section-lead">
          Nhập thông tin cơ bản của thiết bị để quản lý và tạo ticket bảo trì.
        </p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Mã thiết bị</span>
            <input
              type="text"
              className="input"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="Ví dụ: LAP-IT-001"
              required
              disabled={isEdit || isPending} // Không cho phép sửa mã thiết bị khi cập nhật
            />
          </label>

          <label className="field">
            <span>Tên thiết bị</span>
            <input
              type="text"
              className="input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ví dụ: Laptop Dell Vostro 5402"
              required
              disabled={isPending}
            />
          </label>

          <label className="field">
            <span>Phòng ban sở hữu</span>
            <select
              className="select"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value ? Number(e.target.value) : "")}
              required
              disabled={isDeptsLoading || isPending}
            >
              <option value="">-- Chọn phòng ban --</option>
              {departments.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          </label>

          <label className="field">
            <span>Trạng thái</span>
            <select
              className="select"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              disabled={isPending}
            >
              <option value="Active">Hoạt động (Active)</option>
              <option value="Inactive">Ngưng hoạt động (Inactive)</option>
              {isEdit && <option value="Retired">Thanh lý (Retired)</option>}
              {isEdit && equipment.status === "UnderMaintenance" && (
                <option value="UnderMaintenance" disabled>
                  Đang bảo trì (UnderMaintenance - Chỉ đổi qua Ticket)
                </option>
              )}
            </select>
          </label>

          <label className="field">
            <span>Ngày mua</span>
            <input
              type="date"
              className="input"
              value={purchasedDate}
              onChange={(e) => setPurchasedDate(e.target.value)}
              disabled={isPending}
            />
          </label>

          <label className="field">
            <span>Mô tả chi tiết</span>
            <textarea
              className="textarea"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả cấu hình, vị trí đặt, v.v."
              rows={3}
              disabled={isPending}
            />
          </label>

          <div className="button-row spaced">
            <button type="submit" className="button primary" disabled={isPending}>
              {isPending ? "Đang lưu..." : isEdit ? "Lưu thay đổi" : "Tạo mới"}
            </button>
            <button
              type="button"
              className="button secondary"
              onClick={onClose}
              disabled={isPending}
            >
              Hủy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
