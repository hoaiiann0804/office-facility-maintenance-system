using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Api.DTOs.MaintenanceTicket;

public class CreateTicketRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
    public string Description { get; set; } = string.Empty;

    [Range(1,int.MaxValue, ErrorMessage ="EquipmentId must be greater than 0")]
    public int EquipmentId {get;set;}

    [StringLength(20, ErrorMessage = "Priority must not exceed 20 characters")]
    public string Priority { get; set; } = "Medium";

    // // Tạm thời để test khi chưa có authentication
    // // Sau này có login/JWT thì bỏ field này và lấy UserId từ token 
    // [Range(1, int.MaxValue, ErrorMessage = "CreatedByUserId must be greater than 0")]
    public int CreatedByUserId { get; set; }
}