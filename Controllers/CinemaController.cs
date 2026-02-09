using FilmesAPI.Data.DTO;
using FilmesAPI.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;


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
    public IActionResult AdicionaCinema([FromBody] CreateCinemaDTO cinemaDTO)
    {
        // O service vai cuidar de tudo
        ReadCinemaDTO readDTO = _cinemaService.AdicionaCinema(cinemaDTO);

        return CreatedAtAction(nameof(ObterCinemaPorId), new { id = readDTO.Id }, readDTO);
    }

    [HttpGet("{id}")]
    public IActionResult ObterCinemaPorId(int id)
    {
        ReadCinemaDTO readDTO = _cinemaService.ObterCinemaPorId(id);
        if(readDTO == null) return NotFound();
        return Ok(readDTO);
    }

    [HttpGet]
    public ActionResult ObterCinemas([FromQuery] int skip = 0, [FromQuery] int take = 25, [FromQuery] int? enderecoId = null) {
        var cinemas = _cinemaService.ObterCinemas(skip, take, enderecoId);
        return Ok(cinemas);
    }
    

    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
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
    public IActionResult AtualizaCinema(int id, UpdateCinemaDTO cinemaDTO) {
        Result result = _cinemaService.AtualizaCinema(id, cinemaDTO);
        if (result.IsFailed) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
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
