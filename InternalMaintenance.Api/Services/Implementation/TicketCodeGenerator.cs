using InternalMaintenance.Api.Services.Interface;

namespace InternalMaintenance.Api.Services;

public class TicketCodeGenerator : ITicketCodeGenerator
{
    public string GenerateTicketCode()
    {
        var now = DateTime.UtcNow;

        // Thêm randomPart để giảm nguy cơ trùng mã nếu nhiều người tạo ticket cùng lúc.
        // KHông dùng CountAsync vì 2 request đồng thời có thể cùng count một số thứ tự 
        var randomPart = Guid.NewGuid()
        .ToString("N")
        .Substring(0, 6)
        .ToUpper();

        return $"TICKET-{now:yyyyMMdd-HHmmss}-{randomPart}";

    }

}