namespace InternalMaintenance.Maintenance.Domain;

// Persisted string values retained for compatibility with existing SQL rows and API responses.
public static class TicketStatuses
{
    public const string Pending = "Pending";
    public const string Assigned = "Assigned";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
    public const string WaitingForVendor = "WaitingForVendor";
}

public static class TicketPriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}
