using FilmesAPI.Data.DTO;
using FilmesAPI.Services;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;


namespace FilmesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FilmeController : ControllerBase
{
    private readonly FilmeService _filmeService;
    private readonly TmdbService _tmdbService;

    public FilmeController(FilmeService filmeService, TmdbService tmdbService)
    {

         _filmeService = filmeService;
        _tmdbService = tmdbService;
    }

    /// <summary>
    /// Adiciona um filme ao banco de dados
    /// </summary>
    /// <param name="filmeDTO">Objeto com os campos necessários para criação de um filme</param>
    /// <returns>IActionResult</returns>
    /// <response code="201">Caso inserção seja feita com sucesso</response>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public IActionResult AdicionaFilme([FromBody] CreateFilmeDTO filmeDTO) {
        ReadFilmeDTO filme = _filmeService.AdicionaFilme(filmeDTO);
        return CreatedAtAction(nameof(ObterFilmesPorId), new {id = filme.Id}, filme);
    }

    /// <summary>
    /// Recupera uma lista de filmes do banco de dados com filtros opcionais
    /// </summary>
    /// <remarks>
    /// Permite paginação e filtragem por cinema, nome ou disponibilidade.
    /// Exemplo de requisição:
    /// GET /filme?skip=0&take=10&nomeFilme=Batman
    /// </remarks>
    /// <param name="filtro">Objeto contendo os parâmetros de filtro e paginação (Skip, Take, CinemaId, NomeFilme, ApenasDisponiveis)</param>
    /// <returns>Uma lista de objetos de filme filtrada</returns>
    /// <response code="200">Retorna a lista de filmes com sucesso</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ObterFilmes([FromQuery] FiltroFilmeDTO filtro) {
        List<ReadFilmeDTO> filmes = _filmeService.ObterFilmes(filtro);
        return Ok(filmes);
    }

    /// <summary>
    /// Recupera um filme específico do banco de dados por ID
    /// </summary>
    /// <param name="id">O ID numérico do filme a ser recuperado</param>
    /// <returns>Um objeto com os detalhes do filme</returns>
    /// <response code="200">Retorna o objeto filme com sucesso</response>
    /// <response code="404">Caso o ID do filme não seja encontrado</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ObterFilmesPorId(int id, [FromQuery]bool verSessoesPassadas = false) {
        bool isAdmin = User.IsInRole("admin");
        ReadFilmeDTO dto = _filmeService.ObterFilmesPorId(id, isAdmin, verSessoesPassadas);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// Atualiza todos os dados de um filme existente
    /// </summary>
    /// <param name="id">O ID do filme a ser atualizado</param>
    /// <param name="filmeDTO">Objeto com os novos dados do filme</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso a atualização seja feita com sucesso</response>
    /// <response code="404">Caso o ID do filme não seja encontrado</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizaFilme(int id, [FromBody] UpdateFilmeDTO filmeDTO)
    {
        Result result = _filmeService.AtualizaFilme(id, filmeDTO);
        if (result.IsFailed) {
            if (result.Errors.Any(r => r.Message.Equals(FilmeService.ErroNaoEncontrado))) { 
                return NotFound();
            }
            return BadRequest(result.Errors);
        }
        return NoContent();
    }

    /// <summary>
    /// Atualiza parcialmente um filme usando JSON Patch
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo da requisição (JSON Patch):
    /// [
    ///   { "op": "replace", "path": "/titulo", "value": "Novo Título" }
    /// ]
    /// </remarks>
    /// <param name="id">O ID do filme a ser atualizado</param>
    /// <param name="patch">O documento JSON Patch com as alterações desejadas</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso a atualização parcial seja feita com sucesso</response>
    /// <response code="400">Caso o corpo da requisição seja inválido ou viole regras de negócio</response>
    /// <response code="404">Caso o ID do filme não seja encontrado</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizaFilmeParcial(int id, JsonPatchDocument<UpdateFilmeDTO> patch)
    {
        var filmeParaAtualizar = _filmeService.RecuperaFilmeParaAtualizar(id);
        if (filmeParaAtualizar == null) return NotFound();
        patch.ApplyTo(filmeParaAtualizar, ModelState);
        if (!TryValidateModel(filmeParaAtualizar))
        {
            return ValidationProblem(ModelState);
        }

        var resultado = _filmeService.AtualizaFilme(id, filmeParaAtualizar);

        if (resultado.IsFailed)
        {
            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Realiza a exclusão lógica (Soft Delete) de um filme
    /// </summary>
    /// <remarks>
    /// O registro não é removido fisicamente, apenas marcado como excluído.
    /// A exclusão será bloqueada se houver sessões futuras vinculadas ao filme.
    /// </remarks>
    /// <param name="id">O ID do filme a ser excluído</param>
    /// <returns>Sem conteúdo</returns>
    /// <response code="204">Caso o filme seja marcado como excluído com sucesso</response>
    /// <response code="400">Caso existam regras de negócio impedindo a exclusão (ex: sessões futuras)</response>
    /// <response code="404">Caso o ID do filme não seja encontrado</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeletaFilme(int id, [FromQuery] bool force = false)
    {
        Result result = _filmeService.DeletaFilme(id, force);
        if (result.IsFailed) {
            if (result.Errors.Any(r => r.Message.Equals(FilmeService.ErroNaoEncontrado))) {  
                return NotFound(); 
            }
            return BadRequest(result.Errors); 
        }
        return NoContent();
    }

    /// <summary>
    /// Importa filmes do TMDB (Now Playing e Upcoming) automaticamente.
    /// Requer permissão de Admin.
    /// </summary>
    [HttpPost("importar")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ImportarFilmesDoTmdb()
    {
        // Chama o service
        Result resultadoNow = await _tmdbService.ImportarFilmesNowPlaying();

        // Se falhar, já retorna erro (Bad Request ou 500 dependendo de como você trata)
        if (resultadoNow.IsFailed)
        {
            return StatusCode(500, resultadoNow.Errors[0].Message);
        }

        // Chama o segundo (opcional, pode fazer separado)
        Result resultadoUp = await _tmdbService.ImportarFilmesUpcoming();

        if (resultadoUp.IsFailed)
        {
            // Se o primeiro passou e o segundo falhou, avisa
            return StatusCode(500, resultadoUp.Errors[0].Message);
        }

        // Combina as mensagens de sucesso para retornar pro Front
        var mensagens = $"{resultadoNow.Successes[0].Message} | {resultadoUp.Successes[0].Message}";

        return Ok(new { message = mensagens });
    }
}
