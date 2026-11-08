import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Download, ExternalLink, FileText, Image as ImageIcon, Video } from "lucide-react";
import type { TicketAttachment } from "@/entities/ticket/model/types";

type Props = {
  attachment: TicketAttachment | null;
  url: string | null;
  isOpen: boolean;
  onClose: () => void;
};

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function AttachmentPreviewModal({ attachment, url, isOpen, onClose }: Props) {
  if (!attachment || !url) return null;

  const isImage = attachment.fileType === "Image" || attachment.contentType?.startsWith("image/");
  const isVideo = attachment.fileType === "Video" || attachment.contentType?.startsWith("video/");
  const isPdf =
    attachment.contentType?.includes("pdf") ||
    attachment.originalFileName.toLowerCase().endsWith(".pdf");

  const handleDownload = () => {
    window.open(url, "_blank", "noopener,noreferrer");
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-4xl max-h-[90vh] flex flex-col p-4 sm:p-6 gap-4">
        <DialogHeader className="flex flex-row items-center justify-between space-y-0 pr-6 border-b pb-3">
          <div className="min-w-0 pr-4">
            <DialogTitle className="text-base font-semibold truncate flex items-center gap-2">
              {isImage && <ImageIcon className="h-4 w-4 text-blue-500 shrink-0" />}
              {isVideo && <Video className="h-4 w-4 text-purple-500 shrink-0" />}
              {!isImage && !isVideo && <FileText className="h-4 w-4 text-amber-500 shrink-0" />}
              <span className="truncate">{attachment.originalFileName}</span>
            </DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground mt-0.5">
              {formatBytes(attachment.fileSize)} · Đăng bởi {attachment.uploadedByUserName}
            </DialogDescription>
          </div>

          <Button variant="outline" size="sm" onClick={handleDownload} className="gap-1.5 shrink-0">
            <Download className="h-3.5 w-3.5" />
            <span>Tải về</span>
          </Button>
        </DialogHeader>

        {/* Media Preview Container */}
        <div className="flex-1 min-h-0 overflow-auto flex items-center justify-center bg-muted/20 rounded-xl p-2 border">
          {isImage ? (
            <img
              src={url}
              alt={attachment.originalFileName}
              className="max-h-[68vh] max-w-full object-contain rounded-lg shadow-sm"
            />
          ) : isVideo ? (
            <video
              src={url}
              controls
              autoPlay
              className="max-h-[68vh] max-w-full rounded-lg shadow-sm"
            >
              Trình duyệt của bạn không hỗ trợ phát video.
            </video>
          ) : isPdf ? (
            <iframe
              src={url}
              title={attachment.originalFileName}
              className="w-full h-[68vh] rounded-lg border bg-white"
            />
          ) : (
            <div className="text-center py-12 px-4 space-y-3">
              <div className="w-16 h-16 rounded-full bg-primary/10 text-primary flex items-center justify-center mx-auto">
                <FileText className="h-8 w-8" />
              </div>
              <div>
                <p className="font-semibold text-foreground text-sm">
                  {attachment.originalFileName}
                </p>
                <p className="text-xs text-muted-foreground mt-1">
                  Định dạng file không hỗ trợ xem trước trực tiếp trong ứng dụng.
                </p>
              </div>
              <Button variant="default" size="sm" onClick={handleDownload} className="gap-1.5">
                <ExternalLink className="h-3.5 w-3.5" />
                <span>Mở trong tab mới / Tải về</span>
              </Button>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
