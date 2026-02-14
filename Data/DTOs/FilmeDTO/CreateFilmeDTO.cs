using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Data.DTO;

public class CreateFilmeDTO
{
    [Required(ErrorMessage = "O titulo do filme é obrigatorio!")]
    public string Titulo { get; set; }
    [Required(ErrorMessage = "O genero do filme é obrigatorio!")]
    [StringLength(50, ErrorMessage = "O tamanho do gênero não pode exceder 50 caracteres!")]
    public string Genero { get; set; }
    [Required]
    [Range(60, 600, ErrorMessage = "A duracao deve ser entre 60 e 600 minutos!")]
    public int Duracao { get; set; }
    [Required]
    public DateTime DataLancamento { get; set; }
}
