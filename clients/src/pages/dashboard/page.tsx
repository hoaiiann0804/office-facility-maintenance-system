import { useMemo } from "react";
import { Link } from "react-router-dom";
import { appRoutes } from "@/shared/config/routes";
import { useTicketsQuery } from "@/features/tickets/api/use-tickets-query";
import { useDashboardSummaryQuery } from "@/features/dashboard/api/use-dashboard-summary-query";
import { useDashboardChartsQuery } from "@/features/dashboard/api/use-dashboard-charts-query";
import { calculateSlaInfo } from "@/features/tickets/lib/sla-utils";
import { TicketSlaBadge } from "@/features/tickets/components/ticket-sla-badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Ticket,
  Wrench,
  Building2,
  ArrowRight,
  Clock,
  AlertTriangle,
  ShieldCheck,
} from "lucide-react";
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
} from "recharts";

const STATUS_WORKFLOW: Array<{ status: string; label: string; desc: string }> = [
  { status: "Pending", label: "Chờ tiếp nhận", desc: "Ticket mới tạo, chờ phân công" },
  { status: "Assigned", label: "Đã phân công", desc: "Kỹ thuật viên đã được gán" },
  { status: "InProgress", label: "Đang xử lý", desc: "Đang tiến hành sửa chữa/bảo trì" },
  { status: "Resolved", label: "Đã xử lý", desc: "Sửa xong, chờ xác nhận" },
  { status: "Closed", label: "Đã đóng", desc: "Yêu cầu đã hoàn tất hoàn toàn" },
];

import { formatDateTime } from "@/shared/lib/date-utils";

const CHART_COLORS = ["#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899"];

