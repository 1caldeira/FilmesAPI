using AutoMapper;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Data.DTOs.Usuario;
using FilmesAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace FilmesAPI.Services;

public class UsuarioService
{
    private IMapper _mapper;
    private UserManager<Usuario> _userManager;
    private SignInManager<Usuario> _signInManager;

    public UsuarioService(IMapper mapper, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
    {
        _mapper = mapper;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task Login(LoginUsuarioDTO dto)
    {
        var resultado = await _signInManager.PasswordSignInAsync(dto.Username,dto.Password,false,false);
        if (!resultado.Succeeded) {
            throw new ApplicationException("Usuario nao autenticado!");
        }

    }

    public async Task Cadastra(CreateUsuarioDTO dto) {
        Usuario usuario = _mapper.Map<Usuario>(dto);

        IdentityResult resultado = await _userManager.CreateAsync(usuario, dto.Password);

        if (!resultado.Succeeded) {
            throw new ApplicationException("Falha ao cadastrar usuário");
        }
    }
}
