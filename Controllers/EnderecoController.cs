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
public class EnderecoController : ControllerBase
{
    private EnderecoService _enderecoService;
   
    public EnderecoController(EnderecoService enderecoService)
    {
        _enderecoService = enderecoService;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Adiciona um endereço",
        Description = "Requer permissão de administrador. Cadastra um novo logradouro e número."
    )]
    [SwaggerResponse(201, "Endereço criado com sucesso", typeof(ReadEnderecoDTO))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AdicionaEndereco([FromBody] CreateEnderecoDTO EnderecoDTO) {

        var readDTO = _enderecoService.AdicionaEndereco(EnderecoDTO);
        return CreatedAtAction(nameof(ObterEnderecoPorId), new { Id = readDTO.Id }, readDTO);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Recupera um endereço por ID",
        Description = "Retorna os detalhes de um endereço específico (Logradouro e Número)."
    )]
    [SwaggerResponse(200, "Endereço encontrado", typeof(ReadEnderecoDTO))]
    [SwaggerResponse(404, "Endereço não encontrado")]
    public IActionResult ObterEnderecoPorId(int id)
    {
        var readDTO = _enderecoService.ObterEnderecoPorId(id);
        if(readDTO == null) return NotFound();
        return Ok(readDTO);
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Recupera lista de endereços",
        Description = "Permite paginação (skip/take) para listar os endereços cadastrados."
    )]
    [SwaggerResponse(200, "Lista recuperada com sucesso", typeof(IEnumerable<ReadEnderecoDTO>))]
    public ActionResult ObterEnderecos([FromQuery] int skip = 0, [FromQuery] int take = 25) {
        var enderecos = _enderecoService.ObterEnderecos(skip, take);
        return Ok(enderecos);
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um endereço parcialmente",
        Description = "Utiliza JSON Patch. Ex: mudar apenas o número do endereço."
    )]
    [SwaggerResponse(204, "Endereço atualizado com sucesso")]
    [SwaggerResponse(400, "Patch inválido ou erro de validação")]
    [SwaggerResponse(404, "Endereço não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaEnderecoParcial(int id, JsonPatchDocument<UpdateEnderecoDTO> patch) {
        var enderecoParaAtualizar = _enderecoService.RecuperaEnderecoParaAtualizar(id);
        if (enderecoParaAtualizar == null) return NotFound();
        patch.ApplyTo(enderecoParaAtualizar, ModelState);
        if (!TryValidateModel(enderecoParaAtualizar))
        {
            return ValidationProblem(ModelState);
        }

        var resultado = _enderecoService.AtualizaEndereco(id, enderecoParaAtualizar);

        if (resultado.IsFailed)
        {
            return BadRequest(resultado.Errors);
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Atualiza um endereço (Completo)",
        Description = "Substitui todos os campos do endereço. Requer permissão de admin."
    )]
    [SwaggerResponse(204, "Endereço atualizado com sucesso")]
    [SwaggerResponse(404, "Endereço não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult AtualizaEndereco(int id, UpdateEnderecoDTO EnderecoDTO) {
        Result result = _enderecoService.AtualizaEndereco(id, EnderecoDTO);
        if (result.IsFailed) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    [SwaggerOperation(
        Summary = "Exclui um endereço",
        Description = "Remove o registro do banco. Pode falhar se o endereço estiver em uso por um Cinema."
    )]
    [SwaggerResponse(204, "Endereço excluído com sucesso")]
    [SwaggerResponse(400, "Erro de integridade (endereço em uso)")]
    [SwaggerResponse(404, "Endereço não encontrado")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(401, "Não autorizado")]
    public IActionResult DeletaEndereco(int id) {
        Result result = _enderecoService.DeletaEndereco(id);
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
