using AutoMapper;
using GestionInterventionApi.DTOs.Organization;
using GestionInterventionApi.DTOs.User;
using GestionInterventionApi.DTOs.Client;
using GestionInterventionApi.DTOs.Equipment;
using GestionInterventionApi.Models;

namespace GestionInterventionApi.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Organization
        CreateMap<Organization, OrganizationDto>();
        CreateMap<CreateOrganizationDto, Organization>();
        CreateMap<UpdateOrganizationDto, Organization>();

        // User
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        CreateMap<UpdateUserDto, User>();

        // Client
        CreateMap<Client, ClientDto>()
            .ForMember(dest => dest.EquipmentCount, opt => opt.MapFrom(src => src.Equipments.Count));
        CreateMap<Client, ClientDetailDto>()
            .ForMember(dest => dest.Equipments, opt => opt.MapFrom(src => src.Equipments));
        CreateMap<CreateClientDto, Client>();
        CreateMap<UpdateClientDto, Client>();

        // Equipment
        CreateMap<Equipment, EquipmentDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.Name));
        CreateMap<CreateEquipmentDto, Equipment>();
        CreateMap<UpdateEquipmentDto, Equipment>();
    }
}
