namespace FilmesAPI.Data.DTO;

public class ReadCinemaSimpleDTO
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public ReadEnderecoDTO Endereco { get; set; }
}