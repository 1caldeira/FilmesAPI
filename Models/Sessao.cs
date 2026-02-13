using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Models;

public class Sessao
{
    [Key]
    [Required]
    public int Id { get; set; }
    [Required]
    public int FilmeId { get; set; }
    public virtual Filme Filme { get; set; }
    [Required]
    public int CinemaId { get; set; }
    public virtual Cinema Cinema { get; set; }
    [Required]
    public DateTime Horario { get; set; }
    [Required]
    public int Sala { get; set; }

    public static int ToleranciaAtrasoMinutos = 20;
    public DateTime? DataExclusao { get; set; }
    public string? UsuarioExclusaoId { get; set; }
}
