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

public class EnderecoServiceTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AppDbContext _context;
    private readonly EnderecoService _enderecoService;

    public EnderecoServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new EnderecoProfile());
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

        _enderecoService = new EnderecoService(_mapper, _context, _httpContextAccessorMock.Object);
    }

    [Fact]
    public void AdicionaEndereco_DadosValidos_RetornaEnderecoCriado()
    {
        var enderecoTest = new CreateEnderecoDTO
        {
            Logradouro = "Rua do Teste",
            Numero = 100
        };

        var enderecoResultado = _enderecoService.AdicionaEndereco(enderecoTest);

        Assert.NotNull(enderecoResultado);
        Assert.Equal(enderecoTest.Logradouro, enderecoResultado.Logradouro);
        Assert.Equal(enderecoTest.Numero, enderecoResultado.Numero);
    }

    [Fact]
    public void ObterEnderecoPorId_EnderecoExiste_RetornaDTO()
    {
        var enderecoTest = new Endereco { Id = 1, Logradouro = "Rua Teste", Numero = 100 };
        _context.Enderecos.Add(enderecoTest);
        _context.SaveChanges();

        var result = _enderecoService.ObterEnderecoPorId(1);

        Assert.NotNull(result);
        Assert.Equal("Rua Teste", result.Logradouro);
        Assert.Equal(100, result.Numero);
    }

    [Fact]
    public void ObterEnderecoPorId_EnderecoNaoExiste_RetornaNull()
    {
        var result = _enderecoService.ObterEnderecoPorId(99);
        Assert.Null(result);
    }

    [Fact]
    public void ObterEnderecos_RetornaLista()
    {
        _context.Enderecos.Add(new Endereco { Id = 2, Logradouro = "Rua A", Numero = 1 });
        _context.Enderecos.Add(new Endereco { Id = 3, Logradouro = "Rua B", Numero = 2 });
        _context.SaveChanges();

        var result = _enderecoService.ObterEnderecos(0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void RecuperaEnderecoParaAtualizar_EnderecoExiste_RetornaDTO()
    {
        var enderecoTest = new Endereco { Id = 4, Logradouro = "Rua C", Numero = 3 };
        _context.Enderecos.Add(enderecoTest);
        _context.SaveChanges();

        var result = _enderecoService.RecuperaEnderecoParaAtualizar(4);

        Assert.NotNull(result);
        Assert.Equal("Rua C", result.Logradouro);
        Assert.Equal(3, result.Numero);
    }

    [Fact]
    public void AtualizaEndereco_EnderecoExiste_RetornaSucesso()
    {
        var enderecoTest = new Endereco { Id = 5, Logradouro = "Rua D", Numero = 4 };
        _context.Enderecos.Add(enderecoTest);
        _context.SaveChanges();

        var result = _enderecoService.AtualizaEndereco(5, new UpdateEnderecoDTO { Logradouro = "Rua Nova", Numero = 5 });

        Assert.True(result.IsSuccess);
        var enderecoAtualizado = _context.Enderecos.Find(5);
        Assert.Equal("Rua Nova", enderecoAtualizado.Logradouro);
        Assert.Equal(5, enderecoAtualizado.Numero);
    }

    [Fact]
    public void DeletaEndereco_EnderecoNaoExiste_RetornaFail()
    {
        var result = _enderecoService.DeletaEndereco(99);
        Assert.True(result.IsFailed);
        Assert.Equal(EnderecoService.ErroNaoEncontrado, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaEndereco_CinemaVinculado_RetornaFail()
    {
        var enderecoTest = new Endereco { Id = 6, Logradouro = "Rua E", Numero = 6 };
        _context.Enderecos.Add(enderecoTest);
        _context.Cinemas.Add(new Cinema { Id = 1, Nome = "Cinema Teste", EnderecoId = 6, Endereco = enderecoTest });
        _context.SaveChanges();

        var result = _enderecoService.DeletaEndereco(6);

        Assert.True(result.IsFailed);
        Assert.Contains(EnderecoService.ErroCinemaVinculado, result.Errors[0].Message);
    }

    [Fact]
    public void DeletaEndereco_DadosValidos_ExcluiLogicamente()
    {
        var enderecoTest = new Endereco { Id = 7, Logradouro = "Rua F", Numero = 7 };
        _context.Enderecos.Add(enderecoTest);
        _context.SaveChanges();

        var result = _enderecoService.DeletaEndereco(7);

        Assert.True(result.IsSuccess);
        var enderecoExcluido = _context.Enderecos.IgnoreQueryFilters().FirstOrDefault(e => e.Id == 7);
        Assert.NotNull(enderecoExcluido);
        Assert.True(enderecoExcluido.DataExclusao.HasValue);
        Assert.Equal("user-admin-123", enderecoExcluido.UsuarioExclusaoId);
    }
}
