using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.AdminLogin;

public record AdminLoginCommand(AdminLoginRequest Request) : IRequest<Result<AdminLoginResponse, AdminError>>;

public class AdminLoginCommandHandler(IAdminService _adminService)
    : IRequestHandler<AdminLoginCommand, Result<AdminLoginResponse, AdminError>>
{
    public async Task<Result<AdminLoginResponse, AdminError>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        return await _adminService.LoginAsync(request.Request, cancellationToken);
    }
}
