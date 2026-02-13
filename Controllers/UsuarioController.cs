using FilmesAPI.Data.DTO;
using FilmesAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FilmesAPI.Controllers;

[ApiController]
[Route("[Controller]")]
public class UsuarioController : ControllerBase
{

    private UsuarioService _usuarioService;

    public UsuarioController(UsuarioService cadastroService)
    {
        _usuarioService = cadastroService;
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
        await _usuarioService.Cadastra(dto);
        return Ok("Usuário cadastrado com sucesso");
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
}