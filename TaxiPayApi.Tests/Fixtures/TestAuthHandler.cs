using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaxiPayApi.Tests.Fixtures;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If explicitly requested unauthenticated, skip auth
        if (Request.Headers.TryGetValue("X-Test-Unauthenticated", out var unauthVal) &&
            bool.TryParse(unauthVal, out var isUnauth) && isUnauth)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // If no test role header and no auth header, return NoResult (unauthenticated)
        if (!Request.Headers.TryGetValue("X-Test-Role", out var roleVal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = roleVal.ToString();
        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var uidVal)
            ? uidVal.ToString()
            : Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        if (Request.Headers.TryGetValue("X-Test-Name", out var nameVal))
        {
            claims.Add(new(ClaimTypes.Name, nameVal.ToString()));
        }
        else
        {
            claims.Add(new(ClaimTypes.Name, $"Test {role}"));
        }

        if (Request.Headers.TryGetValue("X-Test-Phone", out var phoneVal))
        {
            claims.Add(new(ClaimTypes.MobilePhone, phoneVal.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-PassengerId", out var passengerIdVal))
        {
            claims.Add(new("passengerId", passengerIdVal.ToString()));
            claims.Add(new("passengerGuid", passengerIdVal.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-DriverId", out var driverIdVal))
        {
            claims.Add(new("driverGuid", driverIdVal.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-DriverCode", out var driverCodeVal))
        {
            claims.Add(new("driverId", driverCodeVal.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-AdminId", out var adminIdVal))
        {
            claims.Add(new("adminGuid", adminIdVal.ToString()));
            claims.Add(new("adminId", adminIdVal.ToString()));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
