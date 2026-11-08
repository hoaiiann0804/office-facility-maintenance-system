using System.Collections.Generic;

namespace InternalMaintenance.Api.Modules.Dashboard.Contracts;

public class ChartDataResponse
{
    public List<ChartItem> TicketsByStatus { get; set; } = new();
    public List<ChartItem> TicketsByPriority { get; set; } = new();
    public List<ChartItem> EquipmentByDepartment { get; set; } = new();
}

public class ChartItem
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}