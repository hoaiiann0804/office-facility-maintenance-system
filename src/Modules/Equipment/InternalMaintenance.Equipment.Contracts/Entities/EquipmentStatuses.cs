namespace InternalMaintenance.Assets.Contracts;

// Equipment sở hữu vocabulary trạng thái; module khác dùng hằng số này thay vì phụ thuộc Api.Constants.
public static class EquipmentStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string UnderMaintenance = "UnderMaintenance";
    public const string Retired = "Retired";
}
