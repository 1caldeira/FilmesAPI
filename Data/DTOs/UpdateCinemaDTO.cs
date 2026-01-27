using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Data.DTOs;

public class UpdateCinemaDTO
{
    [Required(ErrorMessage = "O campo nome é obrigatorio.")]
    public string Nome { get; set; }
}
