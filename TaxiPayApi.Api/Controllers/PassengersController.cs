using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Features.Passengers.Commands.ConfirmAlighting;
using TaxiPayApi.Application.Features.Passengers.Commands.Login;
using TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;
using TaxiPayApi.Application.Features.Passengers.Commands.Register;
using TaxiPayApi.Application.Features.Passengers.Commands.SubmitPassengerRouteRequest;
using TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;
using TaxiPayApi.Application.Features.Passengers.Commands.UpdateProfile;
using TaxiPayApi.Application.Features.Passengers.Queries.GetDashboard;
using TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode;
using TaxiPayApi.Application.Features.Passengers.Queries.GetFare;
using TaxiPayApi.Application.Features.Passengers.Queries.GetPassengerRouteRequests;
using TaxiPayApi.Application.Features.Passengers.Queries.GetProfile;
using TaxiPayApi.Application.Features.Passengers.Queries.GetTripHistory;
using TaxiPayApi.Application.Features.Passengers.Queries.SearchDriversByCode;

namespace TaxiPayApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassengersController(
    IMediator _mediator,
    ILogger<PassengersController> _logger) : ControllerBase
{
    [HttpGet("{id}/profile")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a passenger profile")]
    [EndpointDescription("Returns the profile for the specified passenger. Returns 404 if the passenger does not exist.")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        var query = new GetPassengerProfileQuery(id);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "forbidden"
                ? StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message })
                : NotFound(new { error.Code, error.Message })
        );
    }

    [HttpPut("{id}/profile")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Update a passenger profile")]
    [EndpointDescription("Updates first name, last name, and phone number for the specified passenger.")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdatePassengerProfileRequest request, CancellationToken ct)
    {
        var command = new UpdatePassengerProfileCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message }),
                "phone_already_in_use" => Conflict(new { error.Code, error.Message }),
                "passenger_not_found" => NotFound(new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            }
        );
    }

    [HttpGet("{id}/dashboard")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(PassengerDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a passenger dashboard")]
    [EndpointDescription("Returns dashboard data for the specified passenger. Returns 404 if the passenger does not exist.")]
    public async Task<IActionResult> GetDashboard(Guid id, CancellationToken ct)
    {
        var query = new GetPassengerDashboardQuery(id);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "forbidden"
                ? StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message })
                : NotFound(new { error.Code, error.Message })
        );
    }

    [HttpGet("{id}/trips")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(TripHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a passenger trip history")]
    [EndpointDescription("Returns a paginated trip history for the specified passenger. Returns 404 if the passenger does not exist.")]
    public async Task<IActionResult> GetTripHistory(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetPassengerTripHistoryQuery(id, pageNumber, pageSize);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "forbidden"
                ? StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message })
                : NotFound(new { error.Code, error.Message })
        );
    }

    [HttpGet("drivers/search")]
    [EnableRateLimiting("search-strict")]
    [ProducesResponseType(typeof(List<DriverSearchResultDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Search drivers by code/name/route")]
    [EndpointDescription("Returns case-insensitive partial search matches for driver codes and details from the database.")]
    public async Task<IActionResult> SearchDrivers([FromQuery] string query, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchDriversByCodeQuery(query, limit), ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Message })
        );
    }

    [HttpGet("drivers/{driverId}")]
    [EnableRateLimiting("search-strict")]
    [ProducesResponseType(typeof(DriverByCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Find a driver by code")]
    [EndpointDescription("Returns public driver details for the supplied driver code. Returns 404 if no matching driver exists.")]
    public async Task<IActionResult> GetDriverByCode(string driverId, CancellationToken ct)
    {
        var query = new GetDriverByCodeQuery(driverId);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => NotFound(new { error.Code, error.Message })
        );
    }

    [HttpGet("fares")]
    [ProducesResponseType(typeof(FareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Calculate a trip fare")]
    [EndpointDescription("Calculates the fare between two nodes on a route. Returns 404 when the route or nodes cannot be found.")]
    public async Task<IActionResult> GetFare(
        [FromQuery] Guid routeId,
        [FromQuery] string fromNode,
        [FromQuery] string toNode,
        CancellationToken ct)
    {
        var query = new GetFareQuery(routeId, fromNode, toNode);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => NotFound(new { error.Code, error.Message })
        );
    }

    // AUTHENTICATION
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Register a passenger")]
    [EndpointDescription("Creates a new passenger account. Returns 409 if the submitted registration details already exist.")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterPassengerCommand(request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => CreatedAtAction(
                nameof(GetProfile),
                new { id = response.Id },
                response),
            onFailure: error => Conflict(new { error.Code, error.Message })
        );
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(PassengerLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Authenticate a passenger")]
    [EndpointDescription("Authenticates a passenger and returns an access token. Returns 401 when the credentials are invalid, or 423 when the account is temporarily locked.")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var command = new LoginPassengerCommand(request);
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
                _ => Unauthorized(new { error.Code, error.Message })
            }
        );
    }

    // PAYMENT
    [HttpPost("{id}/pay")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [EnableRateLimiting("financial-strict")]
    [ProducesResponseType(typeof(PayTripResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Pay for a trip")]
    [EndpointDescription("Processes payment for a passenger trip. Returns 400 when the payment request cannot be completed.")]
    public async Task<IActionResult> PayForTrip(Guid id, PayTripRequest request, CancellationToken ct)
    {
        var command = new PayTripCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "forbidden"
                ? StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message })
                : BadRequest(new { error.Code, error.Message })
        );
    }

    // WALLET TOP-UP
    [HttpPost("{id}/topup")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [EnableRateLimiting("financial-strict")]
    [ProducesResponseType(typeof(TopUpWalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Top up passenger wallet")]
    [EndpointDescription("Adds funds to the passenger wallet and persists the new balance.")]
    public async Task<IActionResult> TopUpWallet(Guid id, [FromBody] TopUpWalletRequest request, CancellationToken ct)
    {
        var command = new TopUpWalletCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message }),
                "passenger_not_found" => NotFound(new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            }
        );
    }

    // PASSENGER ROUTE REQUESTS
    [HttpPost("{id}/route-requests")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(PassengerRouteRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Submit a passenger route request")]
    [EndpointDescription("Submits a commuter request for a new or expanded minibus transit route.")]
    public async Task<IActionResult> SubmitRouteRequest(Guid id, [FromBody] CreatePassengerRouteRequestDto request, CancellationToken ct)
    {
        var command = new SubmitPassengerRouteRequestCommand(id, request);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: response => CreatedAtAction(
                nameof(GetPassengerRouteRequests),
                new { id },
                response),
            onFailure: error => error.Code switch
            {
                "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message }),
                "passenger_not_found" => NotFound(new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            }
        );
    }

    [HttpGet("{id}/route-requests")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(List<PassengerRouteRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get passenger route requests")]
    [EndpointDescription("Returns all transit route requests submitted by the authenticated passenger.")]
    public async Task<IActionResult> GetPassengerRouteRequests(Guid id, CancellationToken ct)
    {
        var query = new GetPassengerRouteRequestsQuery(id);
        var result = await _mediator.Send(query, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message }),
                "passenger_not_found" => NotFound(new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            }
        );
    }

    [HttpPost("{id}/journeys/{journeyId}/confirm-alight")]
    [Authorize(Roles = "Passenger,passenger,Admin,admin,SuperAdmin,superadmin")]
    [ProducesResponseType(typeof(ActivePassengerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Confirm passenger alighting")]
    [EndpointDescription("Confirms alighting for a passenger journey, setting status to Alighted and notifying driver.")]
    public async Task<IActionResult> ConfirmPassengerAlighting(Guid id, Guid journeyId, CancellationToken ct)
    {
        var command = new ConfirmAlightingCommand(id, journeyId);
        var result = await _mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "passenger_not_found" or "journey_not_found" => NotFound(new { error.Code, error.Message }),
                "forbidden" or "unauthorized_passenger" => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            }
        );
    }
}
