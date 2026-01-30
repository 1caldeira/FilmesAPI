using AutoMapper;
using FilmesAPI.Data.DTOs.Usuario;
using FilmesAPI.Models;
using FilmesAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FilmesAPI.Controllers;

[ApiController]
[Route("[Controller]")]
public class UsuarioController : ControllerBase
{

    private CadastroService _cadastroService;

    public UsuarioController(CadastroService cadastroService)
    {
        _cadastroService = cadastroService;
    }

    [HttpPost]
    public async Task<IActionResult> CadastraUsuario(CreateUsuarioDTO dto)
    {
        await _cadastroService.Cadastra(dto);
        return Ok("Usuário cadastrado com sucesso");
    }
}