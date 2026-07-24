
using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.Modules.Equipment.Contracts;

public class CreateEquipmentRequest
{
    [Required(ErrorMessage = "Equipment code is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Equipment code must be between 2 and 50 characters")]
    public string Code {get;set;} = string.Empty;

    [Required(ErrorMessage = "Equipment name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Equipment name must be between 2 and 150 characters")]
    public string Name {get;set;} = string.Empty;

    // Bắt buộc DeparmemtId > 0 tránh giá trị mặc định là (0) gây ra lỗi khóa ngoại ở DB
    [Range(1, int.MaxValue,  ErrorMessage = "DepartmentId must be greater than 0")]
    public int DepartmentId {get;set;}

    [StringLength(50, ErrorMessage="Status must not exceed 50 characters")]
    public string Status {get;set;} = "Active";

    public DateTime? PurchasedDate {get;set;}

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    public int? MaintenanceDepartmentId { get; set; }
}
