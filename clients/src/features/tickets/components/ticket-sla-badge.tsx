import { Badge } from "@/components/ui/badge";
import { Clock, AlertTriangle, CheckCircle2, XCircle } from "lucide-react";
import { calculateSlaInfo } from "../lib/sla-utils";
import type { MaintenanceTicket } from "../../../entities/ticket/model/types";

export function TicketSlaBadge({ ticket }: { ticket: MaintenanceTicket }) {
  const sla = calculateSlaInfo(ticket);

  const renderIcon = () => {
    switch (sla.statusType) {
      case "MetSLA":
        return <CheckCircle2 className="h-3 w-3 mr-1" />;
      case "MissedSLA":
      case "Breached":
        return <AlertTriangle className="h-3 w-3 mr-1 animate-pulse text-destructive-foreground" />;
      case "NearBreach":
        return <Clock className="h-3 w-3 mr-1 animate-spin" />;
      case "Cancelled":
        return <XCircle className="h-3 w-3 mr-1" />;
      default:
        return <Clock className="h-3 w-3 mr-1" />;
    }
  };

  return (
    <Badge
      variant={sla.badgeVariant}
      className="text-[10px] font-semibold px-2 py-0.5 inline-flex items-center"
    >
      {renderIcon()}
      <span>{sla.badgeLabel}</span>
    </Badge>
  );
}

export function TicketSlaDetailBar({ ticket }: { ticket: MaintenanceTicket }) {
  const sla = calculateSlaInfo(ticket);

  const getContainerBg = () => {
    switch (sla.statusType) {
      case "MetSLA":
        return "bg-emerald-500/10 border-emerald-500/30 text-emerald-700 dark:text-emerald-300";
      case "Breached":
      case "MissedSLA":
        return "bg-destructive/10 border-destructive/30 text-destructive dark:text-red-400";
      case "NearBreach":
        return "bg-amber-500/10 border-amber-500/30 text-amber-700 dark:text-amber-300";
      default:
        return "bg-primary/5 border-primary/20 text-primary";
    }
  };

  return (
    <div
      className={`p-3.5 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs ${getContainerBg()}`}
    >
      <div className="flex items-center gap-2.5">
        {sla.isBreached ? (
          <AlertTriangle className="h-5 w-5 shrink-0 text-destructive animate-bounce" />
        ) : sla.statusType === "MetSLA" ? (
          <CheckCircle2 className="h-5 w-5 shrink-0 text-emerald-500" />
        ) : (
          <Clock className="h-5 w-5 shrink-0 text-primary" />
        )}
        <div>
          <p className="font-semibold text-foreground">
            Hạn chót SLA ({ticket.priority}):{" "}
            <span className="font-mono">{sla.deadlineFormatted}</span>
          </p>
          <p className="text-[11px] text-muted-foreground mt-0.5">{sla.remainingText}</p>
        </div>
      </div>

      <div className="shrink-0">
        <TicketSlaBadge ticket={ticket} />
      </div>
    </div>
  );
}
