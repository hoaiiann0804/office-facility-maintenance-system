import type { PropsWithChildren, ReactNode } from "react";
export { Banner } from "./banner";

type PanelProps = PropsWithChildren<{
  className?: string;
}>;

type BadgeProps = PropsWithChildren<{
  tone?: "default" | "primary" | "good" | "warn" | "bad";
}>;

type StatCardProps = {
  label: string;
  value: ReactNode;
  note?: string;
};

export function Panel({ className = "", children }: PanelProps) {
  return <section className={`panel ${className}`.trim()}>{children}</section>;
}

export function Badge({ tone = "default", children }: BadgeProps) {
  return <span className={`badge ${tone}`.trim()}>{children}</span>;
}

export function StatCard({ label, value, note }: StatCardProps) {
  return (
    <article className="stat-card">
      <div className="stat-card-label">{label}</div>
      <div className="stat-card-value">{value}</div>
      {note ? <div className="stat-card-note">{note}</div> : null}
    </article>
  );
}

type EmptyStateProps = {
  title: string;
  description: string;
};

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      <p>{description}</p>
    </div>
  );
}

export function Spinner() {
  return <div className="spinner" />;
}
