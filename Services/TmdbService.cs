using FilmesAPI.Data;
using FilmesAPI.DTO;
using FilmesAPI.DTOs.TMDB;
using FilmesAPI.Models;
using FluentResults;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace FilmesAPI.Services;

public class TmdbService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<int, string> GenerosTraduzidos = new()
    {
        { 28, "Ação" },
        { 12, "Aventura" },
        { 16, "Animação" },
        { 35, "Comédia" },
        { 80, "Crime" },
        { 99, "Documentário" },
        { 18, "Drama" },
        { 10751, "Família" },
        { 14, "Fantasia" },
        { 36, "História" },
        { 27, "Terror" },
        { 10402, "Música" },
        { 9648, "Mistério" },
        { 10749, "Romance" },
        { 878, "Ficção Científica" },
        { 10770, "Cinema TV" },
        { 53, "Thriller" },
        { 10752, "Guerra" },
        { 37, "Faroeste" }
    };

    public TmdbService(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _configuration = configuration;
    }

    public async Task<Result> ImportarFilmesNowPlaying()
    {
        await ExecutarImportacao("now_playing",1);
        await ExecutarImportacao("now_playing", 2);
        await ExecutarImportacao("now_playing", 3);
        return Result.Ok();
    }

    public async Task<Result> ImportarFilmesUpcoming()
    {

        await ExecutarImportacao("upcoming", 1);
        await ExecutarImportacao("upcoming", 2);
        await ExecutarImportacao("upcoming", 3);

        return Result.Ok();
    }

    private async Task<Result> ExecutarImportacao(string endpoint, int pagina)
    {
        var apiKey = _configuration["TMDB:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return Result.Fail("API Key não configurada.");
        }

        try
        {
            var urlLista = $"https://api.themoviedb.org/3/movie/{endpoint}?api_key={apiKey}&language=pt-BR&page={pagina}";
            var responseLista = await _httpClient.GetAsync(urlLista);

            if (!responseLista.IsSuccessStatusCode)
            {
                return Result.Fail($"Falha ao comunicar com TMDB: {responseLista.StatusCode}");
            }

            var dadosLista = await responseLista.Content.ReadFromJsonAsync<TmdbResponse>();

            if (dadosLista?.Results == null || !dadosLista.Results.Any())
            {
                return Result.Ok().WithSuccess("Nenhum filme encontrado na lista.");
            }

            int count = 0;

            foreach (var item in dadosLista.Results)
            {
                if (await _context.Filmes.AnyAsync(f => f.Titulo == item.Title)) continue;

                var urlDetalhe = $"https://api.themoviedb.org/3/movie/{item.Id}?api_key={apiKey}&language=pt-BR";
                var detalhe = await _httpClient.GetFromJsonAsync<TmdbMovieDetail>(urlDetalhe);

                if (detalhe == null) continue;

                string generoFinal = "Desconhecido";
                if (detalhe.Genres.Any())
                {
                    int idGenero = detalhe.Genres.First().Id;
                    if (!GenerosTraduzidos.TryGetValue(idGenero, out generoFinal))
                    {
                        generoFinal = detalhe.Genres.First().Name;
                    }
                }


                DateTime dataLancamentoConvertida;
                bool temDataValida = DateTime.TryParse(item.ReleaseDate, out dataLancamentoConvertida);

                if (!temDataValida) continue; 

                var novoFilme = new Filme
                {
                    Titulo = item.Title,
                    Sinopse = !string.IsNullOrEmpty(detalhe.Overview) ? detalhe.Overview : "Sinopse indisponível",
                    Duracao = detalhe.Runtime > 0 ? detalhe.Runtime : 120,
                    Genero = generoFinal,
                    PosterUrl = !string.IsNullOrEmpty(item.PosterPath)
                        ? $"https://image.tmdb.org/t/p/w500{item.PosterPath}"
                        : null,
                    DataLancamento = dataLancamentoConvertida
                };

                _context.Filmes.Add(novoFilme);
                count++;

                await Task.Delay(150);
            }

            if (count > 0)
            {
                await _context.SaveChangesAsync();
                return Result.Ok().WithSuccess($"{count} filmes importados com sucesso.");
            }

            return Result.Ok().WithSuccess("Nenhum filme novo para importar.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Erro crítico na importação: {ex.Message}");
        }
    }
}