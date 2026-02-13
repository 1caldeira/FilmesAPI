namespace FilmesAPI.Services;

using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using System;


public class CinemaService
{
    private IMapper _mapper;
    private AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CinemaService(IMapper mapper, AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _mapper = mapper;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public const string ErroNaoEncontrado = "Cinema não encontrado!";
    public const string ErroSessoesVinculadas = "Não é possível excluir o cinema ou editar seu endereço, pois ainda existem sessões pendentes!";

    private string GetUserId()
    {
        var user = _httpContextAccessor.HttpContext!.User;
        var id = user.FindFirst("id")!.Value;
        return id;
    }

    public ReadCinemaDTO AdicionaCinema(CreateCinemaDTO cinemaDTO)
    {
        var cinema = _mapper.Map<Cinema>(cinemaDTO);
        _context.Cinemas.Add(cinema);
        _context.SaveChanges();
        return _mapper.Map<ReadCinemaDTO>(cinema);
    }

    
    public ReadCinemaDTO? ObterCinemaPorId(int id)
    {
        var cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id);
        if (cinema == null) return null;
        return _mapper.Map<ReadCinemaDTO>(cinema);
    }

    public List<ReadCinemaDTO> ObterCinemas(int skip, int take, int? enderecoId)
    {
        IQueryable<Cinema> query = _context.Cinemas
        .Include(c => c.Endereco)           
        .Include(c => c.Sessoes)            
            .ThenInclude(s => s.Filme)                    
        .AsQueryable();

        if (enderecoId != null)
        {
            query = query.Where(cinema => cinema.EnderecoId == enderecoId);
        }
        var listaDeCinemas = query.OrderBy(c => c.Id).Skip(skip).Take(take).ToList();

        return _mapper.Map<List<ReadCinemaDTO>>(listaDeCinemas);
    }

    public UpdateCinemaDTO? RecuperaCinemaParaAtualizar(int id)
    {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return null;
        return _mapper.Map<UpdateCinemaDTO>(cinema);
    }

    public Result AtualizaCinema(int id, UpdateCinemaDTO cinemaDto)
    {
        var cinema = _context.Cinemas.Include(c => c.Endereco).Include(c => c.Sessoes).FirstOrDefault(c => c.Id == id);
        if (cinema == null) return Result.Fail(ErroNaoEncontrado);

        bool temSessaoFutura = cinema.Sessoes.Any(s => s.CinemaId == id && s.Horario > DateTime.Now);

        bool mudouEndereco = cinema.Endereco.Logradouro != cinemaDto.Endereco.Logradouro
            || cinema.Endereco.Numero != cinemaDto.Endereco.Numero;

        if (mudouEndereco && temSessaoFutura) 
        {
                return Result.Fail(ErroSessoesVinculadas);
        }
        _mapper.Map(cinemaDto, cinema);
        _context.SaveChanges();
        return Result.Ok();
    }

    public Result DeletaCinema(int id)
    {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return Result.Fail(ErroNaoEncontrado);

        bool temSessaoFutura = _context.Sessoes.Any(s => s.CinemaId == id && s.Horario > DateTime.Now);
        if (temSessaoFutura) {
            return Result.Fail(ErroSessoesVinculadas);
        }
        cinema.DataExclusao = DateTime.Now;
        cinema.UsuarioExclusaoId = GetUserId();
        _context.SaveChanges();
        return Result.Ok();
    }
}