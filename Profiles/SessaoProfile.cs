using AutoMapper;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;


namespace SessoesAPI.Profiles;

public class SessaoProfile : Profile
{
    public SessaoProfile() {
        CreateMap<CreateSessaoDTO, Sessao>();
        CreateMap<UpdateSessaoDTO, Sessao>();
        CreateMap<Sessao, UpdateSessaoDTO>();
        CreateMap<Sessao, ReadSessaoSimpleDTO>();
        CreateMap<ReadSessaoSimpleDTO, Sessao>();
        CreateMap<Sessao, ReadSessaoDTO>().ForMember(dto => dto.Horario,opt => opt.MapFrom(entity => entity.Horario.ToString("dd-MM-yyyy HH:mm")));
        CreateMap<Sessao, ReadSessaoSimpleDTO>().ForMember(dto => dto.Horario, opt => opt.MapFrom(entity => entity.Horario.ToString("dd-MM-yyyy HH:mm")));
    }
}

