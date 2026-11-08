using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InternalMaintenance.Maintenance.Domain;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Maintenance.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternalMaintenance.Maintenance.Infrastructure;

/// <summary>
/// Lỗi gửi không thể tự hết bằng retry (thiếu cấu hình hoặc Resend từ chối request).
/// Worker dùng kiểu lỗi này để đưa outbox vào trạng thái thất bại cần người vận hành xử lý.
/// </summary>
public sealed class PermanentEmailDeliveryException(string message) : Exception(message)
{
}

/// <summary>
/// Bộ gửi email phục vụ workflow SLA của Maintenance. Đặt adapter cùng Infrastructure
/// để chi tiết Resend/HTTP không trở thành trách nhiệm của controller hoặc module khác.
/// </summary>
public class ResendEmailService : IEmailService
{
    public const string HttpClientName = "ResendClient";
    private const string ResendApiUrl = "https://api.resend.com/emails";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly IIdentityUserDirectory _identityUserDirectory;

    public ResendEmailService(
        IHttpClientFactory httpClientFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailService> logger,
        IIdentityUserDirectory identityUserDirectory)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _identityUserDirectory = identityUserDirectory;
    }

    private static string FormatVnTime(DateTime? dt)
    {
        if (!dt.HasValue) return "N/A";
        return dt.Value.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    }

    private async Task SendEmailAsync(
        string? toEmail,
        string subject,
        string bodyHtml,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            // Không coi thiếu địa chỉ nhận là gửi thành công; outbox cần lưu lại lỗi để quản trị viên bổ sung email.
            throw new PermanentEmailDeliveryException("The SLA email has no recipient address.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            // Trả về thành công ở đây sẽ khiến outbox đánh dấu email đã gửi dù Resend chưa được gọi.
            // Báo lỗi cấu hình rõ ràng để worker giữ lại thông báo và đưa vào trạng thái cần xử lý.
            throw new PermanentEmailDeliveryException("Resend API key is missing (Resend__ApiKey).");
        }

        var senderName = string.IsNullOrWhiteSpace(_options.FromName) ? "Maintenance System" : _options.FromName.Trim();
        var fromAddress = string.IsNullOrWhiteSpace(_options.FromEmail) ? "no-reply@mail.shopmini.io.vn" : _options.FromEmail.Trim();
        var fromHeader = $"{senderName} <{fromAddress}>";

        var payload = new
        {
            from = fromHeader,
            to = new[] { toEmail.Trim() },
            subject = subject,
            html = bodyHtml
        };

        var jsonBody = JsonSerializer.Serialize(payload);
        const int maxAttempts = 3;
        var delay = TimeSpan.FromMilliseconds(500);
        string? lastHttpStatus = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HttpClientName);

                using var request = new HttpRequestMessage(HttpMethod.Post, ResendApiUrl)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    // Cùng một outbox message có thể được worker gửi lại sau timeout; key giúp Resend nhận ra cùng yêu cầu.
                    request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                }

                using var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Successfully dispatched email to {RecipientMasked} (Subject: {Subject}) via Resend on attempt {Attempt}",
                        MaskEmail(toEmail), subject, attempt);
                    return;
                }

                _logger.LogWarning(
                    "Resend API returned non-success HTTP status {StatusCode} on attempt {Attempt}/{MaxAttempts}",
                    (int)response.StatusCode, attempt, maxAttempts);
                lastHttpStatus = ((int)response.StatusCode).ToString();

                var isRateLimited = (int)response.StatusCode == 429;
                var isConcurrentIdempotentRequest = (int)response.StatusCode == 409 &&
                    await IsConcurrentIdempotentRequestAsync(response, cancellationToken);

                // Phần lớn 4xx là lỗi request/cấu hình cần xử lý; chỉ 429 và 409 do request cùng key
                // còn đang chạy mới được retry. 409 do payload khác nhau cần dừng để Admin kiểm tra.
                if ((int)response.StatusCode >= 400 &&
                    (int)response.StatusCode < 500 &&
                    !isRateLimited &&
                    !isConcurrentIdempotentRequest)
                {
                    // 4xx (ngoại trừ rate limit) thường là payload/credential sai; retry không thể tự sửa.
                    // Đánh dấu lỗi vĩnh viễn để outbox không lặp vô hạn và log không làm lộ nội dung phản hồi.
                    throw new PermanentEmailDeliveryException(
                        $"Resend rejected the email with HTTP status {(int)response.StatusCode}.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Email sending operation canceled for recipient {RecipientMasked}", MaskEmail(toEmail));
                throw;
            }
            catch (PermanentEmailDeliveryException)
            {
                // Lỗi này cần đưa thẳng về outbox để đánh dấu thất bại, retry HTTP tại đây không thể sửa được.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Transient error occurred while sending email to {RecipientMasked} on attempt {Attempt}/{MaxAttempts}",
                    MaskEmail(toEmail), attempt, maxAttempts);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff: 0.5s, 2s
            }
        }

        _logger.LogError("Failed to deliver email to {RecipientMasked} after {MaxAttempts} attempts. Subject: {Subject}",
            MaskEmail(toEmail), maxAttempts, subject);

        // Báo lỗi lên caller để outbox không đánh dấu message là đã gửi thành công.
        // Worker sẽ lưu lỗi và retry ở chu kỳ sau; nếu chỉ log rồi return, outbox sẽ mất dấu thất bại.
        var statusDetails = lastHttpStatus is null ? string.Empty : $" (last HTTP status: {lastHttpStatus})";
        throw new HttpRequestException(
            $"Resend did not accept the email after transient delivery attempts{statusDetails}.");
    }

    private static async Task<bool> IsConcurrentIdempotentRequestAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            // Chỉ đọc mã lỗi để phân loại retry; không ghi body trả về của nhà cung cấp vào log.
            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.TryGetProperty("name", out var name) &&
                   string.Equals(name.GetString(), "concurrent_idempotent_requests", StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task SendNearBreachAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        var recipient = (ticket.AssignedTechnicianId.HasValue
                ? await _identityUserDirectory.FindEmailAsync(ticket.AssignedTechnicianId.Value, cancellationToken)
                : null)
            ?? _options.FallbackTechnicianEmail
            ?? _options.FallbackManagerEmail;

        var subject = $"[CẢNH BÁO SLA] Ticket #{ticket.TicketCode} sắp trễ hạn xử lý";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #ea580c; border-bottom: 2px solid #ea580c; padding-bottom: 8px;'>⚠️ Cảnh Báo Hạn Chót SLA</h3>
                <p>Kính gửi Kỹ thuật viên / Quản lý,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> đang tiến gần đến hạn chót cam kết SLA.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Độ ưu tiên:</td><td style='padding: 8px;'>{ticket.Priority}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Hạn xử lý (Due At):</td><td style='padding: 8px; color: #ea580c; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Vui lòng ưu tiên xử lý ticket này để đảm bảo tiêu chuẩn SLA của hệ thống.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, idempotencyKey, cancellationToken);
    }

    public async Task SendBreachedAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        var recipient = (ticket.AssignedTechnicianId.HasValue
                ? await _identityUserDirectory.FindEmailAsync(ticket.AssignedTechnicianId.Value, cancellationToken)
                : null)
            ?? _options.FallbackManagerEmail
            ?? _options.FallbackAdminEmail;

        var subject = $"[VI PHẠM SLA] Ticket #{ticket.TicketCode} ĐÃ QUÁ HẠN XỬ LÝ";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #dc2626; border-bottom: 2px solid #dc2626; padding-bottom: 8px;'>🚨 Vi Phạm Cam Kết SLA</h3>
                <p>Kính gửi Quản lý & Kỹ thuật viên,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> hiện tại <b>ĐÃ VƯỢT QUÁ HẠN XỬ LÝ SLA</b>.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Độ ưu tiên:</td><td style='padding: 8px;'>{ticket.Priority}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Hạn xử lý (Due At):</td><td style='padding: 8px; color: #dc2626; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Yêu cầu bộ phận kỹ thuật cập nhật trạng thái hoặc điều phối phương án xử lý ngay lập tức.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, idempotencyKey, cancellationToken);
    }

    public async Task SendEscalationAlertAsync(
        MaintenanceTicket ticket,
        CancellationToken cancellationToken = default,
        string? idempotencyKey = null)
    {
        var technicianName = ticket.AssignedTechnicianId.HasValue
            ? await _identityUserDirectory.FindDisplayNameAsync(ticket.AssignedTechnicianId.Value, cancellationToken)
            : null;
        var recipient = _options.FallbackAdminEmail
            ?? _options.FallbackManagerEmail;

        var subject = $"[LEO THANG KHẨN CẤP] Ticket #{ticket.TicketCode} vi phạm SLA nghiêm trọng";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: #7f1d1d; border-bottom: 2px solid #7f1d1d; padding-bottom: 8px;'>🔥 CẢNH BÁO LEO THANG (ESCALATION)</h3>
                <p>Kính gửi Quản trị viên & Ban Quản lý,</p>
                <p>Ticket <b>#{ticket.TicketCode}</b> đã quá hạn SLA hơn <b>2 giờ</b> và chưa hoàn tất xử lý.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Tiêu đề:</td><td style='padding: 8px;'>{ticket.Title}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Kỹ thuật viên phụ trách:</td><td style='padding: 8px;'>{technicianName ?? "Chưa phân công"}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Hạn xử lý:</td><td style='padding: 8px; color: #7f1d1d; font-weight: bold;'>{FormatVnTime(ticket.DueAt)}</td></tr>
                </table>
                <p>Vui lòng can thiệp trực tiếp để giải quyết sự cố.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, idempotencyKey, cancellationToken);
    }

    public async Task SendSlaResultAsync(MaintenanceTicket ticket, bool isMet, CancellationToken cancellationToken = default)
    {
        var recipient = await _identityUserDirectory.FindEmailAsync(ticket.CreatedByUserId, cancellationToken);

        var requesterName = await _identityUserDirectory.FindDisplayNameAsync(ticket.CreatedByUserId, cancellationToken);

        var statusText = isMet ? "ĐẠT CHUẨN SLA (MET)" : "QUÁ HẠN SLA (MISSED)";
        var color = isMet ? "#16a34a" : "#dc2626";
        var subject = $"[KẾT QUẢ XỬ LÝ] Ticket #{ticket.TicketCode} đã hoàn tất - {statusText}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h3 style='color: {color}; border-bottom: 2px solid {color}; padding-bottom: 8px;'>Thông Báo Hoàn Tất Ticket</h3>
                <p>Kính gửi <b>{requesterName ?? "Người yêu cầu"}</b>,</p>
                <p>Yêu cầu bảo trì <b>#{ticket.TicketCode}</b> - <i>{ticket.Title}</i> đã được kỹ thuật viên xử lý hoàn tất.</p>
                <table style='width: 100%; max-width: 500px; border-collapse: collapse; margin: 16px 0;'>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Mã Ticket:</td><td style='padding: 8px;'>{ticket.TicketCode}</td></tr>
                    <tr><td style='padding: 8px; font-weight: bold;'>Đánh giá SLA:</td><td style='padding: 8px; color: {color}; font-weight: bold;'>{statusText}</td></tr>
                    <tr style='background: #f8fafc;'><td style='padding: 8px; font-weight: bold;'>Ghi chú xử lý:</td><td style='padding: 8px;'>{ticket.ResolutionNote ?? "Đã hoàn thành"}</td></tr>
                </table>
                <p>Cảm ơn bạn đã sử dụng hệ thống bảo trì thiết bị văn phòng.</p>
                <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                <p style='font-size: 12px; color: #64748b;'>Hệ thống Quản lý Bảo trì Thiết bị Văn phòng</p>
            </div>";

        await SendEmailAsync(recipient, subject, body, cancellationToken: cancellationToken);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 2) return "***" + email[Math.Max(0, atIndex)..];
        return email[..2] + "***" + email[atIndex..];
    }
}
