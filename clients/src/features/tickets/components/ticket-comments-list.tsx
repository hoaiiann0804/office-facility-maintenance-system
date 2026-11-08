import type { TicketComment } from "../../../entities/ticket/model/types";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";

type Props = {
  comments: TicketComment[];
  currentUserId: number | undefined;
};

import { formatDateTime } from "@/shared/lib/date-utils";

function getInitials(name: string | undefined) {
  if (!name) return "?";
  const parts = name.trim().split(" ");
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return name.charAt(0).toUpperCase();
}

export function TicketCommentsList({ comments, currentUserId }: Props) {
  if (!comments || comments.length === 0) {
    return (
      <div className="py-6 text-center text-xs text-muted-foreground italic bg-muted/20 rounded-lg border border-dashed">
        Chưa có bình luận nào.
      </div>
    );
  }

  return (
    <div className="space-y-4 max-h-[380px] overflow-y-auto pr-2 py-1">
      {comments.map((comment) => {
        const isMine = comment.userId === currentUserId;

        return (
          <div
            key={comment.id}
            className={`flex items-start gap-2.5 ${isMine ? "flex-row-reverse" : "flex-row"}`}
          >
            <Avatar className="h-7 w-7 text-xs shrink-0 mt-0.5 border">
              <AvatarFallback className="text-[10px] font-semibold">
                {getInitials(comment.userName)}
              </AvatarFallback>
            </Avatar>

            <div className={`flex flex-col max-w-[80%] ${isMine ? "items-end" : "items-start"}`}>
              <div className="flex items-center gap-1.5 text-[11px] text-muted-foreground mb-1 px-1">
                <span className="font-semibold text-foreground">
                  {isMine ? "Bạn" : comment.userName}
                </span>
                <span>•</span>
                <span>{formatDateTime(comment.createdAt)}</span>
              </div>
              <div
                className={`p-3 rounded-2xl text-xs leading-relaxed break-words shadow-sm ${
                  isMine
                    ? "bg-primary text-primary-foreground rounded-tr-xs"
                    : "bg-muted text-foreground border rounded-tl-xs"
                }`}
              >
                {comment.content}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
