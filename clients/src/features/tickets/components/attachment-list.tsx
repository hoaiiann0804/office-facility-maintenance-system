import { useState } from "react";
import { toast } from "sonner";
import type { TicketAttachment } from "../../../entities/ticket/model/types";
import { getAttachmentDownloadUrl } from "../../../shared/api/attachments";
import { useDeleteAttachmentMutation } from "../api/use-delete-attachment-mutation";
import { getFriendlyErrorMessage } from "@/shared/lib/error-utils";
import { AttachmentPreviewModal } from "./attachment-preview-modal";
import { Button } from "@/components/ui/button";
import { FileText, Image as ImageIcon, Video, Trash2, Download, Eye, Loader2 } from "lucide-react";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(dateStr: string): string {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(dateStr));
}

function getFileIcon(fileType: TicketAttachment["fileType"]) {
  switch (fileType) {
    case "Image":
      return <ImageIcon className="h-4 w-4 text-blue-500" />;
    case "Video":
      return <Video className="h-4 w-4 text-purple-500" />;
    case "Document":
    default:
      return <FileText className="h-4 w-4 text-amber-500" />;
  }
}

type Props = {
  ticketId: number;
  attachments: TicketAttachment[];
  canDelete: boolean;
  currentUserId: number;
};

export function AttachmentList({ ticketId, attachments, canDelete, currentUserId }: Props) {
  const [openingId, setOpeningId] = useState<number | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  // State for preview modal
  const [previewAttachment, setPreviewAttachment] = useState<TicketAttachment | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const deleteMutation = useDeleteAttachmentMutation(ticketId);

  const handlePreview = async (attachment: TicketAttachment) => {
    if (openingId === attachment.id) return;
    setOpeningId(attachment.id);
    try {
      const url = await getAttachmentDownloadUrl(ticketId, attachment.id);
      setPreviewAttachment(attachment);
      setPreviewUrl(url);
    } catch (err) {
      console.error("Preview attachment failed:", err);
      toast.error(getFriendlyErrorMessage(err, "Không thể tải file xem trước."));
    } finally {
      setOpeningId(null);
    }
  };

  const handleDownload = async (attachment: TicketAttachment, e: React.MouseEvent) => {
    e.stopPropagation();
    if (openingId === attachment.id) return;
    setOpeningId(attachment.id);
    try {
      const url = await getAttachmentDownloadUrl(ticketId, attachment.id);
      window.open(url, "_blank", "noopener,noreferrer");
    } catch (err) {
      console.error("Download attachment failed:", err);
      toast.error(getFriendlyErrorMessage(err, "Không thể tải về file."));
    } finally {
      setOpeningId(null);
    }
  };

  const handleDelete = async (attachment: TicketAttachment, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!window.confirm(`Xóa file "${attachment.originalFileName}"?`)) return;
    setDeletingId(attachment.id);
    try {
      await deleteMutation.mutateAsync(attachment.id);
      toast.success("Đã xóa file đính kèm.");
      if (previewAttachment?.id === attachment.id) {
        setPreviewAttachment(null);
        setPreviewUrl(null);
      }
    } catch (err) {
      console.error("Delete attachment failed:", err);
      toast.error(getFriendlyErrorMessage(err, "Không thể xóa file đính kèm."));
    } finally {
      setDeletingId(null);
    }
  };

  if (attachments.length === 0) {
    return (
      <p className="text-xs text-muted-foreground text-center py-3 italic">
        Chưa có file đính kèm nào.
      </p>
    );
  }

  return (
    <>
      <ul className="space-y-2">
        {attachments.map((att) => {
          const isOpening = openingId === att.id;
          const isDeleting = deletingId === att.id;
          const canDeleteThis = canDelete && att.uploadedByUserId === currentUserId;

          return (
            <li
              key={att.id}
              className="flex items-center justify-between gap-3 p-2.5 rounded-lg border bg-card hover:bg-accent/50 transition-colors group"
            >
              <div
                className="flex items-center gap-2.5 min-w-0 flex-1 cursor-pointer"
                onClick={() => void handlePreview(att)}
              >
                <div className="p-1.5 rounded-md bg-muted shrink-0 group-hover:bg-primary/10 group-hover:text-primary transition-colors">
                  {getFileIcon(att.fileType)}
                </div>
                <div className="min-w-0 flex-1">
                  <span className="text-xs font-medium text-foreground group-hover:text-primary transition-colors truncate block">
                    {att.originalFileName}
                  </span>
                  <div className="flex items-center gap-1.5 text-[10px] text-muted-foreground mt-0.5">
                    <span>{formatBytes(att.fileSize)}</span>
                    <span>·</span>
                    <span>{att.uploadedByUserName}</span>
                    <span>·</span>
                    <span>{formatDate(att.createdAt)}</span>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-1 shrink-0">
                {/* Preview Button */}
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-muted-foreground hover:text-primary"
                  onClick={() => void handlePreview(att)}
                  disabled={isOpening}
                  title="Xem trước (Preview)"
                >
                  {isOpening ? (
                    <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  ) : (
                    <Eye className="h-3.5 w-3.5" />
                  )}
                </Button>

                {/* Download Button */}
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-muted-foreground hover:text-primary"
                  onClick={(e) => void handleDownload(att, e)}
                  disabled={isOpening}
                  title="Tải về"
                >
                  <Download className="h-3.5 w-3.5" />
                </Button>

                {/* Delete Button */}
                {canDeleteThis && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-muted-foreground hover:text-destructive"
                    onClick={(e) => void handleDelete(att, e)}
                    disabled={isDeleting}
                    title="Xóa file"
                  >
                    {isDeleting ? (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ) : (
                      <Trash2 className="h-3.5 w-3.5" />
                    )}
                  </Button>
                )}
              </div>
            </li>
          );
        })}
      </ul>

      {/* Preview Modal */}
      <AttachmentPreviewModal
        attachment={previewAttachment}
        url={previewUrl}
        isOpen={Boolean(previewAttachment && previewUrl)}
        onClose={() => {
          setPreviewAttachment(null);
          setPreviewUrl(null);
        }}
      />
    </>
  );
}
