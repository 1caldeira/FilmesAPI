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

    private UsuarioService _usuarioService;

    public UsuarioController(UsuarioService cadastroService)
    {
        _usuarioService = cadastroService;
    }

    [HttpPost("cadastro")]
    public async Task<IActionResult> CadastraUsuario(CreateUsuarioDTO dto)
    {
        await _usuarioService.Cadastra(dto);
        return Ok("Usuário cadastrado com sucesso");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUsuarioDTO dto) {
        await _usuarioService.Login(dto);
        return Ok("Usuario autenticado com sucesso!");
    }
}