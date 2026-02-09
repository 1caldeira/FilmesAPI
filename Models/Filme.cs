using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Models;

public class Filme
{
    [Key]
    [Required]
    public int Id { get; set; }
    [Required(ErrorMessage = "O titulo do filme é obrigatorio!")]
    public string Titulo { get; set; }
    [Required(ErrorMessage = "O genero do filme é obrigatorio!")]
    public string Genero { get; set; }
    [Required]
    [Range(60,600, ErrorMessage = "A duracao deve ser entre 60 e 600 minutos!")]
    public int Duracao { get; set; }
    public string? PosterUrl { get; set; }
    public string? Sinopse { get; set; }
    public string? DataLancamento { get; set; }
    public virtual ICollection<Sessao> Sessoes { get; set; }
    public DateTime? DataExclusao { get; set; }


}


