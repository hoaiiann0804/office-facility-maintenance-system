import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import { useCreateUserMutation, useUpdateUserMutation } from "../api/use-user-mutations";
import { useDepartmentsQuery } from "../../equipment/api/use-departments-query";
import type { User } from "../../../entities/user/model/types";

type Props = {
  user: User | null; // null if creating
  isOpen: boolean;
  onClose: () => void;
};

const ROLES = [
  { id: 1, name: "Admin" },
  { id: 2, name: "Manager" },
  { id: 3, name: "Staff" },
  { id: 4, name: "Technician" },
];

export function UserModal({ user, isOpen, onClose }: Props) {
  const isEdit = !!user;
  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [roleId, setRoleId] = useState<number | "">(user?.roleId ?? "");
  const [departmentId, setDepartmentId] = useState<number | "">(user?.departmentId ?? "");
  const [temporaryPassword, setTemporaryPassword] = useState("");

  const { data: deptsPage, isLoading: isDeptsLoading } = useDepartmentsQuery({ pageSize: 100 });
  const createMutation = useCreateUserMutation();
  const updateMutation = useUpdateUserMutation(user?.id ?? 0);

  if (!isOpen) return null;


  const departments = deptsPage?.items ?? [];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || roleId === "") {
      toast.error("Vui lòng điền họ tên và vai trò.");
      return;
    }

    if (!isEdit && (!email.trim() || !temporaryPassword.trim())) {
      toast.error("Vui lòng điền email và mật khẩu tạm thời khi tạo mới.");
      return;
    }

    try {
      if (isEdit) {
        await updateMutation.mutateAsync({
          fullName: fullName.trim(),
          roleId: Number(roleId),
          departmentId: departmentId === "" ? null : Number(departmentId),
        });
        toast.success("Cập nhật người dùng thành công!");
      } else {
        await createMutation.mutateAsync({
          fullName: fullName.trim(),
          email: email.trim(),
          temporaryPassword: temporaryPassword.trim(),
          roleId: Number(roleId),
          departmentId: departmentId === "" ? null : Number(departmentId),
        });
        toast.success("Thêm người dùng mới thành công!");
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
        <h2>{isEdit ? `Sửa thông tin: ${user.email}` : "Thêm Nhân Viên Mới"}</h2>
        <p className="section-lead">
          Nhập các thông tin cơ bản để cấu hình tài khoản truy cập hệ thống.
        </p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Họ và tên</span>
            <input
              type="text"
              className="input"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Ví dụ: Nguyễn Văn A"
              required
              disabled={isPending}
            />
          </label>

          <label className="field">
            <span>Địa chỉ Email</span>
            <input
              type="email"
              className="input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="email@example.com"
              required
              disabled={isEdit || isPending} // Không được chỉnh sửa email sau khi tạo
            />
          </label>

          {!isEdit && (
            <label className="field">
              <span>Mật khẩu tạm thời</span>
              <input
                type="password"
                className="input"
                value={temporaryPassword}
                onChange={(e) => setTemporaryPassword(e.target.value)}
                placeholder="Ít nhất 8 ký tự"
                required
                disabled={isPending}
              />
            </label>
          )}

          <label className="field">
            <span>Vai trò (Role)</span>
            <select
              className="select"
              value={roleId}
              onChange={(e) => setRoleId(e.target.value ? Number(e.target.value) : "")}
              required
              disabled={isPending}
            >
              <option value="">-- Chọn vai trò --</option>
              {ROLES.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </label>

          <label className="field">
            <span>Phòng ban</span>
            <select
              className="select"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value ? Number(e.target.value) : "")}
              disabled={isDeptsLoading || isPending}
            >
              <option value="">-- Không trực thuộc / Hệ thống --</option>
              {departments.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
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
