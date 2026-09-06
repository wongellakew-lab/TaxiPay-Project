using AutoMapper;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.Common.Mappings;

public class PassengerMappingProfile : Profile
{
    public PassengerMappingProfile()
    {
        CreateMap<Passenger, PassengerResponse>()
            .ForMember(dest => dest.FullName,
                       opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));
    }
}