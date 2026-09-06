using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Security.Requirements;

namespace TaxiPayApi.Infrastructure.Security.Handlers;

/// <summary>
/// Authorizes that the caller is the driver associated with a given DriverShift, or is an Administrator.
/// </summary>
public class DriverOwnsTripAuthorizationHandler
    : AuthorizationHandler<DriverOwnsTripRequirement, DriverShift>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DriverOwnsTripRequirement requirement,
        DriverShift resource)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var driverGuidClaim = context.User.FindFirstValue("driverGuid");
        if (Guid.TryParse(driverGuidClaim, out var callerDriverGuid))
        {
            if (resource.DriverId == callerDriverGuid)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorizes that the caller matches the driver identifier, or is an Administrator.
/// </summary>
public class DriverResourceAuthorizationHandler
    : AuthorizationHandler<DriverOwnsResourceRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DriverOwnsResourceRequirement requirement,
        Guid targetDriverId)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var driverGuidClaim = context.User.FindFirstValue("driverGuid");
        if (Guid.TryParse(driverGuidClaim, out var callerDriverGuid))
        {
            if (targetDriverId == callerDriverGuid)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorizes that the caller matches the passenger identifier, or is an Administrator.
/// </summary>
public class PassengerResourceAuthorizationHandler
    : AuthorizationHandler<PassengerOwnsResourceRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PassengerOwnsResourceRequirement requirement,
        Guid targetPassengerId)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var passengerIdClaim = context.User.FindFirstValue("passengerId")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(passengerIdClaim, out var callerPassengerId))
        {
            if (targetPassengerId == callerPassengerId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Authorizes that the caller is the passenger who owns the PassengerJourney, or is an Administrator.
/// </summary>
public class PassengerJourneyAuthorizationHandler
    : AuthorizationHandler<PassengerOwnsJourneyRequirement, PassengerJourney>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PassengerOwnsJourneyRequirement requirement,
        PassengerJourney journey)
    {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var passengerIdClaim = context.User.FindFirstValue("passengerId")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(passengerIdClaim, out var callerPassengerId))
        {
            if (journey.PassengerId == callerPassengerId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
