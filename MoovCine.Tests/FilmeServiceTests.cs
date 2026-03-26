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

public class FilmeServiceTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AppDbContext _context;
    private readonly FilmeService _filmeService;

    public FilmeServiceTests()
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

        _filmeService = new FilmeService(_mapper, _context, _httpContextAccessorMock.Object);
    }

    [Fact]
    public void AdicionaFilme_DadosValidos_RetornaFilmeCriado()
    {
        var filmeTest = new CreateFilmeDTO
        {
            Titulo = "O Poderoso Chefão",
            Duracao = 175,
            Genero = "Ação"
        };

        var result = _filmeService.AdicionaFilme(filmeTest);

        Assert.NotNull(result);
        Assert.Equal(filmeTest.Titulo, result.Titulo);
        Assert.Equal(filmeTest.Duracao, result.Duracao);
    }

    [Fact]
    public void ObterFilmesPorId_FilmeExiste_RetornaDTO()
    {
        _context.Filmes.Add(new Filme { Id = 1, Titulo = "Filme A", Duracao = 120, DataLancamento = DateTime.Now, Genero = "Ação" });
        _context.SaveChanges();

        var result = _filmeService.ObterFilmesPorId(1, false, false);

        Assert.NotNull(result);
        Assert.Equal("Filme A", result.Titulo);
    }

    [Fact]
    public void AtualizaFilme_SemSessaoVinculada_RetornaSucesso()
    {
        var filmeTest = new Filme { Id = 2, Titulo = "Filme B", Duracao = 100, Genero = "Ação" };
        _context.Filmes.Add(filmeTest);
        _context.SaveChanges();

        var dtoAtualizacao = new UpdateFilmeDTO { Titulo = "Filme B Atualizado", Duracao = 110 };
        var result = _filmeService.AtualizaFilme(2, dtoAtualizacao);

        Assert.True(result.IsSuccess);
        var filmeAtualizado = _context.Filmes.Find(2);
        Assert.Equal("Filme B Atualizado", filmeAtualizado.Titulo);
    }

    [Fact]
    public void AtualizaFilme_ComSessaoVinculada_RetornaFailErroSessao()
    {
        var filmeTest = new Filme { Id = 3, Titulo = "Filme C", Duracao = 100, Genero = "Ação" };
        _context.Filmes.Add(filmeTest);
        _context.Sessoes.Add(new Sessao { Id = 1, FilmeId = 3, Horario = DateTime.Now.AddDays(1) });
        _context.SaveChanges();

        var dtoAtualizacao = new UpdateFilmeDTO { Titulo = "Filme C Atualizado", Duracao = 120 };
        var result = _filmeService.AtualizaFilme(3, dtoAtualizacao);

        Assert.True(result.IsFailed);
        Assert.Equal(FilmeService.ErroSessaoVinculada, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaFilme_NaoExiste_RetornaFail()
    {
        var result = _filmeService.DeletaFilme(99);
        Assert.True(result.IsFailed);
        Assert.Equal(FilmeService.ErroNaoEncontrado, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaFilme_ComHistoricoSessoes_ExcluiLogicamente()
    {
        var filmeTest = new Filme { Id = 4, Titulo = "Filme D", Duracao = 90, Genero = "Ação" };
        _context.Filmes.Add(filmeTest);
        _context.Sessoes.Add(new Sessao { Id = 2, FilmeId = 4, Horario = DateTime.Now.AddDays(-1) });
        _context.SaveChanges();

        var result = _filmeService.DeletaFilme(4);

        Assert.True(result.IsSuccess);
        var f = _context.Filmes.IgnoreQueryFilters().First(x => x.Id == 4);
        Assert.True(f.DataExclusao.HasValue);
    }
}
