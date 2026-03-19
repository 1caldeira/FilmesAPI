using Microsoft.AspNetCore.Identity;


namespace FilmesAPI.Models;

public class Usuario : IdentityUser
{
    public string NomeCompleto { get; set; }
    public string PrimeiroNome { get; set; }
    public DateTime DataNascimento { get; set; }
    public DateTime? DataExclusao { get; set; }
    public Usuario() : base()
    {
        
    }
}
