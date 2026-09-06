using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Features.Admins.Commands.AdminLogin;
using TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver;
using TaxiPayApi.Application.Features.Admins.Commands.AssignDriverRoute;
using TaxiPayApi.Application.Features.Admins.Commands.CreateFareMatrix;
using TaxiPayApi.Application.Features.Admins.Commands.CreateRoute;
using TaxiPayApi.Application.Features.Admins.Commands.DeleteRoute;
using TaxiPayApi.Application.Features.Admins.Commands.GenerateSymmetricFares;
using TaxiPayApi.Application.Features.Admins.Commands.OpenDispute;
using TaxiPayApi.Application.Features.Admins.Commands.CloseDispute;
using TaxiPayApi.Application.Features.Admins.Commands.RejectDriver;
using TaxiPayApi.Application.Features.Admins.Commands.ResolveDispute;
using TaxiPayApi.Application.Features.Admins.Commands.ReviewRouteChangeRequest;
using TaxiPayApi.Application.Features.Admins.Commands.ToggleRouteStatus;
using TaxiPayApi.Application.Features.Admins.Commands.UpdateDriverStatus;
using TaxiPayApi.Application.Features.Admins.Commands.UpdateFareMatrix;
using TaxiPayApi.Application.Features.Admins.Commands.UpdateRoute;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminAnalyticsOverview;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminDisputes;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminDriverById;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminDrivers;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminProfile;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminRouteChangeRequests;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminRouteById;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminRoutes;
using TaxiPayApi.Application.Features.Admins.Queries.GetFareMatricesByRoute;

