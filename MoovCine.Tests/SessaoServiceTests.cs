using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using FilmesAPI.Profiles;
using FilmesAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SessoesAPI.Profiles;
using System.Security.Claims;
using Xunit;

namespace MoovCine.Tests.Services;

public class SessaoServiceTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AppDbContext _context;
    private readonly SessaoService _sessaoService;

    public SessaoServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new CinemaProfile());
            cfg.AddProfile(new EnderecoProfile());
            cfg.AddProfile(new FilmeProfile());
            cfg.AddProfile(new SessaoProfile());
            cfg.AddProfile(new UsuarioProfile());
        });
        _mapper = mapperConfig.CreateMapper();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var claims = new List<Claim> { new Claim("id", "user-admin-123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _sessaoService = new SessaoService(_mapper, _context, _httpContextAccessorMock.Object);
    }

    [Fact]
    public void AdicionaSessao_DadosValidos_RetornaSucesso()
    {
        var filme = new Filme { Id = 1, Titulo = "Filme 1", Duracao = 120, Genero = "Ação" };
        var cinema = new Cinema { Id = 1, Nome = "Cinema 1" };
        _context.Filmes.Add(filme);
        _context.Cinemas.Add(cinema);
        _context.SaveChanges();

        var sessaoDTO = new CreateSessaoDTO
        {
            FilmeId = 1,
            CinemaId = 1,
            Sala = 1,
            Horario = DateTime.Now.AddDays(1)
        };

        var result = _sessaoService.AdicionaSessao(sessaoDTO);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Sala);
    }

    [Fact]
    public void AdicionaSessao_ConflitoDeHorario_RetornaFail()
    {
        var filme = new Filme { Id = 2, Titulo = "Filme 2", Duracao = 120, Genero = "Ação" };
        var cinema = new Cinema { Id = 2, Nome = "Cinema 2" };
        _context.Filmes.Add(filme);
        _context.Cinemas.Add(cinema);
        
        var sessaoExistente = new Sessao
        {
            Id = 1,
            FilmeId = 2,
            CinemaId = 2,
            Sala = 2,
            Horario = DateTime.Now.AddDays(1)
        };
        _context.Sessoes.Add(sessaoExistente);
        _context.SaveChanges();

        var sessaoDTO = new CreateSessaoDTO
        {
            FilmeId = 2,
            CinemaId = 2,
            Sala = 2,
            Horario = DateTime.Now.AddDays(1).AddMinutes(30) // Dentro da duracao do outro filme
        };

        var result = _sessaoService.AdicionaSessao(sessaoDTO);

        Assert.True(result.IsFailed);
        Assert.Contains("Sala ocupada por", result.Errors[0].Message);
    }

    [Fact]
    public void ObterSessoes_ComFiltro_RetornaResultado()
    {
        var filme = new Filme { Id = 3, Titulo = "Filme 3", Duracao = 120, Genero = "Ação" };
        _context.Filmes.Add(filme);
        _context.Sessoes.Add(new Sessao { Id = 3, FilmeId = 3, CinemaId = 3, Horario = DateTime.Now.AddDays(1) });
        _context.SaveChanges();

        var filtro = new FiltroSessaoDTO { FilmeId = 3, Skip = 0, Take = 10 };
        var sessoes = _sessaoService.ObterSessoes(filtro);

        Assert.Single(sessoes);
        Assert.Equal(3, sessoes[0].Id);
    }
    [Fact]
    public void AdicionaSessao_SessaoNoPassado_RetornaFail()
    {
        var filmeTest = new Filme { Id = 4, Titulo = "Filme D", Duracao = 90, Genero = "Ação" };
        _context.Filmes.Add(filmeTest);
        var end1 = new Endereco { Id = 12, Logradouro = "R2", Numero = 2 };
        _context.Enderecos.Add(end1);
        _context.Cinemas.Add(new Cinema { Id = 1, Nome = "C1", EnderecoId = 12, Endereco = end1 });
        _context.SaveChanges();
        var sessaoDTO = new CreateSessaoDTO
        {
            FilmeId = 4,
            CinemaId = 1,
            Sala = 1,
            Horario = DateTime.Now.AddDays(-1)
        };
        var result = _sessaoService.AdicionaSessao(sessaoDTO);
        Assert.True(result.IsFailed);
        Assert.Equal(SessaoService.ErroSessaoNoPassado, result.Errors[0].Message);
    }

    [Fact]
    public void AdicionaSessao_FilmeOuCinemaNaoExistem_RetornaFail()
    {
        var sessaoDTO = new CreateSessaoDTO
        {
            FilmeId = 999,
            CinemaId = 999,
            Sala = 1,
            Horario = DateTime.Now.AddDays(1)
        };
        var result = _sessaoService.AdicionaSessao(sessaoDTO);
        Assert.True(result.IsFailed);
    }
    [Fact]
    public void DeletaSessoes_SessaoNaoEncontrada_RetornaFail()
    {
        var result = _sessaoService.DeletaSessoes(99);
        Assert.True(result.IsFailed);
        Assert.Equal(SessaoService.ErroNaoEncontrado, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaSessoes_DadosValidos_ExcluiLogicamente()
    {
        var sessao = new Sessao { Id = 4, FilmeId = 1, CinemaId = 1, Sala = 1, Horario = DateTime.Now.AddDays(1) };
        _context.Sessoes.Add(sessao);
        _context.SaveChanges();

        var result = _sessaoService.DeletaSessoes(4);

        Assert.True(result.IsSuccess);
        
        var s = _context.Sessoes.IgnoreQueryFilters().First(x => x.Id == 4);
        Assert.True(s.DataExclusao.HasValue);
    }

    [Fact]
    public void DeletaSessoes_SessaoPassada_RetornaFail()
    {
        var sessao = new Sessao { Id = 5, FilmeId = 1, CinemaId = 1, Sala = 1, Horario = DateTime.Now.AddDays(-1) };
        _context.Sessoes.Add(sessao);
        _context.SaveChanges();

        var result = _sessaoService.DeletaSessoes(5);

        Assert.True(result.IsFailed);
        Assert.Equal(SessaoService.ErroSessaoJaPassou, result.Errors[0].Message);
    }
}
