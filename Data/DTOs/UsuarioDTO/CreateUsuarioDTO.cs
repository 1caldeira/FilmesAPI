using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Data.DTO;

public class CreateUsuarioDTO
{
    public string Username { get; set; }
    [Required]
    public string NomeCompleto { get; set; }
    public string PrimeiroNome { get; set; }
    [Required]
    public DateTime DataNascimento { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    [Required]
    [Compare("Password", ErrorMessage = "A senha e a confirmação da senha são diferentes.")]
    public string RePassword { get; set; }
}
