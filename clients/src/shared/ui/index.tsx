import type { PropsWithChildren, ReactNode } from "react";
import { cn } from "@/lib/utils";
import { Card, CardContent } from "@/components/ui/card";
import { Badge as ShadcnBadge } from "@/components/ui/badge";

export { Banner } from "./banner";
export { ThemeToggle } from "./theme-toggle";
export { UserProfile } from "./user-profile";

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
  icon?: ReactNode;
  className?: string;
};

export function Panel({ className = "", children }: PanelProps) {
  return (
    <section className={cn("rounded-xl border bg-card p-6 shadow-sm transition-all", className)}>
      {children}
    </section>
  );
}

export function Badge({ tone = "default", children }: BadgeProps) {
  const variantMap: Record<
    string,
    "default" | "secondary" | "destructive" | "outline" | "success" | "warning"
  > = {
    default: "secondary",
    primary: "default",
    good: "success",
    warn: "warning",
    bad: "destructive",
  };

  return <ShadcnBadge variant={variantMap[tone] ?? "default"}>{children}</ShadcnBadge>;
}

export function StatCard({ label, value, note, icon, className }: StatCardProps) {
  return (
    <Card
      className={cn("overflow-hidden border shadow-sm transition-all hover:shadow-md", className)}
    >
      <CardContent className="p-5">
        <div className="flex items-center justify-between">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            {label}
          </p>
          {icon && <div className="p-2 rounded-lg bg-primary/10 text-primary">{icon}</div>}
        </div>
        <div className="mt-2 text-2xl font-bold tracking-tight text-foreground">{value}</div>
        {note && <p className="mt-1 text-xs text-muted-foreground">{note}</p>}
      </CardContent>
    </Card>
  );
}

type EmptyStateProps = {
  title: string;
  description: string;
};

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed p-8 text-center bg-muted/20">
      <p className="font-semibold text-foreground text-sm">{title}</p>
      <p className="text-xs text-muted-foreground mt-1 max-w-sm">{description}</p>
    </div>
  );
}

export function Spinner() {
  return (
    <div className="flex items-center justify-center p-8">
      <div className="h-7 w-7 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
    </div>
  );
}
