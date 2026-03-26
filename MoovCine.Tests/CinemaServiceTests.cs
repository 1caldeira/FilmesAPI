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

public class CinemaServiceTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AppDbContext _context;
    private readonly CinemaService _cinemaService;


    public CinemaServiceTests()
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

        _cinemaService = new CinemaService(_mapper, _context, _httpContextAccessorMock.Object);
    }



    [Fact]
    public void AdicionaCinema_DadosValidos_RetornaCinemaCriado()
    {
        var enderecoTest = new CreateEnderecoDTO
        {
            Logradouro = "Rua do Teste",
            Numero = 100
        };

        var cinemaTest = new CreateCinemaDTO
        {
            Nome = "Cine Moov",
            Endereco = enderecoTest
        };

        var cinemaResultado = _cinemaService.AdicionaCinema(cinemaTest);

        Assert.NotNull(cinemaResultado);
        Assert.Equal(enderecoTest.Logradouro, cinemaResultado.Endereco.Logradouro);
        Assert.Equal(enderecoTest.Numero, cinemaResultado.Endereco.Numero);
        Assert.Equal(cinemaTest.Nome, cinemaResultado.Nome);

    }

    [Fact]
    public void AtualizaCinema_ComSessaoFutura_RetornaFailErroSessoesVinculadas()
    {
        var enderecoOriginal = new Endereco
        {
            Id = 1,
            Logradouro = "Rua do Teste",
            Numero = 100
        };


        var cinemaExistente = new Cinema
        {
            Id = 1,
            Nome = "Cine Moov",
            EnderecoId = 1,
            Endereco = enderecoOriginal,
            Sessoes = new List<Sessao>
            {
                
                new Sessao { Id = 1, CinemaId = 1, Horario = DateTime.Now.AddDays(1) }
            }
        };


        _context.Enderecos.Add(enderecoOriginal);
        _context.Cinemas.Add(cinemaExistente);
        _context.SaveChanges();

        var dtoAtualizacao = new UpdateCinemaDTO
        {
            Nome = "Cine Moov Atualizado",
            Endereco = new UpdateEnderecoDTO
            {
                Logradouro = "Rua Nova", 
                Numero = 200             
            }
        };

        var resultado = _cinemaService.AtualizaCinema(1, dtoAtualizacao);


        Assert.True(resultado.IsFailed);

        Assert.Equal(CinemaService.ErroSessoesVinculadas, resultado.Errors[0].Message);
    }

    [Fact]
    public void DeletaCinema_CinemaNaoExiste_RetornaFailErroNaoEncontrado()
    {
        // Arrange 

        // Act 
        var result = _cinemaService.DeletaCinema(99);
        // Assert 
        Assert.True(result.IsFailed);
        Assert.Equal(CinemaService.ErroNaoEncontrado, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaCinema_SemSessoesFuturas_ExcluiLogicamenteComSucesso()
    {
        var cinemaTest = new Cinema
        {
            Id = 100, 
            Nome = "Cine Moov",
            Endereco = new Endereco { Id = 100, Logradouro = "Rua do Teste", Numero = 100 }
        };


        _context.Cinemas.Add(cinemaTest);
        _context.SaveChanges();

        var result = _cinemaService.DeletaCinema(cinemaTest.Id);

        Assert.True(result.IsSuccess);
        var cinemaObj = _context.Cinemas.IgnoreQueryFilters().FirstOrDefault(c => c.Id == cinemaTest.Id);
        Assert.NotNull(cinemaObj);
        Assert.True(cinemaObj.DataExclusao.HasValue);
        Assert.Equal("user-admin-123",cinemaObj.UsuarioExclusaoId);
    }

    [Fact]
    public void AtualizaCinema_SemSessaoFutura_RetornaSucesso()
    {
        var enderecoOriginal = new Endereco
        {
            Id = 1,
            Logradouro = "Rua do Teste",
            Numero = 100
        };


        var cinemaExistente = new Cinema
        {
            Id = 1,
            Nome = "Cine Moov",
            EnderecoId = 1,
            Endereco = enderecoOriginal,
        };


        _context.Enderecos.Add(enderecoOriginal);
        _context.Cinemas.Add(cinemaExistente);
        _context.SaveChanges();

        var dtoAtualizacao = new UpdateCinemaDTO
        {
            Nome = "Cine Moov Atualizado",
            Endereco = new UpdateEnderecoDTO
            {
                Logradouro = "Rua Nova",
                Numero = 200
            }
        };

        var resultado = _cinemaService.AtualizaCinema(1, dtoAtualizacao);


        Assert.True(resultado.IsSuccess);

        var cinemaAtualizado = _context.Cinemas.Include(c => c.Endereco).First(c => c.Id == 1);

        Assert.Equal("Cine Moov Atualizado", cinemaAtualizado.Nome);
        Assert.Equal("Rua Nova", cinemaAtualizado.Endereco.Logradouro);
        Assert.Equal(200, cinemaAtualizado.Endereco.Numero);
    }

    [Fact]
    public void ObterCinemaPorId_CinemaExiste_RetornaCinemaDTO()
    {
        var cinemaTest = new Cinema { Id = 10, Nome = "Cine Test 10", Endereco = new Endereco { Id = 10, Logradouro = "Rua Teste", Numero = 10 } };
        _context.Enderecos.Add(cinemaTest.Endereco);
        _context.Cinemas.Add(cinemaTest);
        _context.SaveChanges();

        var result = _cinemaService.ObterCinemaPorId(10);

        Assert.NotNull(result);
        Assert.Equal("Cine Test 10", result.Nome);
    }

    [Fact]
    public void ObterCinemas_ComFiltro_RetornaLista()
    {
        var end1 = new Endereco { Id = 11, Logradouro = "R1", Numero = 1 };
        var end2 = new Endereco { Id = 12, Logradouro = "R2", Numero = 2 };
        _context.Enderecos.AddRange(end1, end2);
        _context.Cinemas.Add(new Cinema { Id = 11, Nome = "C1", EnderecoId = 11, Endereco = end1 });
        _context.Cinemas.Add(new Cinema { Id = 12, Nome = "C2", EnderecoId = 12, Endereco = end2 });
        _context.SaveChanges();

        var result = _cinemaService.ObterCinemas(0, 10, 11);

        Assert.Single(result);
        Assert.Equal("C1", result[0].Nome);
    }

    [Fact]
    public void RecuperaCinemaParaAtualizar_CinemaExiste_RetornaDTO()
    {
        var cinemaTest = new Cinema { Id = 13, Nome = "Cine Test 13", Endereco = new Endereco { Id = 13, Logradouro = "Rua Teste", Numero = 13 } };
        _context.Enderecos.Add(cinemaTest.Endereco);
        _context.Cinemas.Add(cinemaTest);
        _context.SaveChanges();

        var result = _cinemaService.RecuperaCinemaParaAtualizar(13);

        Assert.NotNull(result);
        Assert.Equal("Cine Test 13", result.Nome);
    }
}