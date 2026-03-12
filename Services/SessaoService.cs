using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;



namespace FilmesAPI.Services;

public class SessaoService
{
    private IMapper _mapper;
    private AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessaoService(IMapper mapper, AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _mapper = mapper;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public const string ErroNaoEncontrado = "Sessao não encontrada!";
    public const string ErroSessaoJaPassou = "Só é possivel cancelar uma sessão que ainda não começou";
    public const string ErroHorarioIndisponivel = "O horário escolhido para a nova sessão está indisponível.";
    public const string ErroSessaoNoPassado = "Não é possível criar uma sessão no passado";

    private string GetUserId()
    {
        var user = _httpContextAccessor.HttpContext!.User;
        var id = user.FindFirst("id")!.Value;
        return id;
    }

    public Result<ReadSessaoDTO> AdicionaSessao(CreateSessaoDTO sessaoDTO)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == sessaoDTO.FilmeId);
        if (filme == null) return Result.Fail(FilmeService.ErroNaoEncontrado);

        var cinema = _context.Cinemas.FirstOrDefault(c => c.Id == sessaoDTO.CinemaId);
        if (cinema == null) return Result.Fail(CinemaService.ErroNaoEncontrado);

        var horarioFimNovaSessao = sessaoDTO.Horario.AddMinutes(filme.Duracao);

        var sessaoConflitante = ObterSessaoConflitante(
            sessaoDTO.CinemaId,
            sessaoDTO.Sala,
            sessaoDTO.Horario,
            filme.Duracao,
            null
        );

