namespace FilmesAPI.Data.DTO;

public class ReadFilmeDTO
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public string Genero { get; set; }
    public int Duracao { get; set; }
    public DateTime HoraDaConsulta { get; set; } = DateTime.Now;
    public ICollection<ReadSessaoSimpleDTO> Sessoes  { get; set; }
    public string Sinopse { get; set; }
    public string PosterUrl { get; set; }
    public string DataLancamento { get; set; }
}
