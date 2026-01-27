using AutoMapper;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Models;

namespace FilmesAPI.Profiles;

public class CinemaProfile : Profile
{
    public CinemaProfile() {
        CreateMap<CreateCinemaDTO, Cinema>();
        CreateMap<UpdateCinemaDTO, Cinema>();
        CreateMap<Cinema, UpdateCinemaDTO>();
        CreateMap<Cinema, ReadCinemaDTO>();
    }
}
