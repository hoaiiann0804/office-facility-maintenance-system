import { useState } from "react";
import { toast } from "sonner";
import axios from "axios";
import type { MaintenanceTicketDetail, TicketStatus } from "../../../entities/ticket/model/types";
import type { RoleName } from "../../../entities/auth/model/types";
import type { UserQuery } from "../../../entities/user/model/types";
import { useAuthStore } from "../../auth/model/auth-store";
import { useAssignTicketMutation } from "../api/use-assign-ticket-mutation";
import { useChangeTicketStatusMutation } from "../api/use-change-ticket-status-mutation";
import { useCreateTicketCommentMutation } from "../api/use-create-ticket-comment-mutation";
import { useUsersQuery } from "../api/use-users-query";
import { useTicketAttachmentsQuery } from "../api/use-ticket-attachments-query";
import { useUploadAttachment } from "../api/use-upload-attachment";
import { useVendors } from "../../../features/vendors/api/vendors-api";
import { AttachmentUploadZone } from "./attachment-upload-zone";
import { AttachmentList } from "./attachment-list";
import { TicketCommentsList } from "./ticket-comments-list";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  UserCheck,
  CheckCircle,
  Play,
  XCircle,
  Send,
  Paperclip,
  MessageSquare,
  Loader2,
  AlertTriangle,
} from "lucide-react";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";

function toastApiError(error: unknown, fallback: string) {
  console.error("Ticket action failed:", error);
  toast.error(getFriendlyErrorMessage(error, fallback));
}

type Props = {
  ticket: MaintenanceTicketDetail;
};

export function TicketActionPanel({ ticket }: Props) {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;
  const userId = session?.user.id;
  const departmentId = session?.user.departmentId;

  const [assignTechId, setAssignTechId] = useState<string>(
    ticket.assignedTechnicianId?.toString() ?? "",
  );
  const [supportTechId, setSupportTechId] = useState<string>(
    ticket.supportTechnicianId?.toString() ?? "",
  );
  const [assignNote, setAssignNote] = useState("");
  const [resolutionNote, setResolutionNote] = useState(ticket.resolutionNote ?? "");
  const [statusNote, setStatusNote] = useState("");
  const [commentDraft, setCommentDraft] = useState("");

  const [isVendorDialogOpen, setIsVendorDialogOpen] = useState(false);
  const [vendorId, setVendorId] = useState("");
  const [vendorReturnDate, setVendorReturnDate] = useState("");
  const [vendorNote, setVendorNote] = useState("");

  const [prevTicket, setPrevTicket] = useState(ticket);
  if (ticket !== prevTicket) {
    setPrevTicket(ticket);
    setAssignTechId(ticket.assignedTechnicianId?.toString() ?? "");
    setSupportTechId(ticket.supportTechnicianId?.toString() ?? "");
    setAssignNote("");
    setResolutionNote(ticket.resolutionNote ?? "");
    setStatusNote("");
    setCommentDraft("");
    setVendorId("");
    setVendorReturnDate("");
    setVendorNote("");
  }

  const assignMutation = useAssignTicketMutation(ticket.id);
  const statusMutation = useChangeTicketStatusMutation(ticket.id);
  const commentMutation = useCreateTicketCommentMutation(ticket.id);

  const canAssignTicket =
    role === "Admin" ||
    (role === "Manager" &&
      session?.user.departmentIsMaintenanceTeam &&
      ticket.equipmentMaintenanceDepartmentId === departmentId);

  const technicianQuery: UserQuery =
    role === "Admin"
      ? { role: "Technician", isActive: true, pageSize: 200 }
      : canAssignTicket
        ? {
            role: "Technician",
            isActive: true,
            pageSize: 200,
            departmentId: departmentId ?? undefined,
          }
        : {};

  const { data: techPage, isLoading: isTechLoading } = useUsersQuery(technicianQuery);
  const technicians = techPage?.items ?? [];

  const { data: vendors, isLoading: isVendorsLoading } = useVendors(true);

  const isFinalized = ticket.status === "Closed" || ticket.status === "Cancelled";
  const isAssignedTech = ticket.assignedTechnicianId === userId;
  const isRequester = ticket.createdByUserId === userId;

  const handleAssign = async () => {
    if (!assignTechId) {
      toast.error("Vui lòng chọn kỹ thuật viên.");
      return;
    }
    try {
      await assignMutation.mutateAsync({
        assignedTechnicianId: Number(assignTechId),
        supportTechnicianId: supportTechId ? Number(supportTechId) : null,
        note: assignNote.trim() || undefined,
      });
      toast.success("Giao việc thành công!");
      setAssignNote("");
    } catch (e) {
      toastApiError(e, "Không thể giao việc.");
    }
  };

  const PREDEFINED_CANCEL_REASONS = [
    "Thiết bị đã tự hoạt động bình thường / Đã tự khắc phục",
    "Yêu cầu tạo nhầm hoặc bị trùng lặp ticket khác",
    "Không còn nhu cầu sử dụng / Hủy sự cố",
    "Lý do khác",
  ];

  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);
  const [cancelReasonPreset, setCancelReasonPreset] = useState<string>(
    PREDEFINED_CANCEL_REASONS[0],
  );
  const [customCancelReason, setCustomCancelReason] = useState<string>("");

  const handleConfirmCancel = async () => {
    const finalReason =
      cancelReasonPreset === "Lý do khác" ? customCancelReason.trim() : cancelReasonPreset;

    if (!finalReason) {
      toast.error("Vui lòng chọn hoặc nhập lý do hủy ticket.");
      return;
    }

    try {
      await statusMutation.mutateAsync({
        status: "Cancelled",
        cancellationReason: finalReason,
        note: statusNote.trim() || undefined,
      });
      toast.success("Đã hủy ticket thành công.");
      setIsCancelDialogOpen(false);
      setStatusNote("");
      setCustomCancelReason("");
    } catch (e) {
      toastApiError(e, "Không thể hủy ticket.");
    }
  };

  const handleStatus = async (newStatus: string, requireResolution = false) => {
    if (requireResolution && !resolutionNote.trim()) {
      toast.error("Vui lòng điền ghi chú xử lý trước khi hoàn thành.");
      return;
    }

    if (newStatus === "WaitingForVendor") {
      if (!vendorId || !vendorReturnDate) {
        toast.error("Vui lòng chọn đối tác và ngày dự kiến trả thiết bị.");
        return;
      }
      const selectedDate = new Date(vendorReturnDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (selectedDate < today) {
        toast.error("Ngày dự kiến trả thiết bị không được ở trong quá khứ.");
        return;
      }
    }

    try {
      await statusMutation.mutateAsync({
        status: newStatus as
          "InProgress" | "Resolved" | "Closed" | "Cancelled" | "WaitingForVendor",
        resolutionNote: requireResolution ? resolutionNote.trim() : undefined,
        note: statusNote.trim() || undefined,
        vendorId: newStatus === "WaitingForVendor" ? Number(vendorId) : undefined,
        vendorEstimatedReturnDate:
          newStatus === "WaitingForVendor" ? new Date(vendorReturnDate).toISOString() : undefined,
        vendorNote: newStatus === "WaitingForVendor" ? vendorNote.trim() : undefined,
      });
      toast.success("Cập nhật trạng thái thành công!");
      setStatusNote("");
      setIsVendorDialogOpen(false);
    } catch (e) {
      toastApiError(e, "Không thể cập nhật trạng thái.");
    }
  };

  const handleComment = async () => {
    const content = commentDraft.trim();
    if (!content) {
      toast.error("Nội dung comment không được để trống.");
      return;
    }
    try {
      await commentMutation.mutateAsync({ content });
      setCommentDraft("");
      toast.success("Comment đã được thêm.");
    } catch (e) {
      toastApiError(e, "Không thể thêm comment.");
    }
  };

  const isWorking =
    assignMutation.isPending || statusMutation.isPending || commentMutation.isPending;

  return (
    <div className="space-y-4">
      {/* ── ASSIGN (Admin / Manager) ─────────────────────────── */}
      {canAssignTicket && (ticket.status === "Pending" || ticket.status === "Assigned") && (
        <Card className="border-primary/20 bg-primary/5">
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-semibold flex items-center gap-2 text-primary">
              <UserCheck className="h-4 w-4" />
              <span>Phân công kỹ thuật viên</span>
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 pt-0">
            <div className="space-y-1.5">
              <Label htmlFor="assign-tech">Kỹ thuật viên chính</Label>
              <select
                id="assign-tech"
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                value={assignTechId}
                onChange={(e) => setAssignTechId(e.target.value)}
                disabled={isWorking}
              >
                <option value="" className="bg-background text-foreground">
                  {isTechLoading ? "Đang tải..." : "-- Chọn kỹ thuật viên --"}
                </option>
                {technicians.map((t) => (
                  <option key={t.id} value={t.id} className="bg-background text-foreground">
                    {t.fullName}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="support-tech">Kỹ thuật viên hỗ trợ</Label>
              <select
                id="support-tech"
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                value={supportTechId}
                onChange={(e) => setSupportTechId(e.target.value)}
                disabled={isWorking}
              >
                <option value="" className="bg-background text-foreground">
                  Không chọn
                </option>
                {technicians
                  .filter((t) => t.id.toString() !== assignTechId)
                  .map((t) => (
                    <option key={t.id} value={t.id} className="bg-background text-foreground">
                      {t.fullName}
                    </option>
                  ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="assign-note">Ghi chú phân công</Label>
              <Textarea
                id="assign-note"
                rows={2}
                value={assignNote}
                onChange={(e) => setAssignNote(e.target.value)}
                placeholder="Ví dụ: Xử lý trước 5 giờ chiều..."
                disabled={isWorking}
              />
            </div>

            <Button
              type="button"
              size="sm"
              onClick={handleAssign}
              disabled={isWorking}
              className="w-full sm:w-auto"
            >
              {assignMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-1" />
              ) : (
                <UserCheck className="h-4 w-4 mr-1" />
              )}
              Giao việc
            </Button>
          </CardContent>
        </Card>
      )}

      {/* ── STATUS TRANSITIONS ───────────────────────────────── */}
      {!isFinalized && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-semibold">Cập nhật trạng thái</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 pt-0">
            <div className="space-y-1.5">
              <Label htmlFor="status-note">Ghi chú thay đổi</Label>
              <Textarea
                id="status-note"
                rows={2}
                value={statusNote}
                onChange={(e) => setStatusNote(e.target.value)}
                placeholder="Ghi chú cho lần thay đổi này..."
                disabled={isWorking}
              />
            </div>

            {isAssignedTech && ticket.status === "InProgress" && (
              <div className="space-y-1.5">
                <Label htmlFor="resolution-note">
                  Kết quả xử lý <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  id="resolution-note"
                  rows={3}
                  value={resolutionNote}
                  onChange={(e) => setResolutionNote(e.target.value)}
                  placeholder="Mô tả kết quả sửa chữa..."
                  disabled={isWorking}
                />
              </div>
            )}

            <div className="flex flex-wrap gap-2 pt-1">
              {isAssignedTech &&
                (ticket.status === "Assigned" || ticket.status === "WaitingForVendor") && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => handleStatus("InProgress")}
                    disabled={isWorking}
                  >
                    <Play className="h-4 w-4 mr-1 text-blue-500" />
                    {ticket.status === "WaitingForVendor"
                      ? "Tiếp tục xử lý (Bỏ tạm dừng SLA)"
                      : "Bắt đầu xử lý"}
                  </Button>
                )}

              {isAssignedTech && ticket.status === "InProgress" && (
                <>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() => setIsVendorDialogOpen(true)}
                    disabled={isWorking}
                  >
                    Chuyển đơn vị ngoài
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    onClick={() => handleStatus("Resolved", true)}
                    disabled={isWorking}
                  >
                    <CheckCircle className="h-4 w-4 mr-1 text-emerald-400" />
                    Hoàn thành
                  </Button>
                </>
              )}

              {(isRequester || role === "Admin" || role === "Manager") &&
                ticket.status === "Resolved" && (
                  <Button
                    type="button"
                    size="sm"
                    onClick={() => handleStatus("Closed")}
                    disabled={isWorking}
                  >
                    <CheckCircle className="h-4 w-4 mr-1" />
                    Đóng ticket
                  </Button>
                )}

              {(isRequester || role === "Admin" || role === "Manager") && (
                <Button
                  type="button"
                  variant="destructive"
                  size="sm"
                  onClick={() => setIsCancelDialogOpen(true)}
                  disabled={isWorking}
                >
                  <XCircle className="h-4 w-4 mr-1" />
                  Hủy ticket
                </Button>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* ── ATTACHMENTS ───────────────────────────────────────── */}
      <AttachmentsSection
        ticketId={ticket.id}
        ticketStatus={ticket.status}
        isFinalized={isFinalized}
        currentUserId={userId ?? 0}
        role={role}
        isAssignedTech={isAssignedTech}
        isRequester={isRequester}
      />

      {/* ── COMMENTS ──────────────────────────────────────────── */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <MessageSquare className="h-4 w-4 text-primary" />
            <span>Bình luận</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4 pt-0">
          <TicketCommentsList comments={ticket.comments} currentUserId={userId} />

          {!isFinalized && (
            <div className="space-y-2 pt-2 border-t">
              <Textarea
                rows={2}
                value={commentDraft}
                onChange={(e) => setCommentDraft(e.target.value)}
                placeholder="Nhập bình luận..."
                disabled={isWorking}
              />
              <div className="flex justify-end">
                <Button
                  type="button"
                  size="sm"
                  onClick={handleComment}
                  disabled={isWorking || !commentDraft.trim()}
                  className="gap-1.5"
                >
                  {commentMutation.isPending ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Send className="h-3.5 w-3.5" />
                  )}
                  <span>Gửi comment</span>
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* ── CANCEL TICKET DIALOG ─────────────────────────────── */}
      <Dialog open={isCancelDialogOpen} onOpenChange={setIsCancelDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-destructive">
              <AlertTriangle className="h-5 w-5" />
              <span>Xác nhận Hủy Ticket</span>
            </DialogTitle>
            <DialogDescription>
              Vui lòng chọn hoặc nhập lý do hủy ticket này để lưu vào nhật ký hệ thống.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label className="text-xs font-medium">
                Lý do hủy ticket <span className="text-destructive">*</span>
              </Label>
              <select
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={cancelReasonPreset}
                onChange={(e) => setCancelReasonPreset(e.target.value)}
              >
                {PREDEFINED_CANCEL_REASONS.map((reason) => (
                  <option key={reason} value={reason} className="bg-background text-foreground">
                    {reason}
                  </option>
                ))}
              </select>
            </div>

            {cancelReasonPreset === "Lý do khác" && (
              <div className="space-y-2">
                <Label className="text-xs font-medium">
                  Chi tiết lý do hủy khác <span className="text-destructive">*</span>
                </Label>
                <Textarea
                  rows={3}
                  value={customCancelReason}
                  onChange={(e) => setCustomCancelReason(e.target.value)}
                  placeholder="Nhập lý do cụ thể vì sao hủy ticket..."
                />
              </div>
            )}
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsCancelDialogOpen(false)}
              disabled={statusMutation.isPending}
            >
              Hủy bỏ
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmCancel}
              disabled={statusMutation.isPending}
            >
              {statusMutation.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
              Xác nhận Hủy
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      {/* ── VENDOR TICKET DIALOG ─────────────────────────────── */}
      <Dialog open={isVendorDialogOpen} onOpenChange={setIsVendorDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <span>Chuyển thiết bị cho đơn vị ngoài</span>
            </DialogTitle>
            <DialogDescription>
              Thiết bị sẽ được chuyển cho đối tác sửa chữa, thời gian SLA sẽ được tạm dừng cho đến
              khi thiết bị được trả về.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label className="text-xs font-medium">
                Đơn vị ngoài (Vendor) <span className="text-destructive">*</span>
              </Label>
              <select
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={vendorId}
                onChange={(e) => setVendorId(e.target.value)}
              >
                <option value="">
                  {isVendorsLoading ? "Đang tải danh sách đối tác..." : "-- Chọn đơn vị ngoài --"}
                </option>
                {vendors?.map((v) => (
                  <option key={v.id} value={v.id} className="bg-background text-foreground">
                    {v.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <Label className="text-xs font-medium">
                Dự kiến ngày trả <span className="text-destructive">*</span>
              </Label>
              <input
                type="date"
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                value={vendorReturnDate}
                onChange={(e) => setVendorReturnDate(e.target.value)}
                min={new Date().toISOString().split("T")[0]}
              />
            </div>

            <div className="space-y-2">
              <Label className="text-xs font-medium">Ghi chú thêm</Label>
              <Textarea
                rows={3}
                value={vendorNote}
                onChange={(e) => setVendorNote(e.target.value)}
                placeholder="Ghi chú về tình trạng thiết bị khi bàn giao..."
              />
            </div>
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsVendorDialogOpen(false)}
              disabled={statusMutation.isPending}
            >
              Hủy bỏ
            </Button>
            <Button
              type="button"
              onClick={() => handleStatus("WaitingForVendor")}
              disabled={statusMutation.isPending}
            >
              {statusMutation.isPending && <Loader2 className="h-4 w-4 animate-spin mr-1" />}
              Xác nhận chuyển
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

type AttachmentsSectionProps = {
  ticketId: number;
  ticketStatus: TicketStatus;
  isFinalized: boolean;
  currentUserId: number;
  role: RoleName | undefined;
  isAssignedTech: boolean;
};

function canUploadByRole(
  role: RoleName | undefined,
  ticketStatus: TicketStatus,
  isFinalized: boolean,
  isAssignedTech: boolean,
  isRequester: boolean,
): boolean {
  if (isFinalized) return false;
  if (role === "Admin" || role === "Manager") return true;

  if (isRequester) {
    return ticketStatus === "Pending" || ticketStatus === "Assigned";
  }

  if (role === "Technician") {
    return isAssignedTech && (ticketStatus === "InProgress" || ticketStatus === "Resolved");
  }

  return false;
}

function AttachmentsSection({
  ticketId,
  ticketStatus,
  isFinalized,
  currentUserId,
  role,
  isAssignedTech,
  isRequester,
}: AttachmentsSectionProps & { isRequester: boolean }) {
  const { data: attachments = [], isLoading, error } = useTicketAttachmentsQuery(ticketId);
  const { uploadItems, uploadFiles, removeItem } = useUploadAttachment(ticketId);

  const canUpload = canUploadByRole(role, ticketStatus, isFinalized, isAssignedTech, isRequester);
  const isForbidden = axios.isAxiosError(error) && error.response?.status === 403;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-semibold flex items-center gap-2">
          <Paperclip className="h-4 w-4 text-primary" />
          <span>File đính kèm</span>
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 pt-0">
        {canUpload && !isForbidden && (
          <AttachmentUploadZone
            uploadItems={uploadItems}
            onFilesSelected={uploadFiles}
            onRemoveItem={removeItem}
            disabled={false}
          />
        )}

        {isLoading ? (
          <p className="text-xs text-muted-foreground italic">Đang tải file...</p>
        ) : isForbidden ? (
          <p className="text-xs text-muted-foreground italic">
            Không có quyền xem file đính kèm của ticket này.
          </p>
        ) : (
          <AttachmentList
            ticketId={ticketId}
            attachments={attachments}
            canDelete={!isFinalized}
            currentUserId={currentUserId}
          />
        )}
      </CardContent>
    </Card>
  );
}
