
using FilmesAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FilmesAPI.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateToken(Usuario usuario)
    {
        Claim[] claims = {
            new Claim("username", usuario.UserName!),
            new Claim("id", usuario.Id),
            new Claim(ClaimTypes.DateOfBirth, usuario.DataNascimento.ToString())
        };

        var keyFromConfig = _configuration["JwtSettings:SecretKey"];
        if (keyFromConfig == null) throw new ApplicationException("Secret Key vindo nula do appsettings!");
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyFromConfig!));
        var signingCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(expires: DateTime.Now.AddMinutes(10), claims: claims, signingCredentials:signingCredentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}