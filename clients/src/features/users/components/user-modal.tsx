import { useState } from "react";
import { toast } from "sonner";
import { useCreateUserMutation, useUpdateUserMutation } from "../api/use-user-mutations";
import { useDepartmentsQuery } from "../../equipment/api/use-departments-query";
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

  const [prevUser, setPrevUser] = useState(user);
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);

  if (user !== prevUser || isOpen !== prevIsOpen) {
    setPrevUser(user);
    setPrevIsOpen(isOpen);
    if (isOpen) {
      setFullName(user?.fullName ?? "");
      setEmail(user?.email ?? "");
      setRoleId(user?.roleId ?? "");
      setDepartmentId(user?.departmentId ?? "");
      setTemporaryPassword("");
    }
  }

  const { data: deptsPage, isLoading: isDeptsLoading } = useDepartmentsQuery({ pageSize: 100 });
  const createMutation = useCreateUserMutation();
  const updateMutation = useUpdateUserMutation(user?.id ?? 0);

  const departments = deptsPage?.items ?? [];
  const availableRoles =
    isEdit && user?.roleName === "Admin" ? ROLES : ROLES.filter((role) => role.name !== "Admin");

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
      console.error("Failed to save user:", error);
      toast.error(getFriendlyErrorMessage(error, "Lưu thông tin người dùng thất bại."));
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? `Sửa thông tin: ${user.email}` : "Thêm Nhân Viên Mới"}
          </DialogTitle>
          <DialogDescription>
            Nhập các thông tin cơ bản để cấu hình tài khoản truy cập hệ thống.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="user-fullname">
              Họ và tên <span className="text-destructive">*</span>
            </Label>
            <Input
              id="user-fullname"
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Ví dụ: Nguyễn Văn A"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="user-email">
              Địa chỉ Email <span className="text-destructive">*</span>
            </Label>
            <Input
              id="user-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="email@example.com"
              required
              disabled={isEdit || isPending}
            />
          </div>

          {!isEdit && (
            <div className="space-y-1.5">
              <Label htmlFor="user-temp-pass">
                Mật khẩu tạm thời <span className="text-destructive">*</span>
              </Label>
              <Input
                id="user-temp-pass"
                type="password"
                value={temporaryPassword}
                onChange={(e) => setTemporaryPassword(e.target.value)}
                placeholder="Ít nhất 8 ký tự"
                required
                disabled={isPending}
              />
            </div>
          )}

          <div className="space-y-1.5">
            <Label htmlFor="user-role">
              Vai trò (Role) <span className="text-destructive">*</span>
            </Label>
            <select
              id="user-role"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={roleId}
              onChange={(e) => setRoleId(e.target.value ? Number(e.target.value) : "")}
              required
              disabled={isPending}
            >
              <option value="" className="bg-background text-foreground">
                -- Chọn vai trò --
              </option>
              {availableRoles.map((r) => (
                <option key={r.id} value={r.id} className="bg-background text-foreground">
                  {r.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="user-dept">Phòng ban</Label>
            <select
              id="user-dept"
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value ? Number(e.target.value) : "")}
              disabled={isDeptsLoading || isPending}
            >
              <option value="" className="bg-background text-foreground">
                -- Không trực thuộc / Hệ thống --
              </option>
              {departments.map((d) => (
                <option key={d.id} value={d.id} className="bg-background text-foreground">
                  {d.name}
                </option>
              ))}
            </select>
          </div>

          <DialogFooter className="pt-4">
            <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
              Hủy
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Đang lưu..." : isEdit ? "Lưu thay đổi" : "Tạo mới"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
