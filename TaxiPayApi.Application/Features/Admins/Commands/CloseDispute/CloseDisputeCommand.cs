using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.CloseDispute;

public record CloseDisputeCommand(
    Guid DisputeId,
    Guid AdminId,
    CloseDisputeRequestDto Request) : IRequest<bool>;

public class CloseDisputeCommandHandler(IAdminService _adminService)
    : IRequestHandler<CloseDisputeCommand, bool>
{
    public async Task<bool> Handle(CloseDisputeCommand request, CancellationToken cancellationToken)
    {
        return await _adminService.CloseDisputeAsync(
            request.DisputeId,
            request.AdminId,
            request.Request.ResolutionNotes,
            request.Request.IssueRefund,
            cancellationToken);
    }
}
