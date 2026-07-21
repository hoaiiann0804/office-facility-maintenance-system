namespace InternalMaintenance.Api.Modules.TicketAttachments.Storage;

// Dùng cấu hình cho Storage
public sealed class R2AttachmentStorageOptions
{
    // Định danh account R2
    public string AccountId { get; set; } = string.Empty;
    // Tên bucket chứa file
    public string BucketName { get; set; } = string.Empty;
    // Key public để request
    public string AccessKeyId { get; set; } = string.Empty;
    //Secret để tạo chữ ký
    public string SecretAccessKey { get; set; } = string.Empty;
    // URL endpoint của R2 
    public string Endpoint { get; set; } = string.Empty;
    // Vùng ký AWS-compatible mặc địn "auto"
    public string Region { get; set; } = "auto";
    // URL upload/download tồn tại bao lâu
    public int PresignExpiresMinutes { get; set; } = 15;
}
