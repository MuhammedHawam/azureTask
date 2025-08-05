using AutoMapper;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Application.Outlets.Commands.CreateOutlet;
using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.ValueObjects;

namespace AzureProductApi.Application.Common.Mappings;

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
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Sales.Currency));

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