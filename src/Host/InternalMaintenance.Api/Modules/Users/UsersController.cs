using InternalMaintenance.BuildingBlocks.Pagination;
using InternalMaintenance.Identity.Application;
using InternalMaintenance.Identity.Contracts;
using InternalMaintenance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalMaintenance.Api.Modules.Users;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IUserAdministrationService userAdministration,
    CurrentUserService currentUserService) : ControllerBase
{
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken)
    {
        var users = await userAdministration.GetPageAsync(query, cancellationToken);
        return Ok(users);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUserById(int id, CancellationToken cancellationToken)
    {
        var user = await userAdministration.GetByIdAsync(id, cancellationToken);
        return user is null
            ? NotFound(new { message = "User not found" })
            : Ok(user);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministration.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return CreatedAtAction(nameof(GetUsers), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministration.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<UserResponse>> UpdateUserStatus(
        int id,
        UpdateUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministration.SetActiveAsync(
            id, request.IsActive, currentUserService.UserId, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("{id:int}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(
        int id,
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userAdministration.ResetPasswordAsync(
            id, request.TemporaryPassword, currentUserService.UserId, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(new { message = "User password has been reset successfully." });
    }
}
