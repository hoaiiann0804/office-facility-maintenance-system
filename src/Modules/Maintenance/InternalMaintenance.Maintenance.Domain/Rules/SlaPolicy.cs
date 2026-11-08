namespace InternalMaintenance.Maintenance.Domain;

public static class SlaPolicy
{
    // Ngân sách SLA tính theo priority; pure rule để controller/worker gọi chung.
    public static readonly IReadOnlyDictionary<string, int> SlaHoursMap =
        new Dictionary<string, int>
        {
            [TicketPriorities.Critical] = 2,
            [TicketPriorities.High] = 8,
            [TicketPriorities.Medium] = 24,
            [TicketPriorities.Low] = 48
        };

    public const string InSLA = "InSLA";
    public const string NearBreach = "NearBreach";
    public const string Breached = "Breached";
    public const string MetSLA = "MetSLA";
    public const string MissedSLA = "MissedSLA";
    public const string Paused = "Paused";

    public static DateTime CalculateDueAt(DateTime createdAt, string priority)
    {
        var hours = SlaHoursMap.GetValueOrDefault(priority, 24);
        return createdAt.AddHours(hours);
    }

    // Dùng khi đồng hồ còn chạy. Ticket đã resolve phải chốt theo ResolvedAt.
    public static string GetActiveStatus(DateTime dueAt, DateTime now)
    {
        if (now > dueAt) return Breached;
        return dueAt - now <= TimeSpan.FromHours(1) ? NearBreach : InSLA;
    }
}
