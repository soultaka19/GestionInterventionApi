using AutoMapper;
using GestionInterventionApi.DTOs.Organization;
using GestionInterventionApi.DTOs.User;
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
    }
}
