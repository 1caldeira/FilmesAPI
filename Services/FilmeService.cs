using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using Microsoft.EntityFrameworkCore;
using FluentResults;

namespace FilmesAPI.Services;

public class FilmeService
{
    private IMapper _mapper;
    private AppDbContext _context;

    public FilmeService(IMapper mapper, AppDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public const string ErroNaoEncontrado = "Filme não encontrado!";
    public const string ErroSessaoVinculada = "Não é possível excluir ou editar o filme pois existem sessoes vinculadas!";

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
        if (dto.ApenasDisponiveis)
        {
            query = query.Include(f => f.Sessoes.Where(s => s.Horario.AddMinutes(Sessao.ToleranciaAtrasoMinutos) >= DateTime.Now));
        }

        return _mapper.Map<List<ReadFilmeDTO>>(query.OrderByDescending(f => f.Titulo).Skip(dto.Skip).Take(dto.Take).ToList());
    }

    public ReadFilmeDTO ObterFilmesPorId(int id)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return null;
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

    public Result DeletaFilme(int id)
    {
        Filme filme = _context.Filmes.FirstOrDefault(c => c.Id == id)!;
        if (filme == null) return Result.Fail(ErroNaoEncontrado);

        var sessaoVinculada = _context.Sessoes.Any(s => s.FilmeId == id && s.Horario > DateTime.Now);

        if (sessaoVinculada)
        {
            return Result.Fail(ErroSessaoVinculada);
        }
        filme.DataExclusao = DateTime.Now;
        _context.SaveChanges();
        return Result.Ok();
    }
}