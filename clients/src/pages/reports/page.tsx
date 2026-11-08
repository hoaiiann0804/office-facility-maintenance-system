import { useState } from "react";
import { useAuthStore } from "@/features/auth/model/auth-store";
import {
  useTechnicianPerformanceQuery,
  useSlaComplianceQuery,
  useMaintenanceCostQuery,
  downloadExcelReport,
  type ReportFilterQuery,
} from "@/features/reports/api/reports-api";
import { useDepartmentsQuery } from "@/features/equipment/api/use-departments-query";
import { useUsersQuery } from "@/features/tickets/api/use-users-query";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  FileSpreadsheet,
  Printer,
  Calendar,
  Filter,
  Users,
  DollarSign,
  TrendingUp,
  Clock,
  Wrench,
  Building2,
  Award,
} from "lucide-react";
import { toast } from "sonner";
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

const COLORS = ["#0284C7", "#059669", "#D97706", "#DC2626", "#7C3AED", "#EC4899"];

export function ReportsPage() {
  const session = useAuthStore((state) => state.session);
  const role = session?.user.roleName;

  const [activeTab, setActiveTab] = useState<"tech" | "sla" | "cost">("tech");
  const [datePreset, setDatePreset] = useState<
    "all" | "thisMonth" | "lastMonth" | "thisQuarter" | "thisYear"
  >("all");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [departmentId, setDepartmentId] = useState<string>("");
  const [technicianId, setTechnicianId] = useState<string>("");
  const [isExporting, setIsExporting] = useState(false);

  const { data: deptData } = useDepartmentsQuery({ pageSize: 100 });
  const departments = deptData?.items ?? [];

  const { data: techData } = useUsersQuery({ role: "Technician", pageSize: 100 });
  const technicians = techData?.items ?? [];

  const handlePresetChange = (
    preset: "all" | "thisMonth" | "lastMonth" | "thisQuarter" | "thisYear",
  ) => {
    setDatePreset(preset);
    const now = new Date();

    if (preset === "all") {
      setFromDate("");
      setToDate("");
    } else if (preset === "thisMonth") {
      const first = new Date(now.getFullYear(), now.getMonth(), 1);
      setFromDate(first.toISOString().split("T")[0]);
      setToDate(now.toISOString().split("T")[0]);
    } else if (preset === "lastMonth") {
      const first = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const last = new Date(now.getFullYear(), now.getMonth(), 0);
      setFromDate(first.toISOString().split("T")[0]);
      setToDate(last.toISOString().split("T")[0]);
    } else if (preset === "thisQuarter") {
      const qMonth = Math.floor(now.getMonth() / 3) * 3;
      const first = new Date(now.getFullYear(), qMonth, 1);
      setFromDate(first.toISOString().split("T")[0]);
      setToDate(now.toISOString().split("T")[0]);
    } else if (preset === "thisYear") {
      const first = new Date(now.getFullYear(), 0, 1);
      setFromDate(first.toISOString().split("T")[0]);
      setToDate(now.toISOString().split("T")[0]);
    }
  };

  const filter: ReportFilterQuery = {
    fromDate: fromDate ? new Date(fromDate).toISOString() : undefined,
    toDate: toDate ? new Date(toDate + "T23:59:59").toISOString() : undefined,
    departmentId: departmentId ? Number(departmentId) : undefined,
    technicianId: technicianId ? Number(technicianId) : undefined,
  };

  const { data: techReports = [], isLoading: isTechLoading } =
    useTechnicianPerformanceQuery(filter);
  const { data: slaReport } = useSlaComplianceQuery(filter);
  const { data: costReport } = useMaintenanceCostQuery(filter);

  const handleExportExcel = async () => {
    try {
      setIsExporting(true);
      await downloadExcelReport(filter);
      toast.success("Xuất file Excel báo cáo thành công!");
    } catch (error) {
      console.error("Failed to export Excel report:", error);
      toast.error("Không thể xuất file Excel.");
    } finally {
      setIsExporting(false);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  const formatVND = (val: number) => {
    return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(val);
  };

  if (role !== "Admin" && role !== "Manager") {
    return (
      <div className="p-8 text-center text-muted-foreground">
        Bạn không có quyền truy cập vào phân hệ Báo cáo & Thống kê.
      </div>
    );
  }

  return (
    <div className="space-y-6 pb-12">
      {/* Header & Quick Action Buttons */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between print:hidden">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground">
            Báo Cáo & Thống Kê Quản Trị
          </h1>
          <p className="text-sm text-muted-foreground">
            Phân tích hiệu suất kỹ thuật viên, tỷ lệ vi phạm SLA và chi phí bảo dưỡng thiết bị
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={handlePrint} className="gap-1.5 shadow-sm">
            <Printer className="h-4 w-4" />
            <span>In / Xuất PDF</span>
          </Button>

          <Button
            size="sm"
            onClick={handleExportExcel}
            disabled={isExporting}
            className="gap-1.5 bg-emerald-600 hover:bg-emerald-700 text-white shadow-sm"
          >
            <FileSpreadsheet className="h-4 w-4" />
            <span>{isExporting ? "Đang xuất Excel..." : "Xuất File Excel (.xlsx)"}</span>
          </Button>
        </div>
      </div>

      {/* Filter Toolbar */}
      <Card className="print:hidden border-border/60 shadow-sm bg-card/50 backdrop-blur">
        <CardHeader className="pb-3 pt-4">
          <CardTitle className="text-xs font-semibold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
            <Filter className="h-3.5 w-3.5" />
            <span>Bộ Lọc Báo Cáo</span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 pb-4">
          {/* Presets */}
          <div className="flex flex-wrap items-center gap-1.5 text-xs">
            <span className="text-muted-foreground mr-1 font-medium">Mốc thời gian:</span>
            {[
              { id: "all", label: "Toàn bộ" },
              { id: "thisMonth", label: "Tháng này" },
              { id: "lastMonth", label: "Tháng trước" },
              { id: "thisQuarter", label: "Quý này" },
              { id: "thisYear", label: "Năm nay" },
            ].map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() =>
                  handlePresetChange(
                    p.id as "all" | "thisMonth" | "lastMonth" | "thisQuarter" | "thisYear",
                  )
                }
                className={`px-2.5 py-1 rounded-md transition-colors ${
                  datePreset === p.id
                    ? "bg-primary text-primary-foreground font-semibold"
                    : "bg-muted hover:bg-muted/80 text-foreground"
                }`}
              >
                {p.label}
              </button>
            ))}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3 pt-1">
            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                <Calendar className="h-3.5 w-3.5" /> Từ ngày
              </label>
              <input
                type="date"
                value={fromDate}
                max={new Date().toISOString().split("T")[0]}
                onChange={(e) => {
                  const val = e.target.value;
                  const todayStr = new Date().toISOString().split("T")[0];
                  if (val > todayStr) {
                    toast.error("Không thể chọn ngày trong tương lai.");
                    return;
                  }
                  if (toDate && val > toDate) {
                    toast.error("'Từ ngày' không được lớn hơn 'Đến ngày'.");
                    return;
                  }
                  setFromDate(val);
                  setDatePreset("all");
                }}
                className="flex h-8 w-full rounded-md border border-input bg-background px-2.5 py-1 text-xs shadow-sm"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                <Calendar className="h-3.5 w-3.5" /> Đến ngày
              </label>
              <input
                type="date"
                value={toDate}
                max={new Date().toISOString().split("T")[0]}
                onChange={(e) => {
                  const val = e.target.value;
                  const todayStr = new Date().toISOString().split("T")[0];
                  if (val > todayStr) {
                    toast.error("Không thể chọn ngày trong tương lai.");
                    return;
                  }
                  setToDate(val);
                  setDatePreset("all");
                }}
                className="flex h-8 w-full rounded-md border border-input bg-background px-2.5 py-1 text-xs shadow-sm"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                <Building2 className="h-3.5 w-3.5" /> Phòng ban
              </label>
              <select
                value={departmentId}
                onChange={(e) => setDepartmentId(e.target.value)}
                className="flex h-8 w-full rounded-md border border-input bg-background px-2.5 py-1 text-xs shadow-sm"
              >
                <option value="">-- Tất cả phòng ban --</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1">
                <Users className="h-3.5 w-3.5" /> Kỹ thuật viên
              </label>
              <select
                value={technicianId}
                onChange={(e) => setTechnicianId(e.target.value)}
                className="flex h-8 w-full rounded-md border border-input bg-background px-2.5 py-1 text-xs shadow-sm"
              >
                <option value="">-- Tất cả kỹ thuật viên --</option>
                {technicians.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.fullName}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabs */}
      <div className="flex border-b border-border/80 print:hidden gap-2">
        <button
          type="button"
          onClick={() => setActiveTab("tech")}
          className={`pb-2.5 px-3 text-sm font-semibold transition-all border-b-2 flex items-center gap-1.5 ${
            activeTab === "tech"
              ? "border-primary text-primary"
              : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          <Award className="h-4 w-4" />
          <span>Hiệu Suất Kỹ Thuật Viên</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("sla")}
          className={`pb-2.5 px-3 text-sm font-semibold transition-all border-b-2 flex items-center gap-1.5 ${
            activeTab === "sla"
              ? "border-primary text-primary"
              : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          <Clock className="h-4 w-4" />
          <span>Tuân Thủ & Vi Phạm SLA</span>
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("cost")}
          className={`pb-2.5 px-3 text-sm font-semibold transition-all border-b-2 flex items-center gap-1.5 ${
            activeTab === "cost"
              ? "border-primary text-primary"
              : "border-transparent text-muted-foreground hover:text-foreground"
          }`}
        >
          <DollarSign className="h-4 w-4" />
          <span>Chi Phí Bảo Dưỡng</span>
        </button>
      </div>

      {/* ─────────────────────────────────────────────────────────────
          TAB 1: HIỆU SUẤT KỸ THUẬT VIÊN
      ───────────────────────────────────────────────────────────── */}
      {activeTab === "tech" && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card className="border-l-4 border-l-blue-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">Tổng Kỹ Thuật Viên</div>
                <div className="text-2xl font-bold mt-1 text-foreground">{techReports.length}</div>
                <p className="text-xs text-muted-foreground mt-0.5">Đang quản lý trong đội ngũ</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-emerald-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Tổng Lượt Xử Lý Xong
                </div>
                <div className="text-2xl font-bold mt-1 text-emerald-600">
                  {techReports.reduce((acc, t) => acc + t.totalResolved, 0)}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Ticket hoàn thành sửa chữa</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-purple-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Tỷ Lệ Đạt SLA Trung Bình
                </div>
                <div className="text-2xl font-bold mt-1 text-purple-600">
                  {techReports.length > 0
                    ? Math.round(
                        (techReports.reduce((acc, t) => acc + t.slaComplianceRate, 0) /
                          techReports.length) *
                          10,
                      ) / 10
                    : 100}
                  %
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Cam kết tiến độ toàn đội</p>
              </CardContent>
            </Card>
          </div>

          {/* Chart: Tickets Completed & MTTR */}
          <Card className="shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-semibold flex items-center gap-2">
                <TrendingUp className="h-4 w-4 text-primary" />
                <span>So Sánh Tỷ Lệ Đạt SLA Giữa Các Kỹ Thuật Viên (%)</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="h-72">
              {techReports.length > 0 ? (
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={techReports} margin={{ top: 10, right: 30, left: 0, bottom: 20 }}>
                    <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                    <XAxis dataKey="fullName" tick={{ fontSize: 11 }} />
                    <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
                    <Tooltip formatter={(val) => [`${val}%`, "Tỷ Lệ Đạt SLA"]} />
                    <Bar
                      dataKey="slaComplianceRate"
                      name="Tỷ Lệ Đạt SLA (%)"
                      fill="#2563EB"
                      radius={[4, 4, 0, 0]}
                    />
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                  Chưa có dữ liệu thống kê
                </div>
              )}
            </CardContent>
          </Card>

          {/* Table */}
          <Card className="shadow-sm overflow-hidden">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-semibold">
                Bảng Chi Tiết Hiệu Suất Từng Kỹ Thuật Viên
              </CardTitle>
              <CardDescription className="text-xs">
                Số liệu phân công, trạng thái xử lý và thời gian hoàn thành trung bình (MTTR)
              </CardDescription>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left border-collapse">
                  <thead className="bg-muted/60 text-muted-foreground uppercase text-[10px] font-semibold border-y">
                    <tr>
                      <th className="px-4 py-2.5">Kỹ thuật viên</th>
                      <th className="px-4 py-2.5">Phòng ban</th>
                      <th className="px-4 py-2.5 text-center">Được giao</th>
                      <th className="px-4 py-2.5 text-center">Đang làm</th>
                      <th className="px-4 py-2.5 text-center">Gửi Vendor</th>
                      <th className="px-4 py-2.5 text-center">Đã xong</th>
                      <th className="px-4 py-2.5 text-center">Đạt SLA</th>
                      <th className="px-4 py-2.5 text-center">Trễ SLA</th>
                      <th className="px-4 py-2.5 text-center font-bold">Tỷ lệ Đạt SLA</th>
                      <th className="px-4 py-2.5 text-center">Thời gian TB (MTTR)</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {isTechLoading ? (
                      <tr>
                        <td colSpan={10} className="p-6 text-center text-muted-foreground">
                          Đang tải dữ liệu...
                        </td>
                      </tr>
                    ) : techReports.length === 0 ? (
                      <tr>
                        <td colSpan={10} className="p-6 text-center text-muted-foreground">
                          Không tìm thấy kỹ thuật viên nào trong khoảng thời gian này.
                        </td>
                      </tr>
                    ) : (
                      techReports.map((t) => (
                        <tr key={t.technicianId} className="hover:bg-muted/30 transition-colors">
                          <td className="px-4 py-3">
                            <div className="font-semibold text-foreground">{t.fullName}</div>
                            <div className="text-[10px] text-muted-foreground">{t.email}</div>
                          </td>
                          <td className="px-4 py-3 text-muted-foreground">{t.departmentName}</td>
                          <td className="px-4 py-3 text-center font-semibold">{t.totalAssigned}</td>
                          <td className="px-4 py-3 text-center text-blue-600">
                            {t.inProgressCount}
                          </td>
                          <td className="px-4 py-3 text-center text-amber-600">
                            {t.waitingForVendorCount}
                          </td>
                          <td className="px-4 py-3 text-center text-emerald-600 font-semibold">
                            {t.totalResolved}
                          </td>
                          <td className="px-4 py-3 text-center text-emerald-600">
                            {t.metSlaCount}
                          </td>
                          <td className="px-4 py-3 text-center text-destructive font-semibold">
                            {t.missedSlaCount}
                          </td>
                          <td className="px-4 py-3 text-center">
                            <span
                              className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-bold ${
                                t.slaComplianceRate >= 90
                                  ? "bg-emerald-500/10 text-emerald-600"
                                  : t.slaComplianceRate >= 75
                                    ? "bg-amber-500/10 text-amber-600"
                                    : "bg-destructive/10 text-destructive"
                              }`}
                            >
                              {t.slaComplianceRate}%
                            </span>
                          </td>
                          <td className="px-4 py-3 text-center font-medium">
                            {t.avgResolutionHours > 0 ? `${t.avgResolutionHours} giờ` : "-"}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* ─────────────────────────────────────────────────────────────
          TAB 2: TUÂN THỦ & VI PHẠM SLA
      ───────────────────────────────────────────────────────────── */}
      {activeTab === "sla" && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
            <Card className="border-l-4 border-l-blue-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">Tổng Số Ticket</div>
                <div className="text-2xl font-bold mt-1 text-foreground">
                  {slaReport?.totalTickets ?? 0}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Tất cả ticket theo bộ lọc</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-emerald-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Đạt Đúng Hạn (Met SLA)
                </div>
                <div className="text-2xl font-bold mt-1 text-emerald-600">
                  {slaReport?.metSlaCount ?? 0}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Xử lý trước thời hạn cam kết</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-rose-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Vi Phạm Hạn (Missed SLA)
                </div>
                <div className="text-2xl font-bold mt-1 text-destructive">
                  {slaReport?.missedSlaCount ?? 0}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Quá thời gian quy định</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-purple-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Tỷ Lệ Tuân Thủ Chung
                </div>
                <div className="text-2xl font-bold mt-1 text-purple-600">
                  {slaReport?.overallComplianceRate ?? 100}%
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Mục tiêu doanh nghiệp &ge; 90%
                </p>
              </CardContent>
            </Card>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Chart: SLA Trends over Month */}
            <Card className="shadow-sm">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-semibold">
                  Xu Hướng Tỷ Lệ Đạt SLA Qua Từng Tháng (%)
                </CardTitle>
              </CardHeader>
              <CardContent className="h-64">
                {slaReport?.monthlyTrends && slaReport.monthlyTrends.length > 0 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={slaReport.monthlyTrends}>
                      <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                      <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                      <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
                      <Tooltip formatter={(val) => [`${val}%`, "Tỷ lệ đạt SLA"]} />
                      <Line
                        type="monotone"
                        dataKey="complianceRate"
                        name="Tỷ lệ đạt (%)"
                        stroke="#059669"
                        strokeWidth={2.5}
                        dot={{ r: 4 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                    Chưa có dữ liệu xu hướng
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Chart: SLA Breakdown by Priority */}
            <Card className="shadow-sm">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-semibold">
                  Tỷ Lệ Tuân Thủ SLA Theo Mức Độ Ưu Tiên
                </CardTitle>
              </CardHeader>
              <CardContent className="h-64">
                {slaReport?.byPriority && slaReport.byPriority.length > 0 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={slaReport.byPriority}>
                      <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                      <XAxis dataKey="priority" tick={{ fontSize: 11 }} />
                      <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
                      <Tooltip formatter={(val) => [`${val}%`, "Tỷ lệ đạt"]} />
                      <Bar
                        dataKey="complianceRate"
                        name="Tỷ lệ Đạt SLA (%)"
                        fill="#7C3AED"
                        radius={[4, 4, 0, 0]}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                    Chưa có dữ liệu
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {/* ─────────────────────────────────────────────────────────────
          TAB 3: CHI PHÍ BẢO DƯỠNG
      ───────────────────────────────────────────────────────────── */}
      {activeTab === "cost" && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card className="border-l-4 border-l-emerald-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Tổng Chi Phí Bảo Trì Ngoại Viện
                </div>
                <div className="text-2xl font-bold mt-1 text-emerald-600">
                  {formatVND(costReport?.totalMaintenanceCost ?? 0)}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Tổng số tiền trả cho Vendor</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-blue-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Tổng Lượt Gửi Đối Tác
                </div>
                <div className="text-2xl font-bold mt-1 text-foreground">
                  {costReport?.totalVendorDispatches ?? 0} lượt
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Số lần gửi bảo hành ra ngoài</p>
              </CardContent>
            </Card>

            <Card className="border-l-4 border-l-amber-500 shadow-sm">
              <CardContent className="p-4">
                <div className="text-xs font-medium text-muted-foreground">
                  Chi Phí Trung Bình / Lượt
                </div>
                <div className="text-2xl font-bold mt-1 text-amber-600">
                  {formatVND(costReport?.avgCostPerTicket ?? 0)}
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">Bình quân mỗi lần sửa chữa</p>
              </CardContent>
            </Card>
          </div>

          {/* Charts: Monthly Cost Trend & Department Breakdown */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card className="shadow-sm">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-semibold">
                  Xu Hướng Chi Phí Bảo Dưỡng Theo Tháng (VNĐ)
                </CardTitle>
              </CardHeader>
              <CardContent className="h-64">
                {costReport?.monthlyCosts && costReport.monthlyCosts.length > 0 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={costReport.monthlyCosts}>
                      <CartesianGrid strokeDasharray="3 3" opacity={0.3} />
                      <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                      <YAxis tick={{ fontSize: 11 }} tickFormatter={(val) => `${val / 1000000}M`} />
                      <Tooltip formatter={(val) => [formatVND(Number(val)), "Chi phí"]} />
                      <Bar
                        dataKey="totalCost"
                        name="Chi phí (VNĐ)"
                        fill="#059669"
                        radius={[4, 4, 0, 0]}
                      />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                    Chưa có phát sinh chi phí
                  </div>
                )}
              </CardContent>
            </Card>

            <Card className="shadow-sm">
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-semibold">
                  Chi Phí Phân Bổ Theo Phòng Ban
                </CardTitle>
              </CardHeader>
              <CardContent className="h-64">
                {costReport?.departmentCosts && costReport.departmentCosts.length > 0 ? (
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={costReport.departmentCosts}
                        dataKey="totalCost"
                        nameKey="departmentName"
                        cx="50%"
                        cy="50%"
                        outerRadius={80}
                        // eslint-disable-next-line @typescript-eslint/no-explicit-any
                        label={(entry: any) =>
                          `${entry.departmentName}: ${Math.round(entry.totalCost / 1000000)}M`
                        }
                      >
                        {costReport.departmentCosts.map((_, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip formatter={(val) => [formatVND(Number(val)), "Chi phí"]} />
                    </PieChart>
                  </ResponsiveContainer>
                ) : (
                  <div className="h-full flex items-center justify-center text-xs text-muted-foreground">
                    Chưa có phát sinh chi phí
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Top Costly Equipment Table */}
          <Card className="shadow-sm">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-semibold flex items-center gap-2">
                <Wrench className="h-4 w-4 text-amber-500" />
                <span>Top Thiết Bị Chi Phí Sửa Chữa Cao Nhất (Cảnh Báo Thay Thế)</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-xs text-left border-collapse">
                  <thead className="bg-muted/60 text-muted-foreground uppercase text-[10px] font-semibold border-y">
                    <tr>
                      <th className="px-4 py-2.5">Mã thiết bị</th>
                      <th className="px-4 py-2.5">Tên thiết bị</th>
                      <th className="px-4 py-2.5">Phòng ban quản lý</th>
                      <th className="px-4 py-2.5 text-center">Số lần sửa</th>
                      <th className="px-4 py-2.5 text-right font-bold">Tổng tiền sửa (VNĐ)</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {costReport?.topCostlyEquipment && costReport.topCostlyEquipment.length > 0 ? (
                      costReport.topCostlyEquipment.map((eq) => (
                        <tr key={eq.equipmentId} className="hover:bg-muted/30">
                          <td className="px-4 py-3 font-mono font-semibold text-primary">
                            {eq.equipmentCode}
                          </td>
                          <td className="px-4 py-3 font-medium text-foreground">
                            {eq.equipmentName}
                          </td>
                          <td className="px-4 py-3 text-muted-foreground">{eq.departmentName}</td>
                          <td className="px-4 py-3 text-center">{eq.maintenanceCount}</td>
                          <td className="px-4 py-3 text-right font-bold text-destructive">
                            {formatVND(eq.totalRepairCost)}
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} className="p-6 text-center text-muted-foreground">
                          Chưa có thiết bị nào phát sinh chi phí sửa chữa.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
