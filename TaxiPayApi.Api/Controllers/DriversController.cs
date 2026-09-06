using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Features.Drivers.Commands;
using TaxiPayApi.Application.Features.Drivers.Commands.CompleteCurrentTrip;
using TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;
using TaxiPayApi.Application.Features.Drivers.Commands.RequestAlightingConfirmation;
using TaxiPayApi.Application.Features.Drivers.Commands.RequestRouteChange;
using TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip;
using TaxiPayApi.Application.Features.Drivers.Commands.UpdateProfile;
using TaxiPayApi.Application.Features.Drivers.Queries.GetDashboard;
using TaxiPayApi.Application.Features.Drivers.Queries.GetProfile;
using TaxiPayApi.Application.Features.Drivers.Queries.GetShiftSummary;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriversController(IMediator _mediator, IDriverService _driverService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Register a driver")]
    [EndpointDescription("Creates a driver account. Returns 409 when the phone number or licence number is already registered.")]
    public async Task<IActionResult> CreateDriver(CreateDriverRequest request, CancellationToken ct)
    {
        var command = new CreateDriverCommand(request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => CreatedAtAction(
                nameof(GetProfile),
                new { id = response.Id },
                response),
            onFailure: error => Problem(
                statusCode: GetStatusCode(error.Code),
                title: "Driver Registration Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Authenticate a driver")]
    [EndpointDescription("Authenticates a driver and returns an access token. Returns 401 for invalid credentials, 403 when not permitted to sign in, or 423 when temporarily locked.")]
    public async Task<IActionResult> Login(DriverLoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: GetStatusCode(error.Code),
                title: "Login Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a driver profile")]
    [EndpointDescription("Returns the profile for the specified driver. Returns 404 if the driver does not exist.")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        var query = new GetDriverProfileQuery(id);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: error.Code == "forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound,
                title: error.Code == "forbidden" ? "Access Denied" : "Driver Not Found",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(DriverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Update a driver profile")]
    [EndpointDescription("Updates the specified driver's profile. Returns 409 if the requested phone number is already in use.")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdateProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateDriverProfileCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: GetUpdateStatusCode(error.Code),
                title: error.Code == "forbidden" ? "Access Denied" : "Update Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("{id}/route-change/request")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(RouteChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Request a driver route change")]
    [EndpointDescription("Submits a route-change request for a driver. Returns 409 if an equivalent route-change request already exists.")]
    public async Task<IActionResult> RequestRouteChange(
        Guid id,
        [FromBody] RequestRouteChangeRequest request,
        CancellationToken ct)
    {
        var command = new RequestRouteChangeCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: GetRouteChangeStatusCode(error.Code),
                title: error.Code == "forbidden" ? "Access Denied" : "Route Change Request Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpGet("{id}/route-change/current")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(RouteChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get driver's current route change request")]
    [EndpointDescription("Fetches the latest route change request submitted by the driver from the database.")]
    public async Task<IActionResult> GetCurrentRouteChange(Guid id, CancellationToken ct)
    {
        var result = await _driverService.GetCurrentRouteChangeRequestAsync(id, ct);
        return result.Match<IActionResult>(
            onSuccess: response => response != null ? Ok(response) : NoContent(),
            onFailure: error => Problem(
                statusCode: GetRouteChangeStatusCode(error.Code),
                title: error.Code == "forbidden" ? "Access Denied" : "Failed to Fetch Route Change Request",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpGet("{id}/dashboard")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(DriverDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a driver dashboard")]
    [EndpointDescription("Returns dashboard data for the specified driver. Returns 404 if the driver does not exist.")]
    public async Task<IActionResult> GetDashboard(Guid id, CancellationToken ct)
    {
        var query = new GetDriverDashboardQuery(id);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: error.Code == "forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound,
                title: error.Code == "forbidden" ? "Access Denied" : "Dashboard Not Found",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpGet("shifts/{shiftId}/summary")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(ShiftSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a shift summary")]
    [EndpointDescription("Returns the summary for the specified driver shift. Returns 404 if the shift does not exist.")]
    public async Task<IActionResult> GetShiftSummary(Guid shiftId, CancellationToken ct)
    {
        var query = new GetShiftSummaryQuery(shiftId);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: error.Code == "forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound,
                title: error.Code == "forbidden" ? "Access Denied" : "Shift Not Found",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("shifts/{shiftId}/trips/start")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Start a new trip")]
    [EndpointDescription("Starts a trip for the specified shift and direction. Returns 409 if the shift already has an active trip.")]
    public async Task<IActionResult> StartNewTrip(Guid shiftId, [FromBody] StartTripRequest request, CancellationToken ct)
    {
        var command = new StartNewTripCommand(shiftId, request.Direction);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: GetTripStatusCode(error.Code),
                title: error.Code == "forbidden" ? "Access Denied" : "Start Trip Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("shifts/{shiftId}/trips/complete")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(TripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Complete the current trip")]
    [EndpointDescription("Completes the active trip for the specified shift using the driver's current location. Returns 409 if there is no active trip.")]
    public async Task<IActionResult> CompleteCurrentTrip(
        Guid shiftId,
        [FromBody] CompleteTripRequest request,
        CancellationToken ct)
    {
        var command = new CompleteCurrentTripCommand(
            shiftId,
            request.Latitude,
            request.Longitude);

        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: GetTripStatusCode(error.Code),
                title: error.Code == "forbidden" ? "Access Denied" : "Complete Trip Failed",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("journeys/{journeyId}/request-alight")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(ActivePassengerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Request passenger alighting")]
    [EndpointDescription("Flags a passenger journey as awaiting alighting confirmation.")]
    public async Task<IActionResult> RequestPassengerAlighting(Guid journeyId, CancellationToken ct)
    {
        var command = new RequestAlightingConfirmationCommand(journeyId);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: error.Code == "forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound,
                title: error.Code == "forbidden" ? "Access Denied" : "Journey Not Found",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    [HttpPost("journeys/{journeyId}/confirm-alight")]
    [Authorize(Roles = "Driver,driver,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(ActivePassengerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Confirm passenger alighting")]
    [EndpointDescription("Confirms alighting for a passenger journey, setting status to Alighted and removing them from live manifest.")]
    public async Task<IActionResult> ConfirmPassengerAlighting(Guid journeyId, CancellationToken ct)
    {
        var result = await _driverService.ConfirmPassengerAlightingAsync(journeyId, ct);

        return result.Match<IActionResult>(
            onSuccess: response => Ok(response),
            onFailure: error => Problem(
                statusCode: error.Code == "forbidden" ? StatusCodes.Status403Forbidden : StatusCodes.Status404NotFound,
                title: error.Code == "forbidden" ? "Access Denied" : "Journey Not Found",
                detail: error.Message,
                type: $"https://taxipay.local/errors/{error.Code}")
        );
    }

    private int GetTripStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "forbidden" => StatusCodes.Status403Forbidden,
            "shift_not_found" or "route_not_found" or "destination_node_not_found" => StatusCodes.Status404NotFound,
            "shift_not_active" or "invalid_direction" or "driver_not_at_destination" => StatusCodes.Status400BadRequest,
            "trip_already_active" or "no_active_trip" => StatusCodes.Status409Conflict,
            "cannot_complete_trip" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private int GetRouteChangeStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "forbidden" or "cannot_request_route_change" => StatusCodes.Status403Forbidden,
            "driver_not_found" or "route_not_found" => StatusCodes.Status404NotFound,
            "route_change_already_requested" or "same_route_requested" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private int GetUpdateStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "forbidden" or "cannot_update_suspended" or "cannot_update_pending" => StatusCodes.Status403Forbidden,
            "driver_not_found" => StatusCodes.Status404NotFound,
            "phone_already_in_use" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "forbidden" or "driver_not_approved" or "driver_suspended" or "driver_rejected" => StatusCodes.Status403Forbidden,
            "phone_already_exists" or "license_already_exists" => StatusCodes.Status409Conflict,
            "route_not_found" => StatusCodes.Status404NotFound,
            "invalid_credentials" => StatusCodes.Status401Unauthorized,
            "account_locked_out" => StatusCodes.Status423Locked,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
