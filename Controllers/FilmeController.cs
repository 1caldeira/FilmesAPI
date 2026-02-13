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
public class FilmeController : ControllerBase
{
    private readonly FilmeService _filmeService;
    private readonly TmdbService _tmdbService;

    public FilmeController(FilmeService filmeService, TmdbService tmdbService)
    {

         _filmeService = filmeService;
        _tmdbService = tmdbService;
    }


    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Adiciona um filme ao banco de dados",
        Description = "Requer permissão de administrador. Cria um novo registro de filme com os dados fornecidos.")]
    [SwaggerResponse(201, "Filme criado com sucesso", typeof(ReadFilmeDTO))]
    [SwaggerResponse(400, "Falha na validação dos dados")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AdicionaFilme([FromBody] CreateFilmeDTO filmeDTO) {
        ReadFilmeDTO filme = _filmeService.AdicionaFilme(filmeDTO);
        return CreatedAtAction(nameof(ObterFilmesPorId), new {id = filme.Id}, filme);
    }

    
    [HttpGet]
    [SwaggerOperation(
        Summary = "Recupera uma lista de filmes",
        Description = "Permite paginação (skip/take) e filtragem por cinema, nome ou disponibilidade.")]
    [SwaggerResponse(200, "Lista de filmes recuperada com sucesso", typeof(List<ReadFilmeDTO>))]
    public ActionResult ObterFilmes([FromQuery] FiltroFilmeDTO filtro) {
        List<ReadFilmeDTO> filmes = _filmeService.ObterFilmes(filtro);
        return Ok(filmes);
    }


    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Recupera um filme por ID",
        Description = "Retorna os detalhes de um filme específico. " +
        "Admins podem ver sessões passadas/deletadas via query param.")]
    [SwaggerResponse(200, "Filme encontrado", typeof(ReadFilmeDTO))]
    [SwaggerResponse(404, "Filme não encontrado")]
    public IActionResult ObterFilmesPorId(int id, [FromQuery]bool verSessoesPassadas = false) {
        bool isAdmin = User.IsInRole("admin");
        ReadFilmeDTO dto = _filmeService.ObterFilmesPorId(id, isAdmin, verSessoesPassadas);
        if (dto == null) return NotFound();
        return Ok(dto);
    }


    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um filme (Completo)",
        Description = "Substitui todos os dados do filme pelo objeto enviado."
    )]
    [SwaggerResponse(204, "Filme atualizado com sucesso")]
    [SwaggerResponse(400, "Erro de validação ou regras de negócio")]
    [SwaggerResponse(404, "Filme não encontrado")]
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


    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um filme parcialmente (JSON Patch)",
        Description = "Exemplo de body: [ { \"op\": \"replace\", \"path\": \"/titulo\", \"value\": \"Novo Título\" } ]"
    )]
    [SwaggerResponse(204, "Filme atualizado com sucesso")]
    [SwaggerResponse(400, "Patch inválido ou erro de validação")]
    [SwaggerResponse(404, "Filme não encontrado")]
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


    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Exclusão lógica (Soft Delete)",
        Description = "Marca o filme como excluído. Falhará se houver sessões futuras ativas vinculadas."
    )]
    [SwaggerResponse(204, "Filme excluído com sucesso")]
    [SwaggerResponse(400, "Não é possível excluir (ex: sessões futuras existentes)")]
    [SwaggerResponse(404, "Filme não encontrado")]
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

    [HttpPost("importar")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Importação automática TMDB",
        Description = "Busca filmes 'Now Playing' e 'Upcoming' na API do TMDB e salva no banco."
    )]
    [SwaggerResponse(200, "Importação realizada com sucesso")]
    [SwaggerResponse(500, "Erro ao conectar com TMDB")]
    public async Task<IActionResult> ImportarFilmesDoTmdb()
    {

        Result resultadoNow = await _tmdbService.ImportarFilmesNowPlaying();

        if (resultadoNow.IsFailed)
        {
            return StatusCode(500, resultadoNow.Errors[0].Message);
        }

        Result resultadoUp = await _tmdbService.ImportarFilmesUpcoming();

        if (resultadoUp.IsFailed)
        {

            return StatusCode(500, resultadoUp.Errors[0].Message);
        }

        var mensagens = $"{resultadoNow.Successes[0].Message} | {resultadoUp.Successes[0].Message}";

        return Ok(new { message = mensagens });
    }
}
