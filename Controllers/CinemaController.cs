using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;

namespace FilmesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CinemaController : ControllerBase
{
    private FilmeContext _context;
    private IMapper _mapper;

    public CinemaController(FilmeContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpPost]
    public IActionResult AdicionaCinema([FromBody] CreateCinemaDTO cinemaDTO) {
        Cinema cinema = _mapper.Map<Cinema>(cinemaDTO);
        _context.Cinemas.Add(cinema);
        _context.SaveChanges();
        return CreatedAtAction(nameof(ObterCinemaPorId), new { Id = cinema.Id }, cinemaDTO);
    }

    [HttpGet("{id}")]
    public IActionResult ObterCinemaPorId(int id)
    {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return NotFound();
        ReadCinemaDTO cinemaDTO = _mapper.Map<ReadCinemaDTO>(cinema);
        return Ok(cinemaDTO);
    }
    [HttpGet]
    public IEnumerable<ReadCinemaDTO> ObterCinemas([FromQuery] int skip = 0, [FromQuery] int take = 25) {
        IEnumerable<ReadCinemaDTO> cinemas = _mapper.Map<List<ReadCinemaDTO>>(_context.Cinemas.Skip(skip).Take(take));
        return cinemas;
    }

    [HttpPatch("{id}")]
    public IActionResult AtualizaCinemaParcial(int id, JsonPatchDocument<UpdateCinemaDTO> patch) {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return NotFound();
        UpdateCinemaDTO cinemaDTO = _mapper.Map<UpdateCinemaDTO>(cinema);
        patch.ApplyTo(cinemaDTO, ModelState);
        if (!TryValidateModel(cinema)) {
            return ValidationProblem(ModelState);
        }
        _mapper.Map(cinemaDTO, cinema);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPut("{id}")]
    public IActionResult AtualizaCinema(int id, UpdateCinemaDTO cinemaDTO) {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return NotFound();
        _mapper.Map(cinemaDTO, cinema);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult DeletaCinema(int id) {
        Cinema cinema = _context.Cinemas.FirstOrDefault(c => c.Id == id)!;
        if (cinema == null) return NotFound();
        _context.Remove(cinema);
        _context.SaveChanges();
        return NoContent();
    }
}
