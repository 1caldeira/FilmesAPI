using System.ComponentModel.DataAnnotations;

namespace FilmesAPI.Data.DTO;

public class CreateCinemaDTO
{
    [Required(ErrorMessage = "O campo nome é obrigatorio.")]
    public string Nome { get; set; }
    public int EnderecoId { get; set; }
    public CreateEnderecoDTO Endereco { get; set; }
}
