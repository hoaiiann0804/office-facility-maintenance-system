import { useRef, useState, useCallback, type DragEvent } from "react";
import { toast } from "sonner";
import type { FileUploadItem } from "../../../entities/ticket/model/types";
import {
  UploadCloud,
  X,
  FileText,
  Image as ImageIcon,
  Video,
  CheckCircle2,
  AlertTriangle,
  Loader2,
} from "lucide-react";
import { Button } from "@/components/ui/button";

// Chặn sớm để người dùng không phải chờ upload một file chắc chắn bị API từ chối.
// Đây chỉ là kiểm tra UX; API vẫn validate request và HEAD storage vẫn kiểm tra kích thước object thật.
const MAX_SIZE_BYTES = 100 * 1024 * 1024; // 100 MiB
const ALLOWED_TYPES = [
  "image/jpeg",
  "image/jpg",
  "image/png",
  "image/webp",
  "video/mp4",
  "video/webm",
  "application/pdf",
];

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getStatusBadge(status: FileUploadItem["status"]) {
  switch (status) {
    case "pending":
      return <span className="text-amber-500 font-medium">Chờ...</span>;
    case "uploading":
      return <span className="text-blue-500 font-medium">Đang tải...</span>;
    case "confirming":
      return <span className="text-blue-500 font-medium">Xác nhận...</span>;
    case "done":
      return (
        <span className="text-emerald-500 font-medium flex items-center gap-1">
          <CheckCircle2 className="h-3 w-3" /> Xong
        </span>
      );
    case "error":
      return (
        <span className="text-destructive font-medium flex items-center gap-1">
          <AlertTriangle className="h-3 w-3" /> Lỗi
        </span>
      );
  }
}

function getFileIcon(contentType: string) {
  if (contentType.startsWith("image/")) return <ImageIcon className="h-4 w-4 text-blue-500" />;
  if (contentType.startsWith("video/")) return <Video className="h-4 w-4 text-purple-500" />;
  return <FileText className="h-4 w-4 text-amber-500" />;
}

type Props = {
  uploadItems: FileUploadItem[];
  onFilesSelected: (files: File[]) => void;
  onRemoveItem: (uid: string) => void;
  disabled?: boolean;
};

export function AttachmentUploadZone({
  uploadItems,
  onFilesSelected,
  onRemoveItem,
  disabled,
}: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const processFiles = useCallback(
    (rawFiles: FileList | File[]) => {
      const files = Array.from(rawFiles);
      const valid: File[] = [];

      for (const file of files) {
        if (!ALLOWED_TYPES.includes(file.type)) {
          toast.error(`"${file.name}" — định dạng không được hỗ trợ.`);
          continue;
        }
        if (file.size > MAX_SIZE_BYTES) {
          toast.error(`"${file.name}" — vượt quá giới hạn 100MB.`);
          continue;
        }
        if (file.size === 0) {
          toast.error(`"${file.name}" — file rỗng, không hợp lệ.`);
          continue;
        }
        valid.push(file);
      }

      if (valid.length > 0) {
        onFilesSelected(valid);
      }
    },
    [onFilesSelected],
  );

  const onDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (!disabled) setIsDragging(true);
  };
  const onDragLeave = () => setIsDragging(false);
  const onDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    if (!disabled) processFiles(e.dataTransfer.files);
  };

  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) processFiles(e.target.files);
    e.target.value = "";
  };

  const hasActiveUploads = uploadItems.some(
    (i) => i.status === "uploading" || i.status === "confirming",
  );

  return (
    <div className="space-y-3">
      <div
        className={`border-2 border-dashed rounded-xl p-4 text-center cursor-pointer transition-all flex flex-col items-center justify-center gap-1.5 ${
          isDragging
            ? "border-primary bg-primary/5 scale-[0.99]"
            : disabled
              ? "border-muted bg-muted/20 opacity-60 cursor-not-allowed"
              : "border-muted-foreground/25 hover:border-primary hover:bg-muted/30"
        }`}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
        onClick={() => !disabled && inputRef.current?.click()}
        role="button"
        tabIndex={0}
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept={ALLOWED_TYPES.join(",")}
          className="hidden"
          onChange={onInputChange}
          disabled={disabled}
        />
        <UploadCloud className="h-6 w-6 text-muted-foreground" />
        <p className="text-xs font-semibold text-foreground">
          {disabled
            ? "Ticket đã đóng — không thể đính kèm file"
            : "Kéo thả file vào đây hoặc click để chọn"}
        </p>
        <p className="text-[11px] text-muted-foreground">
          Hỗ trợ: JPG, PNG, WEBP, MP4, WEBM, PDF · Tối đa 100MB/file
        </p>
      </div>

      {uploadItems.length > 0 && (
        <ul className="space-y-2">
          {uploadItems.map((item) => (
            <li
              key={item.uid}
              className="flex items-center justify-between gap-3 p-2.5 rounded-lg border bg-card text-xs"
            >
              <div className="flex items-center gap-2.5 min-w-0 flex-1">
                <div className="p-1 rounded bg-muted shrink-0">{getFileIcon(item.file.type)}</div>
                <div className="min-w-0 flex-1">
                  <p className="font-medium text-foreground truncate">{item.file.name}</p>
                  <div className="flex items-center gap-2 text-[10px] text-muted-foreground mt-0.5">
                    <span>{formatBytes(item.file.size)}</span>
                    <span>·</span>
                    {getStatusBadge(item.status)}
                    {item.status === "error" && item.error && (
                      <span className="text-destructive truncate"> — {item.error}</span>
                    )}
                  </div>

                  {(item.status === "uploading" || item.status === "confirming") && (
                    <div className="w-full bg-muted h-1 rounded-full overflow-hidden mt-1.5">
                      <div
                        className="bg-primary h-full transition-all duration-300"
                        style={{ width: `${item.progress}%` }}
                      />
                    </div>
                  )}
                </div>
              </div>

              {(item.status === "done" || item.status === "error") && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-6 w-6 text-muted-foreground hover:text-foreground shrink-0"
                  onClick={() => onRemoveItem(item.uid)}
                >
                  <X className="h-3.5 w-3.5" />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {hasActiveUploads && (
        <p className="text-[11px] text-blue-500 font-medium animate-pulse flex items-center gap-1.5">
          <Loader2 className="h-3 w-3 animate-spin" /> Đang upload file... Vui lòng không đóng
          trang.
        </p>
      )}
    </div>
  );
}
