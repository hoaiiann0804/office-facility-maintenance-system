import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import { useResetUserPasswordMutation } from "../api/use-user-mutations";
import type { User } from "../../../entities/user/model/types";

type Props = {
  user: User | null;
  isOpen: boolean;
  onClose: () => void;
};

export function ResetPasswordModal({ user, isOpen, onClose }: Props) {
  const [temporaryPassword, setTemporaryPassword] = useState("");
  const resetMutation = useResetUserPasswordMutation(user?.id ?? 0);

  if (!isOpen || !user) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (temporaryPassword.length < 8) {
      toast.error("Mật khẩu tạm thời phải có ít nhất 8 ký tự.");
      return;
    }

    try {
      await resetMutation.mutateAsync({
        temporaryPassword: temporaryPassword.trim(),
      });
      toast.success(`Đặt lại mật khẩu cho ${user.email} thành công!`);
      setTemporaryPassword("");
      onClose();
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Đặt lại mật khẩu thất bại.");
      } else {
        toast.error("Đặt lại mật khẩu thất bại.");
      }
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>Đặt lại mật khẩu</h2>
        <p className="section-lead">
          Nhập mật khẩu tạm thời mới cho tài khoản{" "}
          <strong>
            {user.fullName} ({user.email})
          </strong>
          .
        </p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Mật khẩu tạm thời mới</span>
            <input
              type="password"
              className="input"
              value={temporaryPassword}
              onChange={(e) => setTemporaryPassword(e.target.value)}
              placeholder="Nhập mật khẩu từ 8 ký tự trở lên"
              required
              disabled={resetMutation.isPending}
            />
          </label>

          <div className="button-row spaced">
            <button type="submit" className="button primary" disabled={resetMutation.isPending}>
              {resetMutation.isPending ? "Đang xử lý..." : "Reset mật khẩu"}
            </button>
            <button
              type="button"
              className="button secondary"
              onClick={onClose}
              disabled={resetMutation.isPending}
            >
              Hủy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
