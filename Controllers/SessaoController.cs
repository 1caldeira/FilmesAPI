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
public class SessaoController : ControllerBase
{
    private SessaoService _sessaoService;
    

    public SessaoController(SessaoService sessaoService)
    {
        _sessaoService = sessaoService;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Adiciona uma nova sessão",
        Description = "Requer permissão de admin. Valida conflitos de horário e existência de Filme/Cinema."
    )]
    [SwaggerResponse(201, "Sessão criada com sucesso", typeof(ReadSessaoDTO))]
    [SwaggerResponse(400, "Erro de validação (ex: Conflito de horário, Horário no passado)")]
    [SwaggerResponse(404, "Filme ou Cinema não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AdicionaSessao([FromBody] CreateSessaoDTO createDTO) {
        var result = _sessaoService.AdicionaSessao(createDTO);
        if (result.IsFailed) {
            if (result.Errors.Any(r => r.Message.Equals(CinemaService.ErroNaoEncontrado)
            || r.Message.Equals(FilmeService.ErroNaoEncontrado))){ 
                return NotFound(result.Errors);
            }
            return BadRequest(result.Errors);
        }
        var readDTO = result.Value;
        
        return CreatedAtAction(nameof(ObterSessaoPorId), new { Id = readDTO.Id }, readDTO);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Recupera uma sessão por ID",
        Description = "Retorna detalhes da sessão, incluindo dados aninhados do Filme e Cinema."
    )]
    [SwaggerResponse(200, "Sessão encontrada", typeof(ReadSessaoDTO))]
    [SwaggerResponse(404, "Sessão não encontrada")]
    public IActionResult ObterSessaoPorId(int id)
    {
        var sessaoDTO = _sessaoService.ObterSessoesPorId(id);
        if (sessaoDTO == null) return NotFound();
        return Ok(sessaoDTO);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Recupera lista de sessões",
        Description = "Permite filtrar sessões (ex: por filmeId, cinemaId)."
    )]
    [SwaggerResponse(200, "Lista recuperada com sucesso", typeof(IEnumerable<ReadSessaoDTO>))]
    public IEnumerable<ReadSessaoDTO> ObterSessoes([FromQuery] FiltroSessaoDTO filtro) {
        return _sessaoService.ObterSessoes(filtro);
    }
    

    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza uma sessão parcialmente",
        Description = "Uso de JSON Patch. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Sessão atualizada com sucesso")]
    [SwaggerResponse(400, "Patch inválido ou erro de validação")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaSessaoParcial(int id, JsonPatchDocument<UpdateSessaoDTO> patch) {
        var sessaoParaAtualizar = _sessaoService.RecuperaSessoesParaAtualizar(id);
        if (sessaoParaAtualizar == null) return NotFound();
        patch.ApplyTo(sessaoParaAtualizar, ModelState);
        if (!TryValidateModel(sessaoParaAtualizar)) {
            return ValidationProblem(ModelState);
        }
        Result result = _sessaoService.AtualizaSessoes(id, sessaoParaAtualizar);

        if(result.IsFailed) return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza uma sessão (Completo)",
        Description = "Substitui todos os dados da sessão. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Sessão atualizada com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaSessao(int id, UpdateSessaoDTO sessaoDTO) {
        Result result = _sessaoService.AtualizaSessoes(id, sessaoDTO);
        if (result.IsFailed) {

            if (result.Errors.Any(r => r.Message.Equals(SessaoService.ErroNaoEncontrado)))
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
        Summary = "Exclui uma sessão",
        Description = "Remove a sessão. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Sessão excluída com sucesso")]
    [SwaggerResponse(400, "Erro ao excluir (ex: já vendida/realizada)")]
    [SwaggerResponse(404, "Sessão não encontrada")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult DeletaSessao(int id) {
        Result result = _sessaoService.DeletaSessoes(id);
        if (result.IsFailed) {
            if (result.Errors.Any(r => r.Message.Equals(SessaoService.ErroNaoEncontrado)))
            {
                return NotFound(result.Errors);
            }
            return BadRequest(result.Errors);
        }
        return NoContent();
    }
}
