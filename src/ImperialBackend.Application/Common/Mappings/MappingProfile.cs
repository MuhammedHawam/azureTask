using AutoMapper;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.ValueObjects;

namespace ImperialBackend.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for mapping between domain entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the MappingProfile class
    /// </summary>
    public MappingProfile()
    {
        CreateMap<Outlet, OutletDto>()
            .ForMember(dest => dest.Sales, opt => opt.MapFrom(src => src.Sales.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Sales.Currency))
            .ForMember(dest => dest.StockRisk, opt => opt.MapFrom(src => src.StockRisk));

        CreateMap<Address, AddressDto>().ReverseMap();

        CreateMap<CreateOutletDto, CreateOutletCommand>();

        CreateMap<UpdateOutletDto, Outlet>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Sales, opt => opt.Ignore())
            .ForMember(dest => dest.Address, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}