using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Mappings;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Mappings;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Tests.Fixtures;

public static class TestServiceFactory
{
    private static readonly IMapper _mapper;

    static TestServiceFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<DriverMappingProfile>();
            cfg.AddProfile<PassengerMappingProfile>();
        });
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    public static IMapper Mapper => _mapper;

    public static IOptions<JwtSettings> JwtSettings => Options.Create(new JwtSettings
    {
        Secret = "TaxiPaySuperSecretKeyForTestingPurposes1234567890!",
        Issuer = "TaxiPayApi",
        Audience = "TaxiPayApp",
        AccessTokenExpiryHours = 1,
        RefreshTokenExpiryDays = 7
    });

    public static PassengerService CreatePassengerService(
        TaxiPayDbContext dbContext,
        ITaxiPayRealtimeNotifier realtimeNotifier,
        ICurrentUserService? currentUserService = null,
        INotificationService? notificationService = null)
    {
        var userStore = new Mock<IUserStore<TaxiPayUser>>();
        var userManager = new UserManager<TaxiPayUser>(
            userStore.Object, null!, new PasswordHasher<TaxiPayUser>(), null!, null!, null!, null!, null!, null!);

        return new PassengerService(
            dbContext,
            userManager,
            _mapper,
            JwtSettings,
            realtimeNotifier,
            notificationService ?? new Mock<INotificationService>().Object,
            NullLogger<PassengerService>.Instance,
            currentUserService);
    }

    public static DriverService CreateDriverService(
        TaxiPayDbContext dbContext,
        ITaxiPayRealtimeNotifier realtimeNotifier,
        ICurrentUserService? currentUserService = null,
        INotificationService? notificationService = null)
    {
        var userStore = new Mock<IUserStore<TaxiPayUser>>();
        var userManager = new UserManager<TaxiPayUser>(
            userStore.Object, null!, new PasswordHasher<TaxiPayUser>(), null!, null!, null!, null!, null!, null!);

        return new DriverService(
            dbContext,
            userManager,
            _mapper,
            JwtSettings,
            realtimeNotifier,
            notificationService ?? new Mock<INotificationService>().Object,
            currentUserService);
    }
}
