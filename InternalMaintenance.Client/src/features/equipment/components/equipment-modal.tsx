import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import {
  useCreateEquipmentMutation,
  useUpdateEquipmentMutation,
} from "../api/use-equipment-mutations";
import { useDepartmentsQuery } from "../api/use-departments-query";
import type { Equipment, EquipmentStatus } from "../../../entities/equipment/model/types";

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

  if (!isOpen) return null;

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
            <span>Phòng ban bảo trì (Tùy chọn)</span>
            <select
              className="select"
              value={maintenanceDepartmentId}
              onChange={(e) =>
                setMaintenanceDepartmentId(e.target.value ? Number(e.target.value) : "")
              }
              disabled={isMaintenanceDeptsLoading || isPending}
            >
              <option value="">-- Mặc định (Tự bảo trì) --</option>
              {maintenanceDepartments.map((d) => (
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
              onChange={(e) => setStatus(e.target.value as EquipmentStatus)}
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
