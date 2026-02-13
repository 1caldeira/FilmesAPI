using FilmesAPI.Data.DTO;
using FilmesAPI.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace FilmesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CinemaController : ControllerBase
{
    private CinemaService _cinemaService;

    public CinemaController(CinemaService cinemaService)
    {
        _cinemaService = cinemaService;
    }


    [Authorize(Roles = "admin")]
    [HttpPost]
    [SwaggerOperation(
        Summary = "Adiciona um cinema",
        Description = "Requer permissão de administrador. Cria um novo registro de cinema vinculado a um endereço."
    )]
    [SwaggerResponse(201, "Cinema criado com sucesso", typeof(ReadCinemaDTO))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AdicionaCinema([FromBody] CreateCinemaDTO cinemaDTO)
    {

        ReadCinemaDTO readDTO = _cinemaService.AdicionaCinema(cinemaDTO);

        return CreatedAtAction(nameof(ObterCinemaPorId), new { id = readDTO.Id }, readDTO);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Recupera um cinema por ID",
        Description = "Retorna os detalhes de um cinema específico, incluindo seu endereço e sessões."
    )]
    [SwaggerResponse(200, "Cinema encontrado", typeof(ReadCinemaDTO))]
    [SwaggerResponse(404, "Cinema não encontrado")]
    public IActionResult ObterCinemaPorId(int id)
    {
        ReadCinemaDTO readDTO = _cinemaService.ObterCinemaPorId(id);
        if(readDTO == null) return NotFound();
        return Ok(readDTO);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Recupera lista de cinemas",
        Description = "Permite paginação e filtragem opcional por ID de endereço."
    )]
    [SwaggerResponse(200, "Lista de cinemas recuperada com sucesso", typeof(IEnumerable<ReadCinemaDTO>))]
    public ActionResult ObterCinemas([FromQuery] int skip = 0, [FromQuery] int take = 25, [FromQuery] int? enderecoId = null) {
        var cinemas = _cinemaService.ObterCinemas(skip, take, enderecoId);
        return Ok(cinemas);
    }
    

    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um cinema parcialmente",
        Description = "Utiliza JSON Patch para alterar campos específicos. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Cinema atualizado com sucesso")]
    [SwaggerResponse(400, "Patch inválido ou erro de validação")]
    [SwaggerResponse(404, "Cinema não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaCinemaParcial(int id, JsonPatchDocument<UpdateCinemaDTO> patch) {
        var cinemaParaAtualizar = _cinemaService.RecuperaCinemaParaAtualizar(id);
        if(cinemaParaAtualizar  == null) return NotFound();
        patch.ApplyTo(cinemaParaAtualizar, ModelState);
        if (!TryValidateModel(cinemaParaAtualizar))
        {
            return ValidationProblem(ModelState);
        }

        var resultado = _cinemaService.AtualizaCinema(id, cinemaParaAtualizar);

        if (resultado.IsFailed)
        {
            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um cinema (Completo)",
        Description = "Substitui todos os dados do cinema. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Cinema atualizado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Cinema não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaCinema(int id, UpdateCinemaDTO cinemaDTO) {
        Result result = _cinemaService.AtualizaCinema(id, cinemaDTO);
        if (result.IsFailed)
        {
            if (result.Errors.Any(r => r.Message.Equals(CinemaService.ErroNaoEncontrado)))
            {
                return NotFound();
            }
            return BadRequest(result.Errors);
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Exclui um cinema",
        Description = "Remove o cinema do banco de dados. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Cinema excluído com sucesso")]
    [SwaggerResponse(400, "Erro ao excluir (possível vínculo com sessões/filmes)")]
    [SwaggerResponse(404, "Cinema não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult DeletaCinema(int id) {
        Result result = _cinemaService.DeletaCinema(id);
        if (result.IsFailed) { 
            if (result.Errors.Any(erro => erro.Message == EnderecoService.ErroNaoEncontrado))
            {
            return NotFound(result.Errors);
            }

            return BadRequest(result.Errors);
        }
        return NoContent();
    }
}
