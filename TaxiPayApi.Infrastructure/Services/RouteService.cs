using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Infrastructure.Services;

public class RouteService(TaxiPayDbContext dbContext) : IRouteService
{
    public async Task<List<RouteDetailDto>> GetActiveRoutesAsync(CancellationToken ct = default)
    {
        var routes = await dbContext.Routes
            .AsNoTracking()
            .Include(r => r.Shifts)
            .Where(r => !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.RouteCode)
            .ToListAsync(ct);

        return routes.Select(r => new RouteDetailDto(
            r.Id,
            r.Name,
            r.RouteCode,
            r.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            r.IsActive,
            r.Shifts.Count(s => s.Status == ShiftStatus.Active),
            r.CreatedAt
        )).ToList();
    }

    public async Task<RouteDetailDto?> GetRouteByIdAsync(Guid id, CancellationToken ct = default)
    {
        var route = await dbContext.Routes
            .AsNoTracking()
            .Include(r => r.Shifts)
            .Where(r => r.Id == id && !r.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (route == null) return null;

        return new RouteDetailDto(
            route.Id,
            route.Name,
            route.RouteCode,
            route.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            route.IsActive,
            route.Shifts.Count(s => s.Status == ShiftStatus.Active),
            route.CreatedAt
        );
    }

    public async Task<List<FareMatrixDto>> GetFaresForRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        return await dbContext.FareMatrices
            .Include(f => f.Route)
            .Where(f => f.RouteId == routeId && !f.IsDeleted && f.IsActive)
            .OrderBy(f => f.FromNode)
            .ThenBy(f => f.ToNode)
            .Select(f => new FareMatrixDto(
                f.Id,
                f.RouteId ?? routeId,
                f.Route != null ? f.Route.RouteCode : string.Empty,
                f.FromNode,
                f.ToNode,
                f.Fare,
                f.IsActive
            ))
            .ToListAsync(ct);
    }
}