        if (sessaoConflitante != null)
        {
            var inicio = sessaoConflitante.Horario.ToString("HH:mm");
            var fim = sessaoConflitante.Horario.AddMinutes(sessaoConflitante.Filme.Duracao).ToString("HH:mm");
            var dia = sessaoConflitante.Horario.Date.ToString("dd/MM/yyyy");

            return Result.Fail($"Sala ocupada por: {sessaoConflitante.Filme.Titulo} ({dia} - {inicio} às {fim})");
        }
        if (sessaoDTO.Horario < DateTime.Now)
        {
            return Result.Fail(ErroSessaoNoPassado);
        }
        Sessao sessao = _mapper.Map<Sessao>(sessaoDTO);
        _context.Sessoes.Add(sessao);
        _context.SaveChanges();
        return Result.Ok(_mapper.Map<ReadSessaoDTO>(sessao));
    }

    public List<ReadSessaoDTO> ObterSessoes(FiltroSessaoDTO filtro)
    {
        var query = _context.Sessoes.AsQueryable();

        if (filtro.CinemaId.HasValue)
        {
            query = query.Where(s => s.CinemaId == filtro.CinemaId);
        }

        if (filtro.FilmeId.HasValue)
        {
            query = query.Where(s => s.FilmeId == filtro.FilmeId);
        }

        if (!string.IsNullOrEmpty(filtro.NomeFilme))
        {
            var termoBusca = filtro.NomeFilme.ToLower();
            query = query.Where(s => s.Filme.Titulo.ToLower().Contains(termoBusca));
        }

        if (filtro.SomenteDisponiveis)
        {
            query = query.Where(s => s.Horario >= DateTime.Now);
        }


        var sessoes = query
            .OrderBy(s => s.Horario)
            .Skip(filtro.Skip)
            .Take(filtro.Take)
            .ToList();

        return _mapper.Map<List<ReadSessaoDTO>>(sessoes);
    }

    public ReadSessaoDTO ObterSessoesPorId(int id)
    {
        var sessao = _context.Sessoes.FirstOrDefault(f => f.Id == id);
        if (sessao == null) return null;
        return _mapper.Map<ReadSessaoDTO>(sessao);
    }

    public Result AtualizaSessoes(int id, UpdateSessaoDTO sessaoDTO)
    {
        var sessao = _context.Sessoes.FirstOrDefault(s => s.Id == id);
        if (sessao == null) return Result.Fail(ErroNaoEncontrado);

        int filmeIdParaValidar = sessaoDTO.FilmeId != 0 ? sessaoDTO.FilmeId : sessao.FilmeId;
        var filmeParaValidar = _context.Filmes.FirstOrDefault(f => f.Id == filmeIdParaValidar);
        if (filmeParaValidar == null) return Result.Fail(FilmeService.ErroNaoEncontrado);

        if (TemConflitoDeHorario(sessao.CinemaId, sessaoDTO.Sala, sessaoDTO.Horario, filmeParaValidar.Duracao, id))
        {
            return Result.Fail(ErroHorarioIndisponivel);
        }

        _mapper.Map(sessaoDTO, sessao);
        _context.SaveChanges();
        return Result.Ok();
    }

    public UpdateSessaoDTO? RecuperaSessoesParaAtualizar(int id)
    {
        var sessao = _context.Sessoes.FirstOrDefault(s => s.Id == id);
        if (sessao == null) return null;
        return _mapper.Map<UpdateSessaoDTO>(sessao);
    }

    public Result DeletaSessoes(int id)
    {
        Sessao sessao = _context.Sessoes.FirstOrDefault(s => s.Id == id)!;
        if (sessao == null) return Result.Fail(ErroNaoEncontrado);

        if (sessao.Horario <= DateTime.Now)
        {
            return Result.Fail(ErroSessaoJaPassou);
        }
        sessao.DataExclusao = DateTime.Now;
        sessao.UsuarioExclusaoId = GetUserId();
        _context.SaveChanges();
        return Result.Ok();
    }

    private bool TemConflitoDeHorario(int cinemaId, int sala, DateTime horarioInicio, int duracaoFilme, int? sessaoIdIgnorar)
    {
        var horarioFim = horarioInicio.AddMinutes(duracaoFilme);

        return _context.Sessoes.Local.ToList().Any(s =>
            s.CinemaId == cinemaId &&
            s.Sala == sala &&
            s.DataExclusao == null &&
            (sessaoIdIgnorar == null || s.Id != sessaoIdIgnorar) &&
            horarioInicio < s.Horario.AddMinutes(s.Filme.Duracao) &&
            horarioFim > s.Horario);
    }

    private Sessao? ObterSessaoConflitante(int cinemaId, int sala, DateTime horarioInicio, int duracaoFilme, int? sessaoIdIgnorar)
    {
        var horarioFim = horarioInicio.AddMinutes(duracaoFilme);

        var query = _context.Sessoes
            .Include(s => s.Filme)
            .AsQueryable();

        var conflito = query.FirstOrDefault(s =>
            s.CinemaId == cinemaId
            && s.Sala == sala
            && s.DataExclusao == null
            && (sessaoIdIgnorar == null || s.Id != sessaoIdIgnorar)
            && horarioInicio < s.Horario.AddMinutes(s.Filme.Duracao)
            && horarioFim > s.Horario
        );

        return conflito;
    }

    public async Task<int> GerarSessoesAutomaticamente()
    {
        var hoje = DateTime.Now;
        var random = new Random();

        var gradeMestre = new List<TimeSpan> {
        new TimeSpan(12,0,0), new TimeSpan(14,0,0), new TimeSpan(15,30,0),
        new TimeSpan(17,30,0), new TimeSpan(20,0,0), new TimeSpan(21,0,0)
    };

        var dataCorte = hoje.AddMonths(-2);
        var filmesDisponiveis = await _context.Filmes.Where(f => f.DataLancamento >= dataCorte).ToListAsync();
        var cinemas = await _context.Cinemas.ToListAsync();
        int contador = 0;

        await _context.Sessoes.ToListAsync();

        foreach (var cinema in cinemas)
        {
            // inversão de grandeza (mais salas = menor exclusão)
            int baseExclusaoCinema = Math.Max(10, 95 - (cinema.NumeroSalas * 10));

            var filmesNesteCinema = filmesDisponiveis.Where(f =>
            {
                double bonusPopularidade = Math.Log10(Math.Max(1, f.Popularidade)) * 20;

                int chanceExclusaoFinal = (int)Math.Clamp(baseExclusaoCinema - bonusPopularidade, 5, 95);

                return random.Next(100) >= chanceExclusaoFinal;
            }).ToList();

            if (!filmesNesteCinema.Any() && filmesDisponiveis.Any())
                filmesNesteCinema = filmesDisponiveis.OrderByDescending(f => f.Popularidade).Take(2).ToList();

            for (int sala = 1; sala <= cinema.NumeroSalas; sala++)
            {
                foreach (var hora in gradeMestre)
                {
                    if (random.Next(100) < 20) continue;

                    var filmesPorSucesso = filmesNesteCinema
                    .OrderByDescending(f => f.Popularidade)
                    .ToList();
                    Filme? filmeEscolhido = null;

                    foreach (var f in filmesPorSucesso)
                    {
                        DateTime dataTeste = hoje.Date.AddDays(1).Add(hora);
                        if (!FilmeJaEstaNesseHorario(cinema.Id, f.Id, dataTeste))
                        {
                            filmeEscolhido = f;
                            break;
                        }
                    }

                    if (filmeEscolhido == null) continue;

                    for (int dia = 0; dia <= 7; dia++)
                    {
                        var dataReferencia = hoje.Date.AddDays(dia);
                        var horarioSessao = dataReferencia.Add(hora);

                        if (horarioSessao < hoje) continue;
                        if (filmeEscolhido.DataLancamento.Date > dataReferencia) continue;

                        if (!TemConflitoDeHorario(cinema.Id, sala, horarioSessao, filmeEscolhido.Duracao, null))
                        {
                            _context.Sessoes.Add(new Sessao
                            {
                                FilmeId = filmeEscolhido.Id,
                                CinemaId = cinema.Id,
                                Horario = horarioSessao,
                                Sala = sala
                            });
                            contador++;
                        }
                    }
                }
            }
        }
        await _context.SaveChangesAsync();
        return contador;
    }

    private bool FilmeJaEstaNesseHorario(int cinemaId, int filmeId, DateTime horario)
    {
        return _context.Sessoes.Local.ToList().Any(s =>
            s.CinemaId == cinemaId && s.FilmeId == filmeId && s.Horario == horario);
    }
    
}