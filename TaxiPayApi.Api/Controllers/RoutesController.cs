using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Features.Routes.Queries.GetActiveRoutes;
using TaxiPayApi.Application.Features.Routes.Queries.GetFaresForRoute;
using TaxiPayApi.Application.Features.Routes.Queries.GetRouteById;

namespace TaxiPayApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RoutesController(IMediator _mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RouteDetailDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get all active transit routes")]
    [EndpointDescription("Returns a list of currently operational and non-deleted transit routes with their station waypoint nodes and active driver shift metrics.")]
    public async Task<IActionResult> GetActiveRoutes(CancellationToken ct)
    {
        var query = new GetActiveRoutesQuery();
        var routes = await _mediator.Send(query, ct);
        return Ok(routes);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RouteDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get public transit route by ID")]
    [EndpointDescription("Retrieves route metadata, active status, waypoint node coordinates, and active driver count for the given route identifier. Returns 404 if the route is not found or is marked deleted.")]
    public async Task<IActionResult> GetRouteById(Guid id, CancellationToken ct)
    {
        var query = new GetRouteByIdQuery(id);
        var dto = await _mediator.Send(query, ct);

        if (dto == null)
            return NotFound(new { message = $"Route with ID '{id}' was not found." });

        return Ok(dto);
    }

    [HttpGet("{id:guid}/fares")]
    [ProducesResponseType(typeof(List<FareMatrixDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get fare stages for a transit route")]
    [EndpointDescription("Returns all active station-to-station fare matrices and travel tariffs associated with the specified transit route identifier.")]
    public async Task<IActionResult> GetFaresForRoute(Guid id, CancellationToken ct)
    {
        var query = new GetFaresForRouteQuery(id);
        var fares = await _mediator.Send(query, ct);
        return Ok(fares);
    }
}