export function DashboardPage() {
  const { data: summary, isLoading: isSummaryLoading } = useDashboardSummaryQuery();
  const { data: charts, isLoading: isChartsLoading } = useDashboardChartsQuery();
  const { data: ticketsPage, isLoading: isTicketsLoading } = useTicketsQuery({ pageSize: 100 });

  const allTickets = useMemo(() => ticketsPage?.items ?? [], [ticketsPage?.items]);
  const recentTickets = useMemo(() => allTickets.slice(0, 6), [allTickets]);

  const slaMetrics = useMemo(() => {
    let overdueCount = 0;
    let metSlaCount = 0;
    let missedSlaCount = 0;

    for (const t of allTickets) {
      const info = calculateSlaInfo(t);
      if (info.isBreached) {
        overdueCount++;
      }
      if (info.statusType === "MetSLA") {
        metSlaCount++;
      } else if (info.statusType === "MissedSLA") {
        missedSlaCount++;
      }
    }

    const totalFinalized = metSlaCount + missedSlaCount;
    const complianceRate =
      totalFinalized > 0 ? Math.round((metSlaCount / totalFinalized) * 100) : 100;

    return {
      overdueCount,
      complianceRate,
    };
  }, [allTickets]);

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Resolved":
        return <Badge variant="success">Đã xử lý</Badge>;
      case "Closed":
        return <Badge variant="success">Đã đóng</Badge>;
      case "InProgress":
        return <Badge variant="default">Đang xử lý</Badge>;
      case "Assigned":
        return <Badge variant="warning">Đã phân công</Badge>;
      case "Cancelled":
        return <Badge variant="outline">Đã hủy</Badge>;
      case "WaitingForVendor":
        return <Badge variant="destructive">Chờ đối tác</Badge>;
      default:
        return <Badge variant="secondary">Chờ tiếp nhận</Badge>;
    }
  };

  const getPriorityBadge = (priority: string) => {
    switch (priority) {
      case "Critical":
        return <Badge variant="destructive">Khẩn cấp</Badge>;
      case "High":
        return <Badge variant="destructive">Cao</Badge>;
      case "Medium":
        return <Badge variant="warning">Trung bình</Badge>;
      default:
        return <Badge variant="outline">Thấp</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-card border rounded-2xl p-6 shadow-sm">
        <div>
          <span className="text-xs font-semibold text-primary uppercase tracking-wider">
            Tổng quan
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-foreground mt-1">
            Tổng quan hệ thống
          </h1>
          <p className="text-xs text-muted-foreground mt-1">
            Tổng quan dữ liệu hệ thống quản lý bảo trì nội bộ — dữ liệu thời gian thực.
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          <Button asChild variant="default" size="sm">
            <Link to={appRoutes.tickets} className="gap-2">
              <Ticket className="h-4 w-4" />
              <span>Quản lý Phiếu yêu cầu</span>
            </Link>
          </Button>
        </div>
      </div>

      {/* Top Stats Grid (5 Cards) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase">Phiếu đang mở</p>
              <h3 className="text-2xl font-bold mt-1">
                {isSummaryLoading ? (
                  <span className="text-muted text-base">...</span>
                ) : (
                  (summary?.openTickets ?? 0)
                )}
              </h3>
              <p className="text-[11px] text-muted-foreground mt-1">Yêu cầu chưa hoàn thành</p>
            </div>
            <div className="w-11 h-11 rounded-xl bg-blue-500/10 text-blue-600 flex items-center justify-center shrink-0">
              <Ticket className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase">
                Thiết bị hoạt động
              </p>
              <h3 className="text-2xl font-bold mt-1">
                {isSummaryLoading ? (
                  <span className="text-muted text-base">...</span>
                ) : (
                  (summary?.activeEquipment ?? 0)
                )}
              </h3>
              <p className="text-[11px] text-muted-foreground mt-1">Thiết bị đang hoạt động</p>
            </div>
            <div className="w-11 h-11 rounded-xl bg-emerald-500/10 text-emerald-600 flex items-center justify-center shrink-0">
              <Wrench className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        {/* SLA Compliance Rate Stat */}
        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase">Tỷ lệ đạt SLA</p>
              <h3 className="text-2xl font-bold mt-1 text-emerald-600 dark:text-emerald-400">
                {isTicketsLoading ? "..." : `${slaMetrics.complianceRate}%`}
              </h3>
              <p className="text-[11px] text-muted-foreground mt-1">Tỷ lệ tuân thủ SLA</p>
            </div>
            <div className="w-11 h-11 rounded-xl bg-emerald-500/10 text-emerald-600 flex items-center justify-center shrink-0">
              <ShieldCheck className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>

        {/* Overdue SLA Stat */}
        <Card className="hover:shadow-md transition-shadow border-destructive/30">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase">Quá hạn SLA</p>
              <h3 className="text-2xl font-bold mt-1 text-destructive">
                {isTicketsLoading ? "..." : slaMetrics.overdueCount}
              </h3>
              <p className="text-[11px] text-muted-foreground mt-1">Ticket quá hạn SLA</p>
            </div>
            <div className="w-11 h-11 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center shrink-0">
              <AlertTriangle className="h-5 w-5 animate-pulse" />
            </div>
          </CardContent>
        </Card>

        <Card className="hover:shadow-md transition-shadow">
          <CardContent className="p-5 flex items-center justify-between">
            <div>
              <p className="text-xs font-medium text-muted-foreground uppercase">Phòng ban</p>
              <h3 className="text-2xl font-bold mt-1">
                {isSummaryLoading ? (
                  <span className="text-muted text-base">...</span>
                ) : (
                  (summary?.totalDepartments ?? 0)
                )}
              </h3>
              <p className="text-[11px] text-muted-foreground mt-1">Phòng ban sử dụng</p>
            </div>
            <div className="w-11 h-11 rounded-xl bg-purple-500/10 text-purple-600 flex items-center justify-center shrink-0">
              <Building2 className="h-5 w-5" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Analytics Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="flex flex-col">
          <CardHeader className="pb-2">
            <CardTitle className="text-base font-semibold">Tickets theo trạng thái</CardTitle>
            <CardDescription className="text-xs">
              Phân bổ các yêu cầu bảo trì theo tiến độ xử lý
            </CardDescription>
          </CardHeader>
          <CardContent className="flex-1 min-h-[280px]">
            {isChartsLoading ? (
              <div className="h-full flex items-center justify-center">
                <div className="h-6 w-6 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </div>
            ) : !charts ? (
              <p className="text-xs text-destructive text-center py-8">
                Không thể tải dữ liệu biểu đồ.
              </p>
            ) : charts.ticketsByStatus.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                Chưa có dữ liệu ticket
              </div>
            ) : (
              <div className="h-[260px] w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={charts.ticketsByStatus}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={85}
                      paddingAngle={4}
                      label={({ name, percent }) =>
                        `${name}: ${((percent ?? 0) * 100).toFixed(0)}%`
                      }
                    >
                      {charts.ticketsByStatus.map((_, index) => (
                        <Cell
                          key={`cell-${index}`}
                          fill={CHART_COLORS[index % CHART_COLORS.length]}
                        />
                      ))}
                    </Pie>
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "var(--background)",
                        borderRadius: "8px",
                        borderColor: "var(--border)",
                      }}
                    />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>

        <Card className="flex flex-col">
          <CardHeader className="pb-2">
            <CardTitle className="text-base font-semibold">Thiết bị theo phòng ban</CardTitle>
            <CardDescription className="text-xs">
              Số lượng thiết bị phân bổ theo các phòng ban
            </CardDescription>
          </CardHeader>
          <CardContent className="flex-1 min-h-[280px]">
            {isChartsLoading ? (
              <div className="h-full flex items-center justify-center">
                <div className="h-6 w-6 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </div>
            ) : !charts ? (
              <p className="text-xs text-destructive text-center py-8">
                Không thể tải dữ liệu biểu đồ.
              </p>
            ) : charts.equipmentByDepartment.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                Chưa có dữ liệu thiết bị
              </div>
            ) : (
              <div className="h-[260px] w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart
                    data={charts.equipmentByDepartment}
                    margin={{ top: 20, right: 20, left: -10, bottom: 5 }}
                  >
                    <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                    <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                    <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "var(--background)",
                        borderRadius: "8px",
                        borderColor: "var(--border)",
                      }}
                    />
                    <Bar dataKey="value" fill="#3b82f6" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Main Grid: Recent Tickets (Left 8 Cols) & Workflow Steps (Right 4 Cols) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Recent Tickets List */}
        <Card className="lg:col-span-8">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
            <div>
              <CardTitle className="text-base font-semibold">Ticket mới nhất</CardTitle>
              <CardDescription className="text-xs">
                Các yêu cầu bảo trì vừa được tạo gần đây
              </CardDescription>
            </div>
            <Button asChild variant="ghost" size="sm" className="text-xs gap-1 text-primary">
              <Link to={appRoutes.tickets}>
                <span>Xem tất cả</span>
                <ArrowRight className="h-3.5 w-3.5" />
              </Link>
            </Button>
          </CardHeader>
          <CardContent>
            {isTicketsLoading ? (
              <div className="py-8 flex justify-center">
                <div className="h-6 w-6 rounded-full border-2 border-primary/20 border-t-primary animate-spin" />
              </div>
            ) : recentTickets.length > 0 ? (
              <div className="divide-y border rounded-lg overflow-hidden">
                {recentTickets.map((ticket) => (
                  <div
                    key={ticket.id}
                    className="p-3.5 flex items-center justify-between gap-4 hover:bg-muted/40 transition-colors"
                  >
                    <div className="min-w-0 space-y-1">
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-mono font-semibold text-primary">
                          {ticket.ticketCode}
                        </span>
                        <h4 className="text-xs font-semibold text-foreground truncate">
                          {ticket.title}
                        </h4>
                      </div>
                      <div className="flex items-center gap-3 text-[11px] text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {formatDateTime(ticket.createdAt)}
                        </span>
                      </div>
                    </div>

                    <div className="flex items-center gap-2 shrink-0">
                      <TicketSlaBadge ticket={ticket} />
                      {getPriorityBadge(ticket.priority)}
                      {getStatusBadge(ticket.status)}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-xs text-muted-foreground text-center py-8">Chưa có ticket nào.</p>
            )}
          </CardContent>
        </Card>

        {/* Ticket Lifecycle Workflow */}
        <Card className="lg:col-span-4">
          <CardHeader className="pb-3">
            <CardTitle className="text-base font-semibold">Ticket Life Cycle</CardTitle>
            <CardDescription className="text-xs">
              Quy trình xử lý một yêu cầu bảo trì
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {STATUS_WORKFLOW.map((step, idx) => (
              <div
                key={step.status}
                className="flex items-start gap-3 p-2.5 rounded-lg border bg-muted/20 hover:bg-muted/50 transition-colors"
              >
                <div className="w-6 h-6 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-semibold shrink-0 mt-0.5">
                  {idx + 1}
                </div>
                <div className="min-w-0">
                  <p className="text-xs font-semibold text-foreground">{step.label}</p>
                  <p className="text-[11px] text-muted-foreground mt-0.5">{step.desc}</p>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
