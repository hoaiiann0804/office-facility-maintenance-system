import { useState } from "react";
import { toast } from "sonner";
import { useChangePasswordMutation } from "../api/use-change-password-mutation";
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
import { Label } from "@/components/ui/label";

type Props = {
  isOpen: boolean;
  onClose: () => void;
};

export function ChangePasswordModal({ isOpen, onClose }: Props) {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (isOpen !== prevIsOpen) {
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    }
  }

  const changePasswordMutation = useChangePasswordMutation();

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
      console.error("Change password failed:", error);
      toast.error(getFriendlyErrorMessage(error, "Thay đổi mật khẩu thất bại."));
    }
  };

  const isPending = changePasswordMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Đổi Mật Khẩu Cá Nhân</DialogTitle>
          <DialogDescription>
            Nhập mật khẩu hiện tại và thiết lập mật khẩu mới cho tài khoản của bạn.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="change-current-pass">
              Mật khẩu hiện tại <span className="text-destructive">*</span>
            </Label>
            <Input
              id="change-current-pass"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              placeholder="Nhập mật khẩu hiện tại"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="change-new-pass">
              Mật khẩu mới <span className="text-destructive">*</span>
            </Label>
            <Input
              id="change-new-pass"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Ít nhất 8 ký tự"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="change-confirm-pass">
              Xác nhận mật khẩu mới <span className="text-destructive">*</span>
            </Label>
            <Input
              id="change-confirm-pass"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="Nhập lại mật khẩu mới"
              required
              disabled={isPending}
            />
          </div>

          <DialogFooter className="pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
              Hủy
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Đang xử lý..." : "Lưu thay đổi"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