namespace TaxiPayApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,admin,SuperAdmin,superadmin")]
public class AdminsController(IMediator _mediator) : ControllerBase
{
    private Guid GetAdminId()
    {
        // "adminGuid" is the Admin domain entity Id stored in the JWT by GenerateTokens().
        // ClaimTypes.NameIdentifier holds TaxiPayUser.Id (ASP.NET Identity), which is a
        // different table and a different GUID — do NOT use it for admin domain lookups.
        var idClaim = User.FindFirstValue("adminGuid");
        return Guid.TryParse(idClaim, out var adminId) ? adminId : Guid.Empty;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    [EndpointSummary("Admin Sign In")]
    [EndpointDescription("Authenticates an administrative user using email and password credentials. Returns JWT access and refresh tokens along with admin profile details upon success, 401 for invalid credentials, or 423 when the account is temporarily locked.")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken ct)
    {
        var command = new AdminLoginCommand(request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "account_locked_out" => Problem(
                    statusCode: StatusCodes.Status423Locked,
                    title: "Account Locked",
                    detail: error.Message,
                    type: "https://taxipay.local/errors/account_locked_out"),
                _ => Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: error.Message,
                    type: $"https://taxipay.local/errors/{error.Code}")
            }
        );
    }

    [HttpGet("me")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(AdminProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get authenticated admin profile")]
    [EndpointDescription("Retrieves the profile details, contact information, and role assignments for the currently authenticated administrator identified from the JWT bearer token claims.")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var adminId = GetAdminId();
        var query = new GetAdminProfileQuery(adminId);
        var profile = await _mediator.Send(query, ct);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    // Driver Management
    [HttpGet("drivers")]
    [ProducesResponseType(typeof(List<AdminDriverListItemDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve driver registrations")]
    [EndpointDescription("Returns a list of registered drivers filtered optionally by lifecycle status (e.g. Pending, Approved, Active, Suspended, Rejected). Includes driver code, license number, preferred route, and trip metrics.")]
    public async Task<IActionResult> GetDrivers([FromQuery] string? status, CancellationToken ct)
    {
        var query = new GetAdminDriversQuery(status);
        var drivers = await _mediator.Send(query, ct);
        return Ok(drivers);
    }

    [HttpGet("drivers/{id:guid}")]
    [ProducesResponseType(typeof(AdminDriverListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get driver details by ID")]
    [EndpointDescription("Retrieves complete administrative details for a specific driver by unique identifier, including current assigned route, preferred route, license verification status, and registration timestamp.")]
    public async Task<IActionResult> GetDriverById([FromRoute] Guid id, CancellationToken ct)
    {
        var query = new GetAdminDriverByIdQuery(id);
        var driver = await _mediator.Send(query, ct);
        if (driver == null) return NotFound();
        return Ok(driver);
    }

    [HttpPut("drivers/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Update driver account status")]
    [EndpointDescription("Updates the operational status of a driver account (e.g., Active, Suspended). Validates the driver existence and status transition rules before persisting changes.")]
    public async Task<IActionResult> UpdateDriverStatus([FromRoute] Guid id, [FromBody] UpdateDriverStatusRequest request, CancellationToken ct)
    {
        var command = new UpdateDriverStatusCommand(id, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not update driver status. Verify valid status value.");
        return Ok(new { message = $"Driver status updated to {request.Status}" });
    }

    [HttpPost("drivers/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Approve a driver registration")]
    [EndpointDescription("Approves a pending driver registration, assigns or confirms the driver code, persists the selected preferred route, and updates the driver's status to Approved.")]
    public async Task<IActionResult> ApproveDriver([FromRoute] Guid id, [FromBody] ApproveDriverRequest request, CancellationToken ct)
    {
        var command = new ApproveDriverCommand(id, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not approve driver. Driver not found.");
        return Ok(new { message = "Driver approved successfully." });
    }

    [HttpPost("drivers/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Reject a driver registration")]
    [EndpointDescription("Rejects a pending driver application with a mandatory rejection reason explanation, setting the driver's status to Rejected and notifying administrative auditing.")]
    public async Task<IActionResult> RejectDriver([FromRoute] Guid id, [FromBody] RejectDriverRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            return BadRequest("Rejection reason is required.");

        var command = new RejectDriverCommand(id, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not reject driver. Driver not found or invalid reason.");
        return Ok(new { message = "Driver rejected with specified reason." });
    }

    [HttpPut("drivers/{id:guid}/assign-route")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Assign transit route to driver")]
    [EndpointDescription("Assigns a specific transit route to the driver as their designated operational route, updating their assigned route associations and shift context.")]
    public async Task<IActionResult> AssignDriverRoute([FromRoute] Guid id, [FromBody] AssignDriverRouteRequest request, CancellationToken ct)
    {
        var command = new AssignDriverRouteCommand(id, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not assign route to driver.");
        return Ok(new { message = "Driver route assigned successfully." });
    }

    // Route Management
    [HttpGet("routes")]
    [ProducesResponseType(typeof(List<RouteDetailDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get all transit routes")]
    [EndpointDescription("Returns all configured transit routes in the system including both active and deactivated routes, along with station node waypoints, route codes, and active shift counts.")]
    public async Task<IActionResult> GetRoutes(CancellationToken ct)
    {
        var query = new GetAdminRoutesQuery();
        var routes = await _mediator.Send(query, ct);
        return Ok(routes);
    }

    [HttpGet("routes/{id:guid}")]
    [ProducesResponseType(typeof(RouteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get route details by ID")]
    [EndpointDescription("Retrieves detailed route information including all sequence-ordered transit station nodes, coordinates, operational status, and active minibus count for a given route identifier.")]
    public async Task<IActionResult> GetRouteById([FromRoute] Guid id, CancellationToken ct)
    {
        var query = new GetAdminRouteByIdQuery(id);
        var route = await _mediator.Send(query, ct);
        if (route == null) return NotFound();
        return Ok(route);
    }

    [HttpPost("routes")]
    [ProducesResponseType(typeof(RouteDetailDto), StatusCodes.Status201Created)]
    [EndpointSummary("Create a transit route")]
    [EndpointDescription("Registers a new minibus transit route with its unique route code, descriptive name, and list of ordered geographical waypoint nodes (origin, intermediate stops, and terminus).")]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var command = new CreateRouteCommand(request);
        var route = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetRouteById), new { id = route.Id }, route);
    }

    [HttpPut("routes/{id:guid}")]
    [ProducesResponseType(typeof(RouteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update transit route details")]
    [EndpointDescription("Updates the name, route code, and node waypoints for an existing transit route. Returns the updated route or 404 Not Found if the route does not exist.")]
    public async Task<IActionResult> UpdateRoute([FromRoute] Guid id, [FromBody] UpdateRouteRequest request, CancellationToken ct)
    {
        var command = new UpdateRouteCommand(id, request);
        var route = await _mediator.Send(command, ct);
        if (route == null) return NotFound();
        return Ok(route);
    }

    [HttpPatch("routes/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Toggle route active status")]
    [EndpointDescription("Toggles the operational availability of a transit route between active and inactive. Inactive routes are hidden from commuter searches while preserving historical shift and fare records.")]
    public async Task<IActionResult> ToggleRouteStatus([FromRoute] Guid id, CancellationToken ct)
    {
        var command = new ToggleRouteStatusCommand(id);
        var success = await _mediator.Send(command, ct);
        if (!success) return NotFound();
        return Ok(new { message = "Route status toggled successfully." });
    }

    [HttpDelete("routes/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete a transit route")]
    [EndpointDescription("Soft-deletes a transit route from the platform, ensuring referential integrity with past trip histories and fare matrices.")]
    public async Task<IActionResult> DeleteRoute([FromRoute] Guid id, CancellationToken ct)
    {
        var command = new DeleteRouteCommand(id);
        var success = await _mediator.Send(command, ct);
        if (!success) return NotFound();
        return Ok(new { message = "Route deleted successfully." });
    }

    // Fare Management
    [HttpGet("fare-matrices/route/{routeId:guid}")]
    [ProducesResponseType(typeof(List<FareMatrixDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get fare matrices for a route")]
    [EndpointDescription("Retrieves all directional station-to-station fare definitions configured for the specified transit route identifier.")]
    public async Task<IActionResult> GetFareMatricesByRoute([FromRoute] Guid routeId, CancellationToken ct)
    {
        var query = new GetFareMatricesByRouteQuery(routeId);
        var fares = await _mediator.Send(query, ct);
        return Ok(fares);
    }

    [HttpPost("fare-matrices")]
    [ProducesResponseType(typeof(FareMatrixDto), StatusCodes.Status201Created)]
    [EndpointSummary("Create route fare stage")]
    [EndpointDescription("Defines a new fixed fare rate in ETB between a specified origin node and destination node for a transit route.")]
    public async Task<IActionResult> CreateFareMatrix([FromBody] CreateFareMatrixRequest request, CancellationToken ct)
    {
        var command = new CreateFareMatrixCommand(request);
        var fare = await _mediator.Send(command, ct);
        return Ok(fare);
    }

    [HttpPut("fare-matrices/{id:guid}")]
    [ProducesResponseType(typeof(FareMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update fare stage amount")]
    [EndpointDescription("Updates the fare cost or active status for a specific station-to-station fare matrix entry.")]
    public async Task<IActionResult> UpdateFareMatrix([FromRoute] Guid id, [FromBody] UpdateFareMatrixRequest request, CancellationToken ct)
    {
        var command = new UpdateFareMatrixCommand(id, request);
        var fare = await _mediator.Send(command, ct);
        if (fare == null) return NotFound();
        return Ok(fare);
    }

    [HttpPost("fare-matrices/generate-symmetric")]
    [ProducesResponseType(typeof(List<FareMatrixDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Generate symmetric reverse fares")]
    [EndpointDescription("Automatically generates reverse-direction fare matrix entries for all existing forward route stages that do not yet have an opposing return fare configured.")]
    public async Task<IActionResult> GenerateSymmetricFares([FromBody] GenerateSymmetricFareRequest request, CancellationToken ct)
    {
        var command = new GenerateSymmetricFaresCommand(request);
        var fares = await _mediator.Send(command, ct);
        return Ok(fares);
    }

    // Route Change Requests
    [HttpGet("route-change-requests")]
    [ProducesResponseType(typeof(List<AdminRouteChangeRequestDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List driver route change requests")]
    [EndpointDescription("Retrieves driver shift route change requests submitted from the field, filterable by review status (Pending, Approved, Rejected).")]
    public async Task<IActionResult> GetRouteChangeRequests([FromQuery] string? status, CancellationToken ct)
    {
        var query = new GetAdminRouteChangeRequestsQuery(status);
        var requests = await _mediator.Send(query, ct);
        return Ok(requests);
    }

    [HttpPost("route-change-requests/{id:guid}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Review driver route change request")]
    [EndpointDescription("Approves or rejects a driver's request to change their active transit route during a shift. Validates that the driver has completed current trips before approving route reassignment.")]
    public async Task<IActionResult> ReviewRouteChangeRequest([FromRoute] Guid id, [FromBody] ReviewRouteChangeRequestDto request, CancellationToken ct)
    {
        var adminId = GetAdminId();
        var command = new ReviewRouteChangeRequestCommand(id, adminId, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not review request. Driver may have an active trip or request is already reviewed.");
        return Ok(new { message = request.Approved ? "Route change request approved." : "Route change request rejected." });
    }

    // Disputes
    [HttpGet("disputes")]
    [ProducesResponseType(typeof(List<AdminDisputeDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List passenger-driver disputes")]
    [EndpointDescription("Retrieves reported fare and trip disputes submitted by passengers or drivers, filterable by resolution status (Open, InProgress, Resolved, Dismissed).")]
    public async Task<IActionResult> GetDisputes([FromQuery] string? status, CancellationToken ct)
    {
        var query = new GetAdminDisputesQuery(status);
        var disputes = await _mediator.Send(query, ct);
        return Ok(disputes);
    }

    [HttpPost("disputes/{id:guid}/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Reopen dispute")]
    [EndpointDescription("Transitions a dispute back to Open status for administrative review.")]
    public async Task<IActionResult> OpenDispute([FromRoute] Guid id, [FromBody] OpenDisputeRequestDto request, CancellationToken ct)
    {
        var adminId = GetAdminId();
        var command = new OpenDisputeCommand(id, adminId, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not reopen dispute.");
        return Ok(new { message = "Dispute reopened successfully." });
    }

    [HttpPost("disputes/{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Close dispute")]
    [EndpointDescription("Transitions a dispute to Closed status, recording resolution notes and issuing optional wallet refunds.")]
    public async Task<IActionResult> CloseDispute([FromRoute] Guid id, [FromBody] CloseDisputeRequestDto request, CancellationToken ct)
    {
        var adminId = GetAdminId();
        var command = new CloseDisputeCommand(id, adminId, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not close dispute.");
        return Ok(new { message = "Dispute closed successfully." });
    }

    [HttpPost("disputes/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Resolve or dismiss dispute")]
    [EndpointDescription("Applies administrative resolution to a dispute case.")]
    public async Task<IActionResult> ResolveDispute([FromRoute] Guid id, [FromBody] ReviewDisputeRequestDto request, CancellationToken ct)
    {
        var adminId = GetAdminId();
        var command = new ResolveDisputeCommand(id, adminId, request);
        var success = await _mediator.Send(command, ct);
        if (!success) return BadRequest("Could not resolve dispute.");
        return Ok(new { message = $"Dispute {request.Status.ToLower()} successfully." });
    }

    // Analytics
    [HttpGet("analytics/overview")]
    [ProducesResponseType(typeof(AdminAnalyticsOverviewDto), StatusCodes.Status200OK)]
    [EndpointSummary("Get platform analytics overview")]
    [EndpointDescription("Calculates system-wide analytics including total ridership, passenger counts, active driver fleet size, total gross revenue collected, and dispute resolution metrics.")]
    public async Task<IActionResult> GetAnalyticsOverview(CancellationToken ct)
    {
        var query = new GetAdminAnalyticsOverviewQuery();
        var overview = await _mediator.Send(query, ct);
        return Ok(overview);
    }
}
