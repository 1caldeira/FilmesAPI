using AutoMapper;
using FilmesAPI.Data.DTO;
using FilmesAPI.DTO;
using FilmesAPI.Models;
using FilmesAPI.Utils;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace FilmesAPI.Services;using FilmesAPI.Services.Interfaces;

public class UsuarioService : IUsuarioService
{
    private IMapper _mapper;
    private UserManager<Usuario> _userManager;
    private SignInManager<Usuario> _signInManager;
    private ITokenService _tokenService;
    private RoleManager<IdentityRole> _roleManager;
    private IRabbitMqService _rabbitMqService;

    public UsuarioService(IMapper mapper, UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager, ITokenService tokenService, RoleManager<IdentityRole> roleManager,
        IRabbitMqService rabbitMqService)
    {
        _mapper = mapper;
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _roleManager = roleManager;
        _rabbitMqService = rabbitMqService;
    }

    public virtual async Task<string> Login(LoginUsuarioDTO dto)
    {
        var resultado = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);
        if (!resultado.Succeeded)
        {
            throw new ApplicationException("Usuario ou senha incorretos.");
        }
        var usuario = _signInManager.UserManager.Users.FirstOrDefault(
            user => user.NormalizedUserName == dto.Email);

        var roles = await _signInManager.UserManager.GetRolesAsync(usuario);
        var roleDoUsuario = roles.FirstOrDefault() ?? "usuario";

        var token = _tokenService.GenerateToken(usuario, roleDoUsuario);
        return token;

    }

    public virtual async Task<(string usuarioId, string token)> Cadastra(CreateUsuarioDTO dto)
    {
        Usuario? usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);
        if (usuarioExistente != null) throw new ApplicationException("Já existe uma conta cadastrada com este e-mail");
        Usuario usuario = _mapper.Map<Usuario>(dto);

        IdentityResult resultado = await _userManager.CreateAsync(usuario, dto.Password);

        if (!resultado.Succeeded)
        {
            var mensagensDeErro = resultado.Errors.Select(e => e.Description);
            var erroFormatado = string.Join(" | ", mensagensDeErro);

            throw new ApplicationException(erroFormatado);
        }



        var token = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);


        return (usuario.Id.ToString(), token);
    }

    public virtual async Task<bool> ConfirmaEmail(string userId, string token)
    {
        var usuario = await _userManager.FindByIdAsync(userId);
        if (usuario == null) return false;

        var resultado = await _userManager.ConfirmEmailAsync(usuario, token);

        return resultado.Succeeded;
    }

    public virtual async Task SolicitarRecuperacaoSenha(EsqueciMinhaSenhaDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        byte[] tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
        string tokenCodificadoParaUrl = WebEncoders.Base64UrlEncode(tokenGeneratedBytes);
        string resetLink = $"https://moovcine.site/redefinir-senha?email={user.Email}&token={tokenCodificadoParaUrl}";

        var mensagemFila = new MensagemEmailDTO
        {
            Destinatario = user.Email,
            Assunto = "Moov Cine - Recuperação de Senha",
            Corpo = EmailTemplates.GetRedefinicaoTemplate(user.PrimeiroNome, resetLink)
        };

        await _rabbitMqService.PublicarMensagemDeEmailAsync(mensagemFila);
    }

    public virtual async Task<Result> RedefinirSenhaAsync(RedefinirSenhaDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return Result.Fail("Usuário não encontrado.");
        }
        byte[] tokenDecodedBytes = WebEncoders.Base64UrlDecode(dto.Token);
        string tokenLimpo = Encoding.UTF8.GetString(tokenDecodedBytes);
        var resultado = await _userManager.ResetPasswordAsync(user, tokenLimpo, dto.NovaSenha);

        if (resultado.Succeeded)
        {
            return Result.Ok();
        }

        var erros = string.Join(", ", resultado.Errors.Select(e => e.Description));
        return Result.Fail($"Falha ao redefinir senha: {erros}");
    }
}

