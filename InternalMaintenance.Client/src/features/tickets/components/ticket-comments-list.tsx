import type { TicketComment } from "../../../entities/ticket/model/types";

type Props = {
  comments: TicketComment[];
  currentUserId: number | undefined;
};

const formatDateTime = (value: string | null | undefined) => {
  if (!value) return "N/A";
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
};

export function TicketCommentsList({ comments, currentUserId }: Props) {
  if (!comments || comments.length === 0) {
    return (
      <div style={{ padding: "12px 0", color: "var(--muted)", fontStyle: "italic", fontSize: "0.85rem" }}>
        Chưa có bình luận nào.
      </div>
    );
  }

  return (
    <div className="comments-thread" style={{ display: "flex", flexDirection: "column", gap: "12px", marginTop: "12px", maxHeight: "400px", overflowY: "auto", paddingRight: "8px" }}>
      {comments.map((comment) => {
        const isMine = comment.userId === currentUserId;
        
        return (
          <div
            key={comment.id}
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: isMine ? "flex-end" : "flex-start",
            }}
          >
            <div
              style={{
                fontSize: "0.75rem",
                color: "var(--muted)",
                marginBottom: "4px",
                marginLeft: isMine ? "0" : "4px",
                marginRight: isMine ? "4px" : "0",
              }}
            >
              <strong>{isMine ? "Bạn" : comment.userName}</strong> • {formatDateTime(comment.createdAt)}
            </div>
            <div
              style={{
                backgroundColor: isMine ? "var(--primary-color, #2563eb)" : "var(--surface-sunken, #f1f5f9)",
                color: isMine ? "#fff" : "inherit",
                padding: "8px 12px",
                borderRadius: "16px",
                borderTopRightRadius: isMine ? "4px" : "16px",
                borderTopLeftRadius: !isMine ? "4px" : "16px",
                maxWidth: "85%",
                wordBreak: "break-word",
                whiteSpace: "pre-wrap",
                fontSize: "0.9rem",
                boxShadow: "0 1px 2px rgba(0,0,0,0.05)"
              }}
            >
              {comment.content}
            </div>
          </div>
        );
      })}
    </div>
  );
}
