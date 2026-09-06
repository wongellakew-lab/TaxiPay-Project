using Microsoft.AspNetCore.Authorization;

namespace TaxiPayApi.Infrastructure.Security.Requirements;

/// <summary>
/// Requirement: Authenticated caller must own the DriverShift / Trip resource,
/// or must be an Administrator.
/// </summary>
public class DriverOwnsTripRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Requirement: Authenticated caller must match the DriverId of the resource.
/// </summary>
public class DriverOwnsResourceRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Requirement: Authenticated caller must match the PassengerId of the resource.
/// </summary>
public class PassengerOwnsResourceRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Requirement: Authenticated passenger must own the PassengerJourney resource.
/// </summary>
public class PassengerOwnsJourneyRequirement : IAuthorizationRequirement
{
}
