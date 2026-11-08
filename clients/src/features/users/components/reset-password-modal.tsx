import { useState } from "react";
import { toast } from "sonner";
import { useResetUserPasswordMutation } from "../api/use-user-mutations";
import type { User } from "../../../entities/user/model/types";
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
  user: User | null;
  isOpen: boolean;
  onClose: () => void;
};

export function ResetPasswordModal({ user, isOpen, onClose }: Props) {
  const [temporaryPassword, setTemporaryPassword] = useState("");
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (isOpen !== prevIsOpen) {
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setTemporaryPassword("");
    }
  }

  const resetMutation = useResetUserPasswordMutation(user?.id ?? 0);

  if (!user) return null;

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
      console.error("Failed to reset password:", error);
      toast.error(getFriendlyErrorMessage(error, "Đặt lại mật khẩu thất bại."));
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Đặt lại mật khẩu</DialogTitle>
          <DialogDescription>
            Nhập mật khẩu tạm thời mới cho tài khoản{" "}
            <strong className="text-foreground font-semibold">
              {user.fullName} ({user.email})
            </strong>
            .
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="reset-temp-pass">
              Mật khẩu tạm thời mới <span className="text-destructive">*</span>
            </Label>
            <Input
              id="reset-temp-pass"
              type="password"
              value={temporaryPassword}
              onChange={(e) => setTemporaryPassword(e.target.value)}
              placeholder="Nhập mật khẩu từ 8 ký tự trở lên"
              required
              disabled={resetMutation.isPending}
            />
          </div>

          <DialogFooter className="pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={resetMutation.isPending}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={resetMutation.isPending}>
              {resetMutation.isPending ? "Đang xử lý..." : "Đặt lại mật khẩu"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
