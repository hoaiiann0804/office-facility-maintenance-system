import { useState } from "react";
import { toast } from "sonner";
import type { Vendor } from "@/entities/vendor/model/types";
import { useCreateVendorMutation, useUpdateVendorMutation } from "../api/vendors-api";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Loader2 } from "lucide-react";

interface Props {
  vendor: Vendor | null;
  isOpen: boolean;
  onClose: () => void;
}

export function VendorModal({ vendor, isOpen, onClose }: Props) {
  const isEditing = !!vendor;

  const [name, setName] = useState("");
  const [contactPerson, setContactPerson] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [address, setAddress] = useState("");
  const [isActive, setIsActive] = useState(true);

  const [prevIsOpen, setPrevIsOpen] = useState(isOpen);
  const [prevVendorId, setPrevVendorId] = useState(vendor?.id);

  if (isOpen !== prevIsOpen || vendor?.id !== prevVendorId) {
    setPrevIsOpen(isOpen);
    setPrevVendorId(vendor?.id);
    if (vendor) {
      setName(vendor.name);
      setContactPerson(vendor.contactPerson ?? "");
      setPhone(vendor.phone ?? "");
      setEmail(vendor.email ?? "");
      setAddress(vendor.address ?? "");
      setIsActive(vendor.isActive);
    } else {
      setName("");
      setContactPerson("");
      setPhone("");
      setEmail("");
      setAddress("");
      setIsActive(true);
    }
  }

  const createMutation = useCreateVendorMutation();
  const updateMutation = useUpdateVendorMutation();

  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (
      !name.trim() ||
      !contactPerson.trim() ||
      !phone.trim() ||
      !email.trim() ||
      !address.trim()
    ) {
      toast.error("Vui lòng nhập đầy đủ các trường bắt buộc.");
      return;
    }

    try {
      if (isEditing && vendor) {
        await updateMutation.mutateAsync({
          id: vendor.id,
          payload: {
            name: name.trim(),
            contactPerson: contactPerson.trim() || undefined,
            phone: phone.trim() || undefined,
            email: email.trim() || undefined,
            address: address.trim() || undefined,
            isActive,
          },
        });
        toast.success("Cập nhật đối tác thành công!");
      } else {
        await createMutation.mutateAsync({
          name: name.trim(),
          contactPerson: contactPerson.trim() || undefined,
          phone: phone.trim() || undefined,
          email: email.trim() || undefined,
          address: address.trim() || undefined,
        });
        toast.success("Thêm đối tác mới thành công!");
      }
      onClose();
    } catch (error: unknown) {
      console.error("Failed to save vendor:", error);
      toast.error(getFriendlyErrorMessage(error, "Lưu thông tin đối tác thất bại."));
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? "Chỉnh sửa thông tin đối tác" : "Thêm đối tác bảo hành mới"}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? "Cập nhật chi tiết liên hệ và trạng thái hoạt động của đối tác ngoài."
              : "Nhập thông tin đơn vị ngoài để lưu vào danh bạ bảo hành."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2">
          <div className="space-y-2">
            <Label className="text-xs font-medium">
              Tên đơn vị / Hãng bảo hành <span className="text-destructive">*</span>
            </Label>
            <Input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="VD: Daikin Việt Nam, Dell Service..."
              required
              disabled={isPending}
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label className="text-xs font-medium">
                Người liên hệ <span className="text-destructive">*</span>
              </Label>
              <Input
                type="text"
                value={contactPerson}
                onChange={(e) => setContactPerson(e.target.value)}
                placeholder="VD: Nguyễn Văn A"
                required
                disabled={isPending}
              />
            </div>

            <div className="space-y-2">
              <Label className="text-xs font-medium">
                Số điện thoại <span className="text-destructive">*</span>
              </Label>
              <Input
                type="text"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="VD: 18006777"
                required
                disabled={isPending}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-xs font-medium">
              Email liên hệ <span className="text-destructive">*</span>
            </Label>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="VD: support@daikin.com.vn"
              required
              disabled={isPending}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-xs font-medium">
              Địa chỉ trung tâm bảo hành <span className="text-destructive">*</span>
            </Label>
            <Textarea
              rows={2}
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="VD: 184 Cao Thắng, Quận 10, TP.HCM..."
              required
              disabled={isPending}
            />
          </div>

          {isEditing && (
            <div className="flex items-center gap-2 pt-2">
              <input
                type="checkbox"
                id="is-active-checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                disabled={isPending}
              />
              <Label htmlFor="is-active-checkbox" className="text-xs cursor-pointer">
                Đang hoạt động (Hiển thị trong danh sách chọn ticket)
              </Label>
            </div>
          )}

          <DialogFooter className="gap-2 sm:gap-0 pt-2">
            <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
              Hủy bỏ
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
              {isEditing ? "Lưu thay đổi" : "Thêm mới"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
