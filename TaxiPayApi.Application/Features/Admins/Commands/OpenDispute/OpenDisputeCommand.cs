using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.OpenDispute;

public record OpenDisputeCommand(
    Guid DisputeId,
    Guid AdminId,
    OpenDisputeRequestDto Request) : IRequest<bool>;

public class OpenDisputeCommandHandler(IAdminService _adminService)
    : IRequestHandler<OpenDisputeCommand, bool>
{
    public async Task<bool> Handle(OpenDisputeCommand request, CancellationToken cancellationToken)
    {
        return await _adminService.OpenDisputeAsync(
            request.DisputeId,
            request.AdminId,
            request.Request.Notes,
            cancellationToken);
    }
}
