namespace FilmesAPI.Services;

using AutoMapper;
using FilmesAPI.Data;
using FilmesAPI.Data.DTO;
using FilmesAPI.Models;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using System;

public class EnderecoService
{
    private IMapper _mapper;
    private AppDbContext _context;

    public EnderecoService(IMapper mapper, AppDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public const string ErroNaoEncontrado = "Endereço não encontrado!";
    public const string ErroCinemaVinculado = "Não é possível excluir pois existe cinema vinculado: ";

    public ReadEnderecoDTO AdicionaEndereco(CreateEnderecoDTO enderecoDTO)
    {
        var endereco = _mapper.Map<Endereco>(enderecoDTO);
        _context.Enderecos.Add(endereco);
        _context.SaveChanges();
        return _mapper.Map<ReadEnderecoDTO>(endereco);

    }

    public ReadEnderecoDTO? ObterEnderecoPorId(int id)
    {
        var endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id);
        if (endereco == null) return null;
        return _mapper.Map<ReadEnderecoDTO>(endereco);
    }

    public List<ReadEnderecoDTO> ObterEnderecos(int skip, int take)
    {
        var query = _context.Enderecos.AsQueryable();
        var listaDeEnderecos = query.OrderBy(c => c.Logradouro).Skip(skip).Take(take).ToList();

        return _mapper.Map<List<ReadEnderecoDTO>>(listaDeEnderecos);
    }

    public UpdateEnderecoDTO? RecuperaEnderecoParaAtualizar(int id)
    {
        Endereco endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (endereco == null) return null;
        return _mapper.Map<UpdateEnderecoDTO>(endereco);
    }

    public Result AtualizaEndereco(int id, UpdateEnderecoDTO enderecoDto)
    {
        var endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id);
        if (endereco == null) return Result.Fail("Endereco não encontrado!"); 
        _mapper.Map(enderecoDto, endereco);
        _context.SaveChanges();
        return Result.Ok();
    }

    public Result DeletaEndereco(int id)
    {
        Endereco endereco = _context.Enderecos.FirstOrDefault(c => c.Id == id)!;
        if (endereco == null) return Result.Fail(ErroNaoEncontrado);

        var cinemaVinculado = _context.Cinemas.FirstOrDefault(c => c.EnderecoId == id);

        if (cinemaVinculado != null)
        {
            return Result.Fail(ErroCinemaVinculado+cinemaVinculado.Nome);
        }
        endereco.DataExclusao = DateTime.Now;
        _context.SaveChanges();
        return Result.Ok();
    }
}