using System.Security.Claims;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using TaxiPayApi.Api.ExceptionHandlers;
using TaxiPayApi.Api.Extensions;
using TaxiPayApi.Api.Middlewares;
using TaxiPayApi.Application.Behaviors;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.Common.Mappings;
using TaxiPayApi.Application.Common.Telemetry;
using TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Mappings;
using TaxiPayApi.Infrastructure.Security.Handlers;
using TaxiPayApi.Infrastructure.Security.Requirements;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;
using TaxiPayApi.Infrastructure.SeedData;
using TaxiPayApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Database Context Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(connectionString))
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();
    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<TaxiPayDbContext>(options =>
        options.UseNpgsql(dataSource));
}

// ASP.NET Core Identity Configuration
builder.Services.AddIdentity<TaxiPayUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;

    // Brute-Force Lockout Protection
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<TaxiPayDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication Configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? "fallback-secret-key")),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin", "admin", "SuperAdmin", "superadmin"));
    options.AddPolicy("RequireDriverRole", policy => policy.RequireRole("Driver", "driver", "Admin", "admin"));
    options.AddPolicy("RequirePassengerRole", policy => policy.RequireRole("Passenger", "passenger", "Admin", "admin"));
});

// Resource-Based Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, DriverOwnsTripAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, DriverResourceAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PassengerResourceAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PassengerJourneyAuthorizationHandler>();

builder.Services.AddSignalR();

builder.Logging.AddJsonConsole(options => 
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new() { Indented = false };
});

// Cache Configuration & HybridCache Registration
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection(CacheSettings.SectionName));
var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() ?? new CacheSettings();

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(cacheSettings.DefaultExpirationMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(cacheSettings.DefaultExpirationMinutes)
    };
});
#pragma warning restore EXTEXP0018

// Tier-Aware Rate Limiting
builder.Services.AddTaxiPayRateLimiting();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaxiPayDbContext>("PostgreSQL", tags: ["ready", "db"]);

// OpenTelemetry Observability (Tracing & Metrics)
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: TaxiPayDiagnostics.ServiceName, serviceVersion: TaxiPayDiagnostics.ServiceVersion);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
               .AddSource(TaxiPayDiagnostics.ServiceName)
               .AddSource("Npgsql")
               .AddAspNetCoreInstrumentation(opts =>
               {
                   opts.RecordException = true;
                   opts.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
               })
               .AddHttpClientInstrumentation();

        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
               .AddMeter(TaxiPayDiagnostics.ServiceName)
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation();

        if (builder.Environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();
        }

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<DriverMappingProfile>();
    cfg.AddProfile<PassengerMappingProfile>();
});
builder.Services.AddValidatorsFromAssembly(typeof(CreateDriverCommandValidator).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateDriverCommandHandler).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// Add Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IDriverService, DriverService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaxiPayRealtimeNotifier, TaxiPayRealtimeNotifier>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var exception = context.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (exception is not null)
        {
            context.ProblemDetails.Detail = exception.Message;
        }
    };
});

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TaxiPay API Reference")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TaxiPayDbContext>();
    var userManager = services.GetRequiredService<UserManager<TaxiPayUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        await SeedData.SeedAsync(context, userManager, roleManager, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to run migrations and seed data during startup.");
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Real-time SignalR Hub
app.MapHub<TaxiPayApi.Infrastructure.Hubs.TaxiPayHub>("/hubs/taxipay");

// Health Check Endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // Process liveness check
    ResponseWriter = HealthCheckResponseWriter.WriteLiveResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program { }

