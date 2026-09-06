using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.ResolveDispute;

public record ResolveDisputeCommand(
    Guid DisputeId,
    Guid AdminId,
    ReviewDisputeRequestDto Request) : IRequest<bool>;

public class ResolveDisputeCommandHandler(IAdminService _adminService)
    : IRequestHandler<ResolveDisputeCommand, bool>
{
    public async Task<bool> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        return await _adminService.ResolveDisputeAsync(
            request.DisputeId,
            request.AdminId,
            request.Request,
            cancellationToken);
    }
}
