using FilmesAPI.Data.DTO;
using FilmesAPI.DTO;
using FilmesAPI.Services;
using FilmesAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace FilmesAPI.Controllers;

[ApiController]
[Route("[Controller]")]
public class UsuarioController : ControllerBase
{

    private UsuarioService _usuarioService;
    private RabbitMqService _rabbitMqService;
    private IConfiguration _configuration;

    public UsuarioController(UsuarioService cadastroService, RabbitMqService rabbitMqService, IConfiguration configuration)
    {
        _usuarioService = cadastroService;
        _rabbitMqService = rabbitMqService;
        _configuration = configuration;
    }

    [HttpPost("cadastro")]
    [SwaggerOperation(
        Summary = "Registra um novo usuário",
        Description = "Cria uma conta de usuário no sistema (Identity). Requer senha forte."
    )]
    [SwaggerResponse(200, "Usuário cadastrado com sucesso")]
    [SwaggerResponse(400, "Erro de validação (Senha fraca, usuário já existente, etc)")]
    [SwaggerResponse(500, "Erro interno no servidor")]
    public async Task<IActionResult> CadastraUsuario(CreateUsuarioDTO dto)
    {
        string baseUrl = _configuration["FrontEndUrl"];
        var (usuarioId,token) = await _usuarioService.Cadastra(dto);
        var tokenCodificado = System.Web.HttpUtility.UrlEncode(token);
        var linkConfirmacao = $"{baseUrl}/confirmar-email?userId={usuarioId}&token={tokenCodificado}";
        MensagemEmailDTO email = new MensagemEmailDTO();
        email.Assunto = "Confirmação de email MoovCine";
        email.Destinatario = dto.Email;
        email.Corpo = EmailTemplates.GetConfirmacaoTemplate(dto.NomeCompleto.Split(" ")[0], linkConfirmacao);

        await _rabbitMqService.PublicarMensagemDeEmailAsync(email);

        return Ok("Usuário cadastrado com sucesso! Verifique seu e-mail.");
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Realiza login e obtém Token",
        Description = "Autentica as credenciais e retorna um Token JWT (Bearer) para acesso aos recursos protegidos."
    )]
    [SwaggerResponse(200, "Login realizado com sucesso (Retorna o Token)", typeof(string))]
    [SwaggerResponse(401, "Usuário ou senha inválidos")]
    public async Task<IActionResult> Login(LoginUsuarioDTO dto) {
        var token = await _usuarioService.Login(dto);
        return Ok(token);
    }

    [HttpPost("confirmar-email")]
    public async Task<IActionResult> ConfirmarEmail([FromBody] ConfirmarEmailDTO dto) {
        var sucesso = await _usuarioService.ConfirmaEmail(dto.UserId, dto.Token);
        if (sucesso)
        {
            return Ok(new { mensagem = "E-mail confirmado com sucesso!" });
        }

        return BadRequest(new { mensagem = "Falha ao confirmar o e-mail. Link inválido ou expirado"});
    
    }

    [HttpPost("esqueci-minha-senha")]
    public async Task<IActionResult> EsqueciMinhaSenha(EsqueciMinhaSenhaDTO dto)
    {
        await _usuarioService.SolicitarRecuperacaoSenha(dto);
        return Ok(new { message = "Caso estiver cadastrado você receberá em seu e-mail um link com as instruções para redefinir a senha." });
    }

    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDTO dto)
    {
        var resultado = await _usuarioService.RedefinirSenhaAsync(dto);

        if (resultado.IsFailed)
        {
            return BadRequest(new { message = resultado.Errors.First().Message });
        }

        return Ok(new { message = "Senha redefinida com sucesso!" });
    }


}