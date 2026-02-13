using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;


namespace FilmesAPI.Services;

public class FilmeService
{
    private IMapper _mapper;
    private AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FilmeService(IMapper mapper, AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _mapper = mapper;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public const string ErroNaoEncontrado = "Filme não encontrado!";
    public const string ErroSessaoVinculada = "Não é possível excluir ou editar o filme pois existem sessoes vinculadas!";

    private string GetUserId()
    {
        var user = _httpContextAccessor.HttpContext!.User;
        var id = user.FindFirst("id")!.Value;
        return id;
    }

    public ReadFilmeDTO AdicionaFilme(CreateFilmeDTO filmeDTO)
    {
        Filme filme = _mapper.Map<Filme>(filmeDTO);
        _context.Filmes.Add(filme);
        _context.SaveChanges();
        return _mapper.Map<ReadFilmeDTO>(filme);
    }

    public List<ReadFilmeDTO> ObterFilmes(FiltroFilmeDTO dto)
    {
        var query = _context.Filmes.AsQueryable();


        if (dto.CinemaId != null)
        {
            query = query.Where(f => f.Sessoes.Any(s => s.CinemaId == dto.CinemaId));
        }

        if (!string.IsNullOrEmpty(dto.NomeFilme))
        {
            query = query.Where(f => f.Titulo.Contains(dto.NomeFilme));
        }


        IOrderedQueryable<Filme> queryOrdenada;

        if (dto.ApenasDisponiveis)
        {
            query = query.Where(f => f.Sessoes.Any(s => s.Horario.AddMinutes(Sessao.ToleranciaAtrasoMinutos) >= DateTime.Now));

            
            query = query.Include(f => f.Sessoes.Where(s => s.Horario.AddMinutes(Sessao.ToleranciaAtrasoMinutos) >= DateTime.Now))
                         .ThenInclude(s => s.Cinema)
                         .ThenInclude(c => c.Endereco);

            queryOrdenada = query.OrderByDescending(f => f.Sessoes.Count).ThenBy(f => f.Titulo);
        }
        else
        {
            // --- MODO ADMIN---

            query = query.IgnoreQueryFilters();


            query = query.Include(f => f.Sessoes)
                         .ThenInclude(s => s.Cinema)
                         .ThenInclude(c => c.Endereco);

            
            queryOrdenada = query.OrderByDescending(f => f.DataLancamento);
        }


        var listaFilmes = queryOrdenada
            .Skip(dto.Skip)
            .Take(dto.Take)
            .ToList();

        return _mapper.Map<List<ReadFilmeDTO>>(listaFilmes);
    }

    public ReadFilmeDTO? ObterFilmesPorId(int id, bool isAdmin, bool verSessoesPassadas)
    {
        var query = _context.Filmes.AsQueryable();

        if (isAdmin && verSessoesPassadas)
        {
            query = query.Include(f => f.Sessoes.Where(s => s.DataExclusao == null))
                     .ThenInclude(s => s.Cinema)
                     .ThenInclude(c => c.Endereco);
        }
        else {
            query = query
                .Include(f => f.Sessoes.Where(s => s.Horario >= DateTime.Now))
                .ThenInclude(s => s.Cinema)
                .ThenInclude(c => c.Endereco);
                
        }

        var filme = query.FirstOrDefault(f => f.Id == id);


        if (filme == null) return null;

        filme.Sessoes = filme.Sessoes.OrderBy(s => s.Horario).ToList();

        return _mapper.Map<ReadFilmeDTO>(filme);
    }

    public Result AtualizaFilme(int id, UpdateFilmeDTO filmeDTO)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return Result.Fail(ErroNaoEncontrado);

        if (filme.Duracao != filmeDTO.Duracao)
        {
            bool temSessoesVinculadas = _context.Sessoes.Any(s => s.FilmeId == id);

            if (temSessoesVinculadas)
            {
                return Result.Fail(ErroSessaoVinculada);
            }
        }

        _mapper.Map(filmeDTO, filme);
        _context.SaveChanges();
        return Result.Ok();
    }

    public UpdateFilmeDTO? RecuperaFilmeParaAtualizar(int id)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return null;
        return _mapper.Map<UpdateFilmeDTO>(filme);
    }

    public Result DeletaFilme(int id, bool force = false)
    {
        var filme = _context.Filmes
        .IgnoreQueryFilters()
        .Include(f => f.Sessoes)
        .FirstOrDefault(c => c.Id == id);

        if (filme == null) return Result.Fail(ErroNaoEncontrado);

        var sessaoVinculada = filme.Sessoes.Any(s => s.Horario > DateTime.Now.AddMinutes(Sessao.ToleranciaAtrasoMinutos));

        if (sessaoVinculada)
        {
            return Result.Fail(ErroSessaoVinculada);
        }
       
        bool possuiHistorico = filme.Sessoes.Any();

        if (possuiHistorico)
        {
            filme.DataExclusao = DateTime.Now;
            filme.UsuarioExclusaoId = GetUserId();
        }
        else
        {
            if (!force) {
                return Result.Fail("CONFIRM_HARD_DELETE");
            }
            _context.Filmes.Remove(filme);
        }

        _context.SaveChanges();
        return Result.Ok();
    }
}