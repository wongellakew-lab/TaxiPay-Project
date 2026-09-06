using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Auth;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<TaxiPayUser> _userManager,
    TaxiPayDbContext _dbContext) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get authenticated user profile")]
    [EndpointDescription("Retrieves the identity and linked domain profile (Passenger, Driver, or Admin) for the currently authenticated user using JWT claims.")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "User identifier claim was not found in the security token.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = "The identity account associated with the token does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "User";

        Guid? passengerId = null;
        decimal? walletBalance = null;
        Guid? driverId = null;
        string? driverCode = null;
        string? driverStatus = null;
        Guid? adminId = null;
        string? adminRole = null;

        if (roles.Contains("Passenger") || primaryRole.Equals("Passenger", StringComparison.OrdinalIgnoreCase))
        {
            var passenger = await _dbContext.Passengers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id || p.Phone == user.PhoneNumber, ct);

            if (passenger != null)
            {
                passengerId = passenger.Id;
                walletBalance = passenger.WalletBalance;
            }
        }
        else if (roles.Contains("Driver") || primaryRole.Equals("Driver", StringComparison.OrdinalIgnoreCase))
        {
            var driver = await _dbContext.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == user.Id || d.Phone == user.PhoneNumber, ct);

            if (driver != null)
            {
                driverId = driver.Id;
                driverCode = driver.DriverId;
                driverStatus = driver.Status.ToString();
            }
        }
        else if (roles.Contains("Admin") || primaryRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admin = await _dbContext.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == user.Id || a.Email.ToLower() == (user.Email ?? string.Empty).ToLower(), ct);

            if (admin != null)
            {
                adminId = admin.Id;
                adminRole = admin.Role.ToString();
            }
        }

        var response = new CurrentUserResponse(
            UserId: user.Id,
            Role: primaryRole,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Email: user.Email,
            PhoneNumber: user.PhoneNumber,
            PassengerId: passengerId,
            WalletBalance: walletBalance,
            DriverId: driverId,
            DriverCode: driverCode,
            DriverStatus: driverStatus,
            AdminId: adminId,
            AdminRole: adminRole
        );

        return Ok(response);
    }
}
