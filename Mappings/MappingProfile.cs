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
        // Correction ici : Utilisation de ForCtorParam pour les records
        CreateMap<Client, ClientDto>()
            .ForCtorParam("EquipmentCount", opt => opt.MapFrom(src => src.Equipments != null ? src.Equipments.Count : 0));

        CreateMap<Client, ClientDetailDto>();

        CreateMap<CreateClientDto, Client>();

        // B-7 — une mise a jour n'ecrase pas un champ absent.
        //
        // `_mapper.Map(updateDto, client)` recopiait TOUS les membres du DTO sur
        // l'entite chargee, y compris ceux laisses a null. Le front n'envoyant
        // qu'une partie des champs, un simple changement de nom effacait au
        // passage ville, code postal, telephone, coordonnees et notes — sans
        // aucune erreur, la perte n'etant visible qu'au rechargement.
        //
        // Contrepartie assumee : un champ optionnel ne peut plus etre VIDE en
        // envoyant null ; il faut envoyer une chaine vide. Perdre la capacite
        // d'effacer coute moins cher que d'effacer par accident.
        CreateMap<UpdateClientDto, Client>()
            .ForAllMembers(opt => opt.Condition((_, _, valeurSource) => valeurSource != null));

        // Equipment
        CreateMap<Equipment, EquipmentDto>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.Name));
        CreateMap<CreateEquipmentDto, Equipment>();
        // Meme raison que pour UpdateClientDto ci-dessus (B-7).
        CreateMap<UpdateEquipmentDto, Equipment>()
            .ForAllMembers(opt => opt.Condition((_, _, valeurSource) => valeurSource != null));
    }
}