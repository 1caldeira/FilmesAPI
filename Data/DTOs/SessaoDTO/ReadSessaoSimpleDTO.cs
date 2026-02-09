namespace FilmesAPI.Data.DTO;

public class ReadSessaoSimpleDTO
{
    
    public int Id { get; set; }
    public string Horario { get; set; }
    public int Sala { get; set; }
    public ReadCinemaSimpleDTO Cinema { get; set; }
}
