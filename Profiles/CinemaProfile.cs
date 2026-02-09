using AutoMapper;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;

namespace FilmesAPI.Profiles;

public class CinemaProfile : Profile
{
    public CinemaProfile() {
        CreateMap<CreateCinemaDTO, Cinema>();
        CreateMap<UpdateCinemaDTO, Cinema>();
        CreateMap<Cinema, UpdateCinemaDTO>();
        CreateMap<ReadCinemaSimpleDTO, Cinema>();
        CreateMap<Cinema, ReadCinemaSimpleDTO>();
        CreateMap<Cinema, ReadCinemaDTO>().ForMember(dto => dto.Sessoes, opt => opt.MapFrom(entity => entity.Sessoes));
    }
}
