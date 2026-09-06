using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Infrastructure.SeedData;

public static class SeedData
{
    public static async Task SeedAsync(
        TaxiPayDbContext context,
        UserManager<TaxiPayUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Seed Identity Roles
            string[] roles = ["Admin", "Driver", "Passenger", "SuperAdmin", "FareManager", "RouteManager", "DisputeResolver"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Ensure all existing Admins, Drivers, Passengers in DB have valid TaxiPayUser accounts and working passwords
            var existingAdmins = await context.Admins.ToListAsync(cancellationToken);
            foreach (var admin in existingAdmins)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    admin.Email,
                    admin.Email,
                    admin.Phone,
                    admin.FirstName,
                    admin.LastName,
                    "Admin@123",
                    ["Admin", admin.Role.ToString()]);
                admin.UserId = user.Id;
            }

            var existingDrivers = await context.Drivers.ToListAsync(cancellationToken);
            foreach (var driver in existingDrivers)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    driver.Phone,
                    null,
                    driver.Phone,
                    driver.FirstName,
                    driver.LastName,
                    "Driver@123",
                    ["Driver"]);
                driver.UserId = user.Id;
            }

            var existingPassengers = await context.Passengers.ToListAsync(cancellationToken);
            foreach (var passenger in existingPassengers)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    passenger.Phone,
                    null,
                    passenger.Phone,
                    passenger.FirstName,
                    passenger.LastName,
                    "Pass@123",
                    ["Passenger"]);
                passenger.UserId = user.Id;
            }
            await context.SaveChangesAsync(cancellationToken);

            if (await context.Drivers.AnyAsync(cancellationToken))
            {
                var routesList = await context.Routes.ToListAsync(cancellationToken);
                if (routesList.Any())
                {
                    var allSeedDrivers = SeedDrivers(routesList);
                    var existingPhones = await context.Drivers.Select(d => d.Phone).ToListAsync(cancellationToken);
                    var existingDriverIds = await context.Drivers.Where(d => d.DriverId != null).Select(d => d.DriverId!).ToListAsync(cancellationToken);
                    var toAddDrivers = allSeedDrivers
                        .Where(d => !existingPhones.Contains(d.Phone) && (d.DriverId == null || !existingDriverIds.Contains(d.DriverId)))
                        .ToList();

                    if (toAddDrivers.Any())
                    {
                        logger.LogInformation("Seeding {Count} additional drivers to reach full fleet capacity...", toAddDrivers.Count);
                        foreach (var driver in toAddDrivers)
                        {
                            var user = await EnsureUserAsync(
                                userManager,
                                driver.Phone,
                                null,
                                driver.Phone,
                                driver.FirstName,
                                driver.LastName,
                                "Driver@123",
                                ["Driver"]);
                            driver.UserId = user.Id;
                        }
                        await context.Drivers.AddRangeAsync(toAddDrivers, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                    }
                }

                // Check and seed Disputes if missing
                if (!await context.Disputes.AnyAsync(cancellationToken))
                {
                    var driversList = await context.Drivers.AsNoTracking().ToListAsync(cancellationToken);
                    var passengersList = await context.Passengers.AsNoTracking().ToListAsync(cancellationToken);
                    var tripsList = await context.Trips.AsNoTracking().ToListAsync(cancellationToken);
                    if (driversList.Any() && passengersList.Any() && tripsList.Any())
                    {
                        var disputes = SeedDisputes(driversList, passengersList, tripsList);
                        await context.Disputes.AddRangeAsync(disputes, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Seeded disputes into database.");
                    }
                }

                // Check and seed Route Change Requests if missing
                if (!await context.RouteChangeRequests.AnyAsync(cancellationToken))
                {
                    var driversList = await context.Drivers.AsNoTracking().ToListAsync(cancellationToken);
                    var adminsList = await context.Admins.AsNoTracking().ToListAsync(cancellationToken);
                    if (driversList.Any() && routesList.Any() && adminsList.Any())
                    {
                        var reqs = SeedRouteChangeRequests(driversList, routesList, adminsList);
                        await context.RouteChangeRequests.AddRangeAsync(reqs, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Seeded route change requests into database.");
                    }
                }

                // Check and seed initial AppNotifications if missing
                if (!await context.AppNotifications.AnyAsync(cancellationToken))
                {
                    var notifs = new List<AppNotification>
                    {
                        new AppNotification
                        {
                            Role = "Admin",
                            Title = "Platform Operations Active",
                            Message = "TaxiPay urban transit supervision console initialized with real-time telemetry.",
                            Type = "info",
                            TargetUrl = "/admin/dashboard",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddHours(-1)
                        },
                        new AppNotification
                        {
                            Role = "Driver",
                            Title = "Corridor Shift Ready",
                            Message = "Your assigned route corridor is synchronized. Select direction to start trips.",
                            Type = "success",
                            TargetUrl = "/driver/dashboard",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                        },
                        new AppNotification
                        {
                            Role = "Passenger",
                            Title = "Welcome to TaxiPay",
                            Message = "Cashless digital payments and instant boarding active on all blue minibuses.",
                            Type = "info",
                            TargetUrl = "/passenger/dashboard",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
                        }
                    };
                    await context.AppNotifications.AddRangeAsync(notifs, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Seeded notifications into database.");
                }

                // Backfill Receipts for existing completed PassengerJourneys if missing
                var existingReceiptJourneyIds = await context.Receipts
                    .Where(r => r.JourneyId != null)
                    .Select(r => r.JourneyId!.Value)
                    .ToListAsync(cancellationToken);

                var unreceiptedJourneys = await context.PassengerJourneyss
                    .Include(pj => pj.Passenger)
                    .Include(pj => pj.Trip)
                        .ThenInclude(t => t.Driver)
                    .Include(pj => pj.Trip)
                        .ThenInclude(t => t.Route)
                    .Where(pj => pj.PaymentStatus == PaymentStatus.Completed && !existingReceiptJourneyIds.Contains(pj.Id))
                    .ToListAsync(cancellationToken);

                if (unreceiptedJourneys.Any())
                {
                    logger.LogInformation("Backfilling {Count} missing receipts for genuine completed historical journeys...", unreceiptedJourneys.Count);
                    var newReceipts = unreceiptedJourneys.Select(j =>
                    {
                        var route = j.Trip?.Route;
                        var driver = j.Trip?.Driver;
                        var passenger = j.Passenger;

                        var boardingName = route?.Nodes?.FirstOrDefault(n =>
                            n.Id.Equals(j.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
                            n.Name.Equals(j.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? j.BoardingNode;
                        var destinationName = route?.Nodes?.FirstOrDefault(n =>
                            n.Id.Equals(j.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
                            n.Name.Equals(j.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? j.DestinationNode;

                        var receiptNumber = $"RCP-{j.CreatedAt:yyyyMMdd}-{j.Id.ToString()[..8].ToUpper()}";

                        return new Receipt
                        {
                            TripId = j.TripId,
                            PassengerId = j.PassengerId,
                            JourneyId = j.Id,
                            ReceiptNumber = receiptNumber,
                            PassengerPhone = passenger?.Phone ?? string.Empty,
                            PassengerName = passenger != null ? $"{passenger.FirstName} {passenger.LastName}" : "Commuter",
                            DriverName = driver != null ? $"{driver.FirstName} {driver.LastName}" : "Driver",
                            DriverCode = driver?.DriverId ?? string.Empty,
                            RouteName = route?.Name ?? "Minibus Transit",
                            BoardingNode = boardingName,
                            DestinationNode = destinationName,
                            Fare = j.Fare,
                            PaymentMethod = j.PaymentMethod.ToString(),
                            PaymentStatus = j.PaymentStatus.ToString(),
                            TransactionRef = j.TransactionRef ?? string.Empty,
                            ReceiptText = $"MINIBUS E-TICKET: {route?.Name ?? "Minibus Transit"} ({boardingName} → {destinationName}) | Fare: {j.Fare:F2} ETB",
                            SentAt = j.CreatedAt,
                            DeliveryStatus = ReceiptDeliveryStatus.Sent,
                            CreatedAt = j.CreatedAt
                        };
                    }).ToList();

                    await context.Receipts.AddRangeAsync(newReceipts, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Successfully backfilled {Count} historical receipts.", newReceipts.Count);
                }

                logger.LogInformation("Database verified and in sync.");
                return;
            }

            logger.LogInformation("Starting database seeding...");

            logger.LogInformation("Seeding routes...");
            var routes = SeedRoutes();
            await context.Routes.AddRangeAsync(routes, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding fare matrices...");
            var fareMatrices = SeedFareMatrices(routes);
            await context.FareMatrices.AddRangeAsync(fareMatrices, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding admins...");
            var admins = SeedAdmins();
            foreach (var admin in admins)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    admin.Email,
                    admin.Email,
                    admin.Phone,
                    admin.FirstName,
                    admin.LastName,
                    "Admin@123",
                    ["Admin", admin.Role.ToString()]);
                admin.UserId = user.Id;
            }
            await context.Admins.AddRangeAsync(admins, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding drivers...");
            var drivers = SeedDrivers(routes);
            foreach (var driver in drivers)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    driver.Phone,
                    null,
                    driver.Phone,
                    driver.FirstName,
                    driver.LastName,
                    "Driver@123",
                    ["Driver"]);
                driver.UserId = user.Id;
            }
            await context.Drivers.AddRangeAsync(drivers, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding passengers...");
            var passengers = SeedPassengers();
            foreach (var passenger in passengers)
            {
                var user = await EnsureUserAsync(
                    userManager,
                    passenger.Phone,
                    null,
                    passenger.Phone,
                    passenger.FirstName,
                    passenger.LastName,
                    "Pass@123",
                    ["Passenger"]);
                passenger.UserId = user.Id;
            }
            await context.Passengers.AddRangeAsync(passengers, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding driver shifts...");
            var shifts = SeedDriverShifts(drivers, routes);
            await context.DriverShifts.AddRangeAsync(shifts, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding trips...");
            var trips = SeedTrips(shifts, passengers, routes);
            await context.Trips.AddRangeAsync(trips, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding passenger journeys...");
            var journeys = SeedPassengerJourneys(trips, passengers);
            await context.PassengerJourneyss.AddRangeAsync(journeys, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeding route change requests...");
            var routeChangeRequests = SeedRouteChangeRequests(drivers, routes, admins);
            await context.RouteChangeRequests.AddRangeAsync(routeChangeRequests, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task<TaxiPayUser> EnsureUserAsync(
        UserManager<TaxiPayUser> userManager,
        string userName,
        string? email,
        string phone,
        string firstName,
        string lastName,
        string password,
        string[] roles)
    {
        var user = await userManager.FindByNameAsync(userName)
            ?? (email != null ? await userManager.FindByEmailAsync(email) : null)
            ?? await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        if (user == null)
        {
            user = new TaxiPayUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = phone,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(user, password);
            if (!res.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create seed user {userName}: {string.Join(", ", res.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Ensure password hash is set and matches
            if (string.IsNullOrEmpty(user.PasswordHash) || !await userManager.HasPasswordAsync(user))
            {
                var addPassResult = await userManager.AddPasswordAsync(user, password);
                if (!addPassResult.Succeeded)
                {
                    user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
                    await userManager.UpdateAsync(user);
                }
            }
            else
            {
                var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
                if (!isPasswordValid)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var resetRes = await userManager.ResetPasswordAsync(user, token, password);
                    if (!resetRes.Succeeded)
                    {
                        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
                        await userManager.UpdateAsync(user);
                    }
                }
            }

            user.FirstName = string.IsNullOrEmpty(user.FirstName) ? firstName : user.FirstName;
            user.LastName = string.IsNullOrEmpty(user.LastName) ? lastName : user.LastName;
            user.PhoneNumber = string.IsNullOrEmpty(user.PhoneNumber) ? phone : user.PhoneNumber;
            if (email != null && string.IsNullOrEmpty(user.Email))
            {
                user.Email = email;
            }
            await userManager.UpdateAsync(user);
        }

        await userManager.SetLockoutEnabledAsync(user, true);

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }

        return user;
    }

    private static List<Route> SeedRoutes()
    {
        return new List<Route>
        {
            new Route
            {
                Name = "Bole ↔ Merkato",
                RouteCode = "BM-001",
                Nodes = new List<Node>
                {
                    new Node { Id = "A", Name = "Bole Medhanealem", Latitude = 9.0145, Longitude = 38.7468 },
                    new Node { Id = "B", Name = "Bole Bridge", Latitude = 9.0185, Longitude = 38.7512 },
                    new Node { Id = "C", Name = "Megenagna", Latitude = 9.0225, Longitude = 38.7556 },
                    new Node { Id = "D", Name = "Mexico", Latitude = 9.0265, Longitude = 38.7600 },
                    new Node { Id = "E", Name = "Meskel Square", Latitude = 9.0305, Longitude = 38.7644 },
                    new Node { Id = "F", Name = "Merkato", Latitude = 9.0345, Longitude = 38.7688 }
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Route
            {
                Name = "Piazza ↔ Kality",
                RouteCode = "PK-001",
                Nodes = new List<Node>
                {
                    new Node { Id = "P", Name = "Piazza Churchill", Latitude = 9.0300, Longitude = 38.7500 },
                    new Node { Id = "Q", Name = "Stadium", Latitude = 9.0100, Longitude = 38.7550 },
                    new Node { Id = "R", Name = "Gotera", Latitude = 8.9900, Longitude = 38.7600 },
                    new Node { Id = "S", Name = "Kality Total", Latitude = 8.9300, Longitude = 38.7700 }
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Route
            {
                Name = "Bole ↔ Mexico",
                RouteCode = "BM-002",
                Nodes = new List<Node>
                {
                    new Node { Id = "A", Name = "Bole Medhanealem", Latitude = 9.0145, Longitude = 38.7468 },
                    new Node { Id = "B", Name = "Bole Bridge", Latitude = 9.0185, Longitude = 38.7512 },
                    new Node { Id = "C", Name = "Megenagna", Latitude = 9.0225, Longitude = 38.7556 },
                    new Node { Id = "D", Name = "Mexico", Latitude = 9.0265, Longitude = 38.7600 }
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static List<FareMatrix> SeedFareMatrices(List<Route> routes)
    {
        var fareMatrices = new List<FareMatrix>();

        // Bole ↔ Merkato fares (A→F)
        var route1 = routes.First(r => r.RouteCode == "BM-001");
        var fareData1 = new Dictionary<string, decimal>
        {
            { "A→B", 5 }, { "A→C", 10 }, { "A→D", 15 }, { "A→E", 20 }, { "A→F", 25 },
            { "B→C", 5 }, { "B→D", 10 }, { "B→E", 15 }, { "B→F", 20 },
            { "C→D", 5 }, { "C→E", 10 }, { "C→F", 15 },
            { "D→E", 5 }, { "D→F", 10 },
            { "E→F", 5 }
        };

        foreach (var fare in fareData1)
        {
            var parts = fare.Key.Split('→');
            fareMatrices.Add(new FareMatrix
            {
                RouteId = route1.Id,
                FromNode = parts[0],
                ToNode = parts[1],
                Fare = fare.Value,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Piazza ↔ Kality fares (P→S)
        var route2 = routes.First(r => r.RouteCode == "PK-001");
        var fareData2 = new Dictionary<string, decimal>
        {
            { "P→Q", 5 }, { "P→R", 10 }, { "P→S", 15 },
            { "Q→R", 5 }, { "Q→S", 10 },
            { "R→S", 5 }
        };

        foreach (var fare in fareData2)
        {
            var parts = fare.Key.Split('→');
            fareMatrices.Add(new FareMatrix
            {
                RouteId = route2.Id,
                FromNode = parts[0],
                ToNode = parts[1],
                Fare = fare.Value,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Bole ↔ Mexico fares (A→D)
        var route3 = routes.First(r => r.RouteCode == "BM-002");
        var fareData3 = new Dictionary<string, decimal>
        {
            { "A→B", 5 }, { "A→C", 10 }, { "A→D", 15 },
            { "B→C", 5 }, { "B→D", 10 },
            { "C→D", 5 }
        };

        foreach (var fare in fareData3)
        {
            var parts = fare.Key.Split('→');
            fareMatrices.Add(new FareMatrix
            {
                RouteId = route3.Id,
                FromNode = parts[0],
                ToNode = parts[1],
                Fare = fare.Value,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        return fareMatrices;
    }

    private static List<Admin> SeedAdmins()
    {
        return new List<Admin>
        {
            new Admin
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@taxipay.com",
                Phone = "0912345678",
                Role = AdminRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Admin
            {
                FirstName = "Fare",
                LastName = "Manager",
                Email = "fare@taxipay.com",
                Phone = "0923456789",
                Role = AdminRole.FareManager,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Admin
            {
                FirstName = "Route",
                LastName = "Manager",
                Email = "route@taxipay.com",
                Phone = "0934567890",
                Role = AdminRole.RouteManager,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    private static List<Driver> SeedDrivers(List<Route> routes)
    {
        var route1 = routes.FirstOrDefault(r => r.RouteCode == "BM-001") ?? routes[0];
        var route2 = routes.FirstOrDefault(r => r.RouteCode == "PK-001") ?? routes[0];
        var route3 = routes.FirstOrDefault(r => r.RouteCode == "BM-002") ?? routes[0];

        return new List<Driver>
        {
            new Driver
            {
                DriverId = "AB1234",
                FirstName = "Abebe",
                LastName = "Kebede",
                Phone = "0911122334",
                LicenseNumber = "DL-001",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-30),
                CurrentRouteId = route1.Id,
                Rating = 4.8m,
                TotalTrips = 150,
                TotalRevenue = 1500m,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "TK9876",
                FirstName = "Tigist",
                LastName = "Mekonnen",
                Phone = "0922233445",
                LicenseNumber = "DL-002",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-25),
                CurrentRouteId = route2.Id,
                Rating = 4.5m,
                TotalTrips = 120,
                TotalRevenue = 1200m,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "HD4321",
                FirstName = "Hailu",
                LastName = "Desta",
                Phone = "0933344556",
                LicenseNumber = "DL-003",
                Status = DriverStatus.Pending,
                IsVerified = false,
                CurrentRouteId = route1.Id,
                Rating = 0,
                TotalTrips = 0,
                TotalRevenue = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "EM5678",
                FirstName = "Eden",
                LastName = "Mulugeta",
                Phone = "0944455667",
                LicenseNumber = "DL-004",
                Status = DriverStatus.Suspended,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-20),
                CurrentRouteId = route2.Id,
                Rating = 3.2m,
                TotalTrips = 45,
                TotalRevenue = 450m,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "DA1001",
                FirstName = "Dawit",
                LastName = "Alemu",
                Phone = "0911223344",
                LicenseNumber = "DL-88912",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-15),
                CurrentRouteId = route1.Id,
                Rating = 4.8m,
                TotalTrips = 142,
                TotalRevenue = 3550m,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "TK1002",
                FirstName = "Tewodros",
                LastName = "Kassahun",
                Phone = "0922334455",
                LicenseNumber = "DL-77301",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-12),
                CurrentRouteId = route2.Id,
                Rating = 4.9m,
                TotalTrips = 208,
                TotalRevenue = 5200m,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "MT1005",
                FirstName = "Meron",
                LastName = "Tadesse",
                Phone = "0955667788",
                LicenseNumber = "DL-55201",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-10),
                CurrentRouteId = route3.Id,
                Rating = 4.7m,
                TotalTrips = 98,
                TotalRevenue = 2450m,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "YG1006",
                FirstName = "Yared",
                LastName = "Getachew",
                Phone = "0966778899",
                LicenseNumber = "DL-44310",
                Status = DriverStatus.Pending,
                IsVerified = false,
                CurrentRouteId = route1.Id,
                Rating = 0,
                TotalTrips = 0,
                TotalRevenue = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "BA1007",
                FirstName = "Biruk",
                LastName = "Assefa",
                Phone = "0977889900",
                LicenseNumber = "DL-33420",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-18),
                CurrentRouteId = route2.Id,
                Rating = 4.6m,
                TotalTrips = 175,
                TotalRevenue = 4375m,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "SM1008",
                FirstName = "Selamawit",
                LastName = "Mengistu",
                Phone = "0988990011",
                LicenseNumber = "DL-22530",
                Status = DriverStatus.Active,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-22),
                CurrentRouteId = route3.Id,
                Rating = 4.9m,
                TotalTrips = 230,
                TotalRevenue = 5750m,
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "TA1009",
                FirstName = "Tariku",
                LastName = "Ayele",
                Phone = "0999001122",
                LicenseNumber = "DL-11640",
                Status = DriverStatus.Pending,
                IsVerified = false,
                CurrentRouteId = route2.Id,
                Rating = 0,
                TotalTrips = 0,
                TotalRevenue = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new Driver
            {
                DriverId = "KW1010",
                FirstName = "Kassaye",
                LastName = "Wolde",
                Phone = "0910001010",
                LicenseNumber = "DL-99750",
                Status = DriverStatus.Suspended,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-14),
                CurrentRouteId = route1.Id,
                Rating = 2.8m,
                TotalTrips = 30,
                TotalRevenue = 750m,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private static List<Passenger> SeedPassengers()
    {
        return new List<Passenger>
        {
            new Passenger
            {
                FirstName = "Alem",
                LastName = "Tesfaye",
                Phone = "0955566778",
                TotalTrips = 25,
                TotalSpent = 250m,
                Rating = 4.5m,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-15),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Passenger
            {
                FirstName = "Biruk",
                LastName = "Mekonnen",
                Phone = "0966677889",
                TotalTrips = 18,
                TotalSpent = 180m,
                Rating = 4.2m,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Passenger
            {
                FirstName = "Chala",
                LastName = "Hailu",
                Phone = "0977788990",
                TotalTrips = 30,
                TotalSpent = 300m,
                Rating = 4.8m,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-20),
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Passenger
            {
                FirstName = "Desta",
                LastName = "Wondimu",
                Phone = "0988899001",
                TotalTrips = 12,
                TotalSpent = 120m,
                Rating = 3.8m,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Passenger
            {
                FirstName = "Eden",
                LastName = "Assefa",
                Phone = "0999900112",
                TotalTrips = 8,
                TotalSpent = 80m,
                Rating = 4.0m,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
    }

    private static List<DriverShift> SeedDriverShifts(List<Driver> drivers, List<Route> routes)
    {
        var driver1 = drivers.First(d => d.DriverId == "AB1234");
        var driver2 = drivers.First(d => d.DriverId == "TK9876");
        var route1 = routes.First(r => r.RouteCode == "BM-001");
        var route2 = routes.First(r => r.RouteCode == "PK-001");

        return new List<DriverShift>
        {
            new DriverShift
            {
                DriverId = driver1.Id,
                RouteId = route1.Id,
                ShiftStart = DateTime.UtcNow.AddHours(-8),
                ShiftEnd = DateTime.UtcNow.AddHours(-2),
                TotalPassengers = 12,
                TotalRevenue = 150m,
                CurrentTripNumber = 2,
                Status = ShiftStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-8)
            },
            new DriverShift
            {
                DriverId = driver2.Id,
                RouteId = route2.Id,
                ShiftStart = DateTime.UtcNow.AddHours(-4),
                TotalPassengers = 8,
                TotalRevenue = 100m,
                CurrentTripNumber = 1,
                Status = ShiftStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            }
        };
    }

    private static List<Trip> SeedTrips(List<DriverShift> shifts, List<Passenger> passengers, List<Route> routes)
    {
        var shift1 = shifts.First(s => s.Status == ShiftStatus.Completed);
        var shift2 = shifts.First(s => s.Status == ShiftStatus.Active);
        var route1 = routes.First(r => r.RouteCode == "BM-001");
        var route2 = routes.First(r => r.RouteCode == "PK-001");

        return new List<Trip>
        {
            new Trip
            {
                DriverShiftId = shift1.Id,
                DriverId = shift1.DriverId,
                RouteId = route1.Id,
                Direction = "A→F",
                TripNumber = 1,
                TripStart = DateTime.UtcNow.AddHours(-7),
                TripEnd = DateTime.UtcNow.AddHours(-6),
                PassengersCount = 8,
                TripRevenue = 120m,
                HasReachedFinalDestination = true,
                Status = TripStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-7)
            },
            new Trip
            {
                DriverShiftId = shift1.Id,
                DriverId = shift1.DriverId,
                RouteId = route1.Id,
                Direction = "F→A",
                TripNumber = 2,
                TripStart = DateTime.UtcNow.AddHours(-5),
                TripEnd = DateTime.UtcNow.AddHours(-3),
                PassengersCount = 4,
                TripRevenue = 30m,
                HasReachedFinalDestination = true,
                Status = TripStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new Trip
            {
                DriverShiftId = shift2.Id,
                DriverId = shift2.DriverId,
                RouteId = route2.Id,
                Direction = "P→S",
                TripNumber = 1,
                TripStart = DateTime.UtcNow.AddHours(-3),
                PassengersCount = 8,
                TripRevenue = 100m,
                HasReachedFinalDestination = false,
                Status = TripStatus.Active,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            }
        };
    }

    private static List<PassengerJourney> SeedPassengerJourneys(List<Trip> trips, List<Passenger> passengers)
    {
        var trip1 = trips.First(t => t.TripNumber == 1 && t.Status == TripStatus.Completed);
        var trip2 = trips.First(t => t.TripNumber == 2 && t.Status == TripStatus.Completed);
        var trip3 = trips.First(t => t.Status == TripStatus.Active);

        var p1 = passengers[0];
        var p2 = passengers[1];
        var p3 = passengers[2];
        var p4 = passengers[3];

        return new List<PassengerJourney>
        {
            new PassengerJourney
            {
                TripId = trip1.Id,
                PassengerId = p1.Id,
                BoardingNode = "A",
                DestinationNode = "C",
                Fare = 10m,
                PaymentMethod = PaymentMethod.Telebirr,
                PaymentStatus = PaymentStatus.Completed,
                TransactionRef = "TXN-001",
                AlightingStatus = AlightingStatus.Alighted,
                AlightedAt = DateTime.UtcNow.AddHours(-6).AddMinutes(-30),
                CreatedAt = DateTime.UtcNow.AddHours(-7)
            },
            new PassengerJourney
            {
                TripId = trip1.Id,
                PassengerId = p2.Id,
                BoardingNode = "B",
                DestinationNode = "F",
                Fare = 20m,
                PaymentMethod = PaymentMethod.Wallet,
                PaymentStatus = PaymentStatus.Completed,
                TransactionRef = "TXN-002",
                AlightingStatus = AlightingStatus.Alighted,
                AlightedAt = DateTime.UtcNow.AddHours(-6),
                CreatedAt = DateTime.UtcNow.AddHours(-7)
            },
            new PassengerJourney
            {
                TripId = trip2.Id,
                PassengerId = p3.Id,
                BoardingNode = "F",
                DestinationNode = "D",
                Fare = 10m,
                PaymentMethod = PaymentMethod.Telebirr,
                PaymentStatus = PaymentStatus.Completed,
                TransactionRef = "TXN-003",
                AlightingStatus = AlightingStatus.Alighted,
                AlightedAt = DateTime.UtcNow.AddHours(-4),
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new PassengerJourney
            {
                TripId = trip3.Id,
                PassengerId = p4.Id,
                BoardingNode = "P",
                DestinationNode = "R",
                Fare = 10m,
                PaymentMethod = PaymentMethod.Wallet,
                PaymentStatus = PaymentStatus.Completed,
                TransactionRef = "TXN-004",
                AlightingStatus = AlightingStatus.OnBoard,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            }
        };
    }

    private static List<RouteChangeRequest> SeedRouteChangeRequests(
        List<Driver> drivers,
        List<Route> routes,
        List<Admin> admins)
    {
        var activeDriver = drivers.First(d => d.Status == DriverStatus.Active);
        var targetRoute = routes.First(r => r.Id != activeDriver.CurrentRouteId);
        var admin = admins.First(a => a.Role == AdminRole.SuperAdmin || a.Role == AdminRole.RouteManager);

        return new List<RouteChangeRequest>
        {
            new RouteChangeRequest
            {
                DriverId = activeDriver.Id,
                CurrentRouteId = activeDriver.CurrentRouteId ?? routes[0].Id,
                NewRouteId = targetRoute.Id,
                Reason = "Relocated residence closer to the requested corridor.",
                Status = RouteChangeStatus.Pending,
                RequestedAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
    }

    private static List<Dispute> SeedDisputes(
        List<Driver> drivers,
        List<Passenger> passengers,
        List<Trip> trips)
    {
        var driver = drivers.First(d => d.Status == DriverStatus.Active);
        var passenger = passengers.First(p => p.IsVerified);
        var trip = trips.First(t => t.Status == TripStatus.Completed);

        return new List<Dispute>
        {
            new Dispute
            {
                PassengerId = passenger.Id,
                DriverId = driver.Id,
                TripId = trip.Id,
                Type = DisputeType.Overcharged,
                Description = "Overcharged fare for short distance node transition.",
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }
}