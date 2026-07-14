import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import { useChangePasswordMutation } from "../api/use-change-password-mutation";

type Props = {
  isOpen: boolean;
  onClose: () => void;
};

export function ChangePasswordModal({ isOpen, onClose }: Props) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const changePasswordMutation = useChangePasswordMutation();

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentPassword || !newPassword || !confirmPassword) {
      toast.error("Vui lòng nhập đầy đủ tất cả các trường.");
      return;
    }
    if (newPassword !== confirmPassword) {
      toast.error("Mật khẩu mới và mật khẩu xác nhận không khớp.");
      return;
    }
    if (newPassword.length < 8) {
      toast.error("Mật khẩu mới phải có ít nhất 8 ký tự.");
      return;
    }

    try {
      await changePasswordMutation.mutateAsync({
        currentPassword,
        newPassword,
      });
      toast.success("Thay đổi mật khẩu thành công!");
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      onClose();
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.message ?? error.response?.data;
        toast.error(typeof msg === "string" ? msg : "Thay đổi mật khẩu thất bại.");
      } else {
        toast.error("Thay đổi mật khẩu thất bại.");
      }
    }
  };

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>Đổi Mật Khẩu Cá Nhân</h2>
        <p className="section-lead">Nhập mật khẩu cũ và thiết lập mật khẩu mới.</p>

        <form onSubmit={handleSubmit} className="stack spaced">
          <label className="field">
            <span>Mật khẩu cũ</span>
            <input
              type="password"
              className="input"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              placeholder="Nhập mật khẩu hiện tại"
              required
              disabled={changePasswordMutation.isPending}
            />
          </label>

          <label className="field">
            <span>Mật khẩu mới</span>
            <input
              type="password"
              className="input"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Mật khẩu phải từ 8 ký tự"
              required
              disabled={changePasswordMutation.isPending}
            />
          </label>

          <label className="field">
            <span>Xác nhận mật khẩu mới</span>
            <input
              type="password"
              className="input"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="Nhập lại mật khẩu mới"
              required
              disabled={changePasswordMutation.isPending}
            />
          </label>

          <div className="button-row spaced">
            <button
              type="submit"
              className="button primary"
              disabled={changePasswordMutation.isPending}
            >
              {changePasswordMutation.isPending ? "Đang xử lý..." : "Lưu thay đổi"}
            </button>
            <button
              type="button"
              className="button secondary"
              onClick={onClose}
              disabled={changePasswordMutation.isPending}
            >
              Hủy
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
