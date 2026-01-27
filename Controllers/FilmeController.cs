using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTOs;
using FilmesAPI.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace FilmesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FilmeController : ControllerBase
{
    private FilmeContext _context;
    private IMapper _mapper;

    public FilmeController(FilmeContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Adiciona um filme ao banco de dados
    /// </summary>
    /// <param name="filmeDTO">Objeto com os campos necessários para criação de um filme</param>
    /// <returns>IActionResult</returns>
    /// <response code="201">Caso inserção seja feita com sucesso</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public IActionResult AdicionaFilme([FromBody] CreateFilmeDTO filmeDTO) {
        Filme filme = _mapper.Map<Filme>(filmeDTO);
        _context.Filmes.Add(filme);
        _context.SaveChanges();
        return CreatedAtAction(nameof(ObterFilmesPorId), new {id = filme.Id}, filme);
    }

    /// <summary>
    /// Recupera uma lista de filmes do banco de dados
    /// </summary>
    /// <remarks>
    /// Exemplo de requisição:
    /// GET /filme?skip=0&take=10
    /// </remarks>
    /// <param name="skip">Número de registros a serem ignorados (paginação)</param>
    /// <param name="take">Número de registros a serem recuperados (paginação)</param>
    /// <returns>Uma lista de objetos de filme</returns>
    /// <response code="200">Retorna a lista de filmes com sucesso</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<ReadFilmeDTO> ObterFilmes([FromQuery] int skip = 0, [FromQuery] int take = 25) {
        return _mapper.Map<List<ReadFilmeDTO>>(_context.Filmes.Skip(skip).Take(take));
    }

    /// <summary>
    /// Recupera um filme do banco de dados
    /// </summary>
    /// <remarks>
    /// Exemplo de requisição:
    /// GET /filme/1
    /// </remarks>
    /// <param name="id">Id do filme a ser recuperado</param>
    /// <returns>Um objeto filme</returns>
    /// <response code="200">Retorna o objeto filme com sucesso</response>
    /// <response code="404">Caso o ID do filme não seja encontrado</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ObterFilmesPorId(int id) {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return NotFound();
        ReadFilmeDTO dto = _mapper.Map<ReadFilmeDTO>(filme);
        return Ok(dto);
    }
    /// <summary>
    /// Atualiza um filme no banco de dados
    /// </summary>
    /// <param name="id">Id do filme a ser atualizado</param>
    /// <param name="filmeDTO">Objeto com os campos necessários para atualização de um filme</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso a atualização seja feita com sucesso</response>
    /// <response code="404">Caso o id do filme não seja encontrado</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizaFilme(int id, [FromBody] UpdateFilmeDTO filmeDTO)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return NotFound();

        _mapper.Map(filmeDTO, filme);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>
    /// Atualiza parcialmente um filme no banco de dados usando JSON Patch
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo da requisição (JSON Patch):
    /// [
    ///   { "op": "replace", "path": "/titulo", "value": "Novo Título" }
    /// ]
    /// </remarks>
    /// <param name="id">Id do filme a ser atualizado</param>
    /// <param name="patch">O documento JSON Patch com as alterações desejadas</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso a atualização parcial seja feita com sucesso</response>
    /// <response code="400">Caso o corpo da requisição esteja inválido ou mal formatado</response>
    /// <response code="404">Caso o id do filme não seja encontrado</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizaFilmeParcial(int id, JsonPatchDocument<UpdateFilmeDTO> patch)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return NotFound();

        UpdateFilmeDTO filmeParaAtualizar = _mapper.Map<UpdateFilmeDTO>(filme);
        patch.ApplyTo(filmeParaAtualizar, ModelState);

        if (!TryValidateModel(filmeParaAtualizar))
        {
            return ValidationProblem(ModelState);
        }
        _mapper.Map(filmeParaAtualizar, filme);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>
    /// Deleta um filme do banco de dados
    /// </summary>
    /// <param name="id">Id do filme a ser removido</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso o filme seja deletado com sucesso</response>
    /// <response code="404">Caso o id do filme não seja encontrado</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeletaFilme(int id)
    {
        var filme = _context.Filmes.FirstOrDefault(f => f.Id == id);
        if (filme == null) return NotFound();
        _context.Remove(filme);
        _context.SaveChanges();
        return NoContent();
    }
}
