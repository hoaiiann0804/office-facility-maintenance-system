using System.ComponentModel.DataAnnotations;

namespace InternalMaintenance.Vendors.Contracts;

public class UpdateVendorRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên người liên hệ là bắt buộc")]
    [StringLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
