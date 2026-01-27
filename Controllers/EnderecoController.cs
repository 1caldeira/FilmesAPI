using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;

namespace FilmesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class EnderecoController : ControllerBase
{
    private FilmeContext _context;
    private IMapper _mapper;

    public EnderecoController(FilmeContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpPost]
    public IActionResult AdicionaEndereco([FromBody] CreateEnderecoDTO EnderecoDTO) {
        Endereco Endereco = _mapper.Map<Endereco>(EnderecoDTO);
        _context.Enderecos.Add(Endereco);
        _context.SaveChanges();
        return CreatedAtAction(nameof(ObterEnderecoPorId), new { Id = Endereco.Id }, EnderecoDTO);
    }

    [HttpGet("{id}")]
    public IActionResult ObterEnderecoPorId(int id)
    {
        Endereco Endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (Endereco == null) return NotFound();
        ReadEnderecoDTO EnderecoDTO = _mapper.Map<ReadEnderecoDTO>(Endereco);
        return Ok(EnderecoDTO);
    }
    [HttpGet]
    public IEnumerable<ReadEnderecoDTO> ObterEnderecos([FromQuery] int skip = 0, [FromQuery] int take = 25) {
        IEnumerable<ReadEnderecoDTO> Enderecos = _mapper.Map<List<ReadEnderecoDTO>>(_context.Enderecos.Skip(skip).Take(take));
        return Enderecos;
    }

    [HttpPatch("{id}")]
    public IActionResult AtualizaEnderecoParcial(int id, JsonPatchDocument<UpdateEnderecoDTO> patch) {
        Endereco Endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (Endereco == null) return NotFound();
        UpdateEnderecoDTO EnderecoDTO = _mapper.Map<UpdateEnderecoDTO>(Endereco);
        patch.ApplyTo(EnderecoDTO, ModelState);
        if (!TryValidateModel(Endereco)) {
            return ValidationProblem(ModelState);
        }
        _mapper.Map(EnderecoDTO, Endereco);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpPut("{id}")]
    public IActionResult AtualizaEndereco(int id, UpdateEnderecoDTO EnderecoDTO) {
        Endereco Endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (Endereco == null) return NotFound();
        _mapper.Map(EnderecoDTO, Endereco);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult DeletaEndereco(int id) {
        Endereco Endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (Endereco == null) return NotFound();
        _context.Remove(Endereco);
        _context.SaveChanges();
        return NoContent();
    }
}
