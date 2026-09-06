using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    public Guid? DriverGuid
    {
        get
        {
            var val = User?.FindFirstValue("driverGuid");
            return Guid.TryParse(val, out var guid) ? guid : null;
        }
    }

    public string? DriverCode => User?.FindFirstValue("driverId");

    public Guid? PassengerGuid
    {
        get
        {
            var val = User?.FindFirstValue("passengerId");
            return Guid.TryParse(val, out var guid) ? guid : null;
        }
    }

    public Guid? AdminGuid
    {
        get
        {
            var val = User?.FindFirstValue("adminGuid");
            return Guid.TryParse(val, out var guid) ? guid : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin =>
        User != null && (
            User.IsInRole("Admin") ||
            User.IsInRole("admin") ||
            User.IsInRole("SuperAdmin") ||
            User.IsInRole("superadmin") ||
            string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
        );

    public bool IsDriver =>
        User != null && (
            User.IsInRole("Driver") ||
            User.IsInRole("driver") ||
            string.Equals(Role, "Driver", StringComparison.OrdinalIgnoreCase)
        );

    public bool IsPassenger =>
        User != null && (
            User.IsInRole("Passenger") ||
            User.IsInRole("passenger") ||
            string.Equals(Role, "Passenger", StringComparison.OrdinalIgnoreCase)
        );
}
