using InternalMaintenance.Api.Modules.TicketAttachments.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.TicketAttachments;

[ApiController]
[Route("api/tickets/{ticketId:int}/attachments")]
public class TicketAttachmentsController : ControllerBase
{
    private readonly ITicketAttachmentsService _ticketAttachmentsService;

    public TicketAttachmentsController(ITicketAttachmentsService ticketAttachmentsService)
    {
        _ticketAttachmentsService = ticketAttachmentsService;
    }

    [Authorize]
    [HttpPost("presign")]
    public async Task<ActionResult<PresignAttachmentResponse>> CreatePresign(
        int ticketId,
        PresignAttachmentRequest request)
    {
        var result = await _ticketAttachmentsService.CreatePresignAsync(ticketId, request);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("confirm")]
    public async Task<ActionResult<TicketAttachmentResponse>> Confirm(
        int ticketId,
        ConfirmAttachmentRequest request)
    {
        var result = await _ticketAttachmentsService.ConfirmAsync(ticketId, request);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<TicketAttachmentResponse>>> GetByTicketId(int ticketId)
    {
        var result = await _ticketAttachmentsService.GetByTicketIdAsync(ticketId);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("{attachmentId:int}/download-url")]
    public async Task<ActionResult<string>> GetDownloadUrl(int ticketId, int attachmentId)
    {
        var result = await _ticketAttachmentsService.GetDownloadUrlAsync(ticketId, attachmentId);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("{attachmentId:int}")]
    public async Task<IActionResult> Delete(int ticketId, int attachmentId)
    {
        var result = await _ticketAttachmentsService.DeleteAsync(ticketId, attachmentId);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return NoContent();
    }
}
