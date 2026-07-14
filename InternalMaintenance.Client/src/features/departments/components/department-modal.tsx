import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import {
  useCreateDepartmentMutation,
  useUpdateDepartmentMutation,
} from "../api/use-department-mutations";
import type { Department } from "../../../entities/department/model/types";

type Props = {
  department: Department | null; // null if creating
  isOpen: boolean;
  onClose: () => void;
};

export function DepartmentModal({ department, isOpen, onClose }: Props) {
  const isEdit = !!department;
  const [name, setName] = useState(department?.name ?? "");
  const [description, setDescription] = useState(department?.description ?? "");

  const [prevDepartment, setPrevDepartment] = useState(department);
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (department !== prevDepartment || isOpen !== prevIsOpen) {
    setPrevDepartment(department);
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setName(department?.name ?? "");
      setDescription(department?.description ?? "");
    }
  }

  const createMutation = useCreateDepartmentMutation();
  const updateMutation = useUpdateDepartmentMutation(department?.id ?? 0);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      toast.error("Vui lòng nhập tên phòng ban.");
      return;
    }

    const payload = {
      name: name.trim(),
      description: description.trim() || null,
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
        <h2>{isEdit ? `Chỉnh sửa: ${department.name}` : "Thêm Phòng Ban Mới"}</h2>
        <p className="section-lead">Nhập thông tin cơ bản của phòng ban trong tổ chức.</p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Tên phòng ban</span>
            <input
              type="text"
              className="input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ví dụ: Phòng Kỹ Thuật"
              required
              disabled={isPending}
            />
          </label>

          <label className="field">
            <span>Mô tả</span>
            <textarea
              className="textarea"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Mô tả chức năng, nhiệm vụ của phòng ban..."
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
