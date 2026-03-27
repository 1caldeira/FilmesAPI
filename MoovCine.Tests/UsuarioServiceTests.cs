using AutoMapper;
using FilmesAPI.Data.DTO;
using FilmesAPI.DTO;
using FilmesAPI.Models;
using FilmesAPI.Profiles;
using FilmesAPI.Services;
using FilmesAPI.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text;
using Xunit;

namespace MoovCine.Tests.Services;

public class UsuarioServiceTests
{
    private readonly Mock<UserManager<Usuario>> _userManagerMock;
    private readonly Mock<SignInManager<Usuario>> _signInManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IRabbitMqService> _rabbitMqServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly IMapper _mapper;
    private readonly UsuarioService _usuarioService;

    public UsuarioServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new UsuarioProfile());
        });
        _mapper = mapperConfig.CreateMapper();

        var store = new Mock<IUserStore<Usuario>>();
        _userManagerMock = new Mock<UserManager<Usuario>>(store.Object, null, null, null, null, null, null, null, null);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<Usuario>>();
        _signInManagerMock = new Mock<SignInManager<Usuario>>(_userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

        _configurationMock = new Mock<IConfiguration>();
        _tokenServiceMock = new Mock<ITokenService>();
        _rabbitMqServiceMock = new Mock<IRabbitMqService>();

        _usuarioService = new UsuarioService(
            _mapper,
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _roleManagerMock.Object,
            _rabbitMqServiceMock.Object
        );
    }

    [Fact]
    public async Task Login_Sucesso_RetornaToken()
    {
        var dto = new LoginUsuarioDTO { Email = "teste@teste.com", Password = "Password123!" };
        var usuario = new Usuario { UserName = "teste", NormalizedUserName = "teste@teste.com" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(usuario);

        _signInManagerMock.Setup(s => s.PasswordSignInAsync(usuario.UserName, dto.Password, false, false))
            .ReturnsAsync(SignInResult.Success);
            
        _userManagerMock.Setup(u => u.GetRolesAsync(usuario)).ReturnsAsync(new List<string> { "admin" });
        
        _tokenServiceMock.Setup(t => t.GenerateToken(usuario, "admin")).Returns("TOKENMOCKADO");

        var token = await _usuarioService.Login(dto);

        Assert.Equal("TOKENMOCKADO", token);
    }

    [Fact]
    public async Task Login_Falha_SenhaIncorreta_LancaApplicationException()
    {
        var dto = new LoginUsuarioDTO { Email = "teste@teste.com", Password = "WrongPassword!" };
        var usuario = new Usuario { UserName = "teste" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(usuario);

        _signInManagerMock.Setup(s => s.PasswordSignInAsync(usuario.UserName, dto.Password, false, false))
            .ReturnsAsync(SignInResult.Failed);

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => _usuarioService.Login(dto));
        Assert.Equal("Usuario ou senha incorretos.", exception.Message);
    }
    
    [Fact]
    public async Task Login_Falha_EmailIncorreto_LancaApplicationException()
    {
        var dto = new LoginUsuarioDTO { Email = "teste@teste.com", Password = "WrongPassword!" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => _usuarioService.Login(dto));
        Assert.Equal("Usuario ou senha incorretos.", exception.Message);
    }

    [Fact]
    public async Task Cadastra_Sucesso_RetornaIdEToken()
    {
        var dto = new CreateUsuarioDTO { NomeCompleto = "Teste", Email = "novo@teste.com", Password = "Password123!" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<Usuario>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<Usuario>()))
            .ReturnsAsync("token123");

        var result = await _usuarioService.Cadastra(dto);

        Assert.Equal("token123", result.token);
        Assert.False(string.IsNullOrEmpty(result.usuarioId));
    }

    [Fact]
    public async Task Cadastra_EmailExistente_LancaApplicationException()
    {
        var dto = new CreateUsuarioDTO { NomeCompleto = "Teste", Email = "existente@teste.com", Password = "Password123!" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(new Usuario());

        var exception = await Assert.ThrowsAsync<ApplicationException>(() => _usuarioService.Cadastra(dto));
        Assert.Equal("Já existe uma conta cadastrada com este e-mail", exception.Message);
    }

    [Fact]
    public async Task ConfirmaEmail_Sucesso_RetornaTrue()
    {
        var userId = "user1";
        var token = "token1";
        var usuario = new Usuario { Id = userId };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(usuario);
        _userManagerMock.Setup(u => u.ConfirmEmailAsync(usuario, token)).ReturnsAsync(IdentityResult.Success);

        var result = await _usuarioService.ConfirmaEmail(userId, token);

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmaEmail_UsuarioNaoEncontrado_RetornaFalse()
    {
        _userManagerMock.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync((Usuario?)null);

        var result = await _usuarioService.ConfirmaEmail("user1", "token1");

        Assert.False(result);
    }

    [Fact]
    public async Task SolicitarRecuperacaoSenha_Sucesso_ChamaRabbitMq()
    {
        var dto = new EsqueciMinhaSenhaDTO { Email = "teste@teste.com" };
        var usuario = new Usuario { Email = "teste@teste.com", PrimeiroNome = "Teste" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(usuario);
        _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(usuario)).ReturnsAsync("resetToken123");
        
        _rabbitMqServiceMock.Setup(r => r.PublicarMensagemDeEmailAsync(It.IsAny<MensagemEmailDTO>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _usuarioService.SolicitarRecuperacaoSenha(dto);

        _rabbitMqServiceMock.Verify(r => r.PublicarMensagemDeEmailAsync(It.IsAny<MensagemEmailDTO>()), Times.Once);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_Sucesso_RetornaResultOk()
    {
        var tokenOriginal = "resetToken123";
        byte[] tokenBytes = Encoding.UTF8.GetBytes(tokenOriginal);
        var tokenCodificado = WebEncoders.Base64UrlEncode(tokenBytes);

        var dto = new RedefinirSenhaDTO { Email = "teste@teste.com", NovaSenha = "NewPassword123!", Token = tokenCodificado };
        var usuario = new Usuario { Email = "teste@teste.com" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(usuario);
        
        _userManagerMock.Setup(u => u.ResetPasswordAsync(usuario, tokenOriginal, dto.NovaSenha))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _usuarioService.RedefinirSenhaAsync(dto);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_UsuarioNaoEncontrado_RetornaResultFail()
    {
        var dto = new RedefinirSenhaDTO { Email = "teste@teste.com", NovaSenha = "NewPassword123!", Token = "token" };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((Usuario?)null);

        var result = await _usuarioService.RedefinirSenhaAsync(dto);

        Assert.True(result.IsFailed);
        Assert.Equal("Usuário não encontrado.", result.Errors.First().Message);
    }
}
