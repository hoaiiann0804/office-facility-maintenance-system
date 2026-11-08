namespace InternalMaintenance.Reporting.Contracts;

public class MonthlyCostItem
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int TicketCount { get; set; }
}

public class DepartmentCostItem
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int TicketCount { get; set; }
}

public class VendorCostItem
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int DispatchCount { get; set; }
}

public class CostlyEquipmentItem
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalRepairCost { get; set; }
    public int MaintenanceCount { get; set; }
}

public class MaintenanceCostReportResponse
{
    public decimal TotalMaintenanceCost { get; set; }
    public int TotalVendorDispatches { get; set; }
    public decimal AvgCostPerTicket { get; set; }
    public List<MonthlyCostItem> MonthlyCosts { get; set; } = new();
    public List<DepartmentCostItem> DepartmentCosts { get; set; } = new();
    public List<VendorCostItem> VendorCosts { get; set; } = new();
    public List<CostlyEquipmentItem> TopCostlyEquipment { get; set; } = new();
}
