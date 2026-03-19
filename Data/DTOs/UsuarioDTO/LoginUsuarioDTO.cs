using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Data.DTO;

public class LoginUsuarioDTO
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}
