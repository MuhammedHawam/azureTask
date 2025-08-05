using AutoMapper;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Application.Products.Commands.CreateProduct;
using AzureProductApi.Domain.Entities;

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
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.ToList()));

        CreateMap<CreateProductDto, CreateProductCommand>();

        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Price, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.SKU, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}