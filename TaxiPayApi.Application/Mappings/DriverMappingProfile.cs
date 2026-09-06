using AutoMapper;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.Mappings;

public class DriverMappingProfile : Profile
{
    public DriverMappingProfile()
    {
        CreateMap<CreateDriverRequest, Driver>()
            .ForAllMembers(opt => opt.Condition((_, _, srcMember) => srcMember != null));

        CreateMap<UpdateProfileRequest, Driver>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName.Trim()))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName.Trim()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone.Trim()))
            .ForAllMembers(opt => opt.Condition((_, _, srcMember) => srcMember != null));

        CreateMap<Driver, DriverResponse>()
            .ForMember(dest => dest.CurrentRouteName,
                opt => opt.MapFrom(src => src.CurrentRoute != null ? src.CurrentRoute.Name : null));

        CreateMap<Driver, DriverSummaryDto>()
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.RouteName,
                opt => opt.MapFrom(src => src.CurrentRoute != null ? src.CurrentRoute.Name : null))
            .ForMember(dest => dest.RouteCode,
                opt => opt.MapFrom(src => src.CurrentRoute != null ? src.CurrentRoute.RouteCode : null))
            .ForMember(dest => dest.RouteNodes,
                opt => opt.MapFrom(src => src.CurrentRoute != null ? src.CurrentRoute.Nodes : new List<Node>()));

        CreateMap<Node, NodeDto>();
        CreateMap<Route, RouteSummaryDto>();

        CreateMap<DriverShift, ShiftStatusDto>()
            .ForMember(dest => dest.ShiftId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.ShiftStart))
            .ForMember(dest => dest.IsActive,
                opt => opt.MapFrom(src => src.Status == ShiftStatus.Active));

        CreateMap<Trip, TripSummaryDto>()
            .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Trip, TripResponse>()
            .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ShiftId, opt => opt.MapFrom(src => src.DriverShiftId))
            .ForMember(dest => dest.DriverName, opt => opt.MapFrom(src => ResolveDriverName(src)))
            .ForMember(dest => dest.RouteName, opt => opt.MapFrom(src => ResolveRouteName(src)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<PassengerJourney, ActivePassengerDto>()
            .ForMember(dest => dest.JourneyId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PassengerName,
                opt => opt.MapFrom(src =>
                    src.Passenger != null
                        ? $"{src.Passenger.FirstName} {src.Passenger.LastName}"
                        : "Unknown"))
            .ForMember(dest => dest.PassengerPhone,
                opt => opt.MapFrom(src => src.Passenger != null ? src.Passenger.Phone : "Unknown"))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
            .ForMember(dest => dest.BoardedAt, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<RouteChangeRequest, RouteChangeResponse>()
            .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DriverName,
                opt => opt.MapFrom(src =>
                    src.Driver != null
                        ? $"{src.Driver.FirstName} {src.Driver.LastName}"
                        : string.Empty))
            .ForMember(dest => dest.CurrentRoute, opt => opt.MapFrom(src => src.CurrentRoute!))
            .ForMember(dest => dest.RequestedRoute, opt => opt.MapFrom(src => src.NewRoute!))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }

    private static string ResolveDriverName(Trip trip)
    {
        if (trip.Driver != null)
            return $"{trip.Driver.FirstName} {trip.Driver.LastName}";

        if (trip.DriverShift?.Driver != null)
            return $"{trip.DriverShift.Driver.FirstName} {trip.DriverShift.Driver.LastName}";

        return "Unknown";
    }

    private static string ResolveRouteName(Trip trip)
    {
        if (trip.Route != null)
            return trip.Route.Name;

        if (trip.DriverShift?.Route != null)
            return trip.DriverShift.Route.Name;

        return "Unknown";
    }
}
