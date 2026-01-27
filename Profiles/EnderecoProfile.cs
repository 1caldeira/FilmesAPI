using AutoMapper;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Models;

namespace FilmesAPI.Profiles;

public class EnderecoProfile : Profile
{
    public EnderecoProfile() {
        CreateMap<CreateEnderecoDTO, Endereco>();
        CreateMap<UpdateEnderecoDTO, Endereco>();
        CreateMap<Endereco, UpdateEnderecoDTO>();
        CreateMap<Endereco, ReadEnderecoDTO>();
    }
}
