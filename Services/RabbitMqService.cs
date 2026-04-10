using FilmesAPI.DTO;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

using FilmesAPI.Services.Interfaces;

public class RabbitMqService : IRabbitMqService
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    public RabbitMqService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private async Task InicializarConexaoAsync() {
        if (_connection is not null && _connection.IsOpen) return;

        var rabbitHost = _configuration["RabbitMQ:HostName"] ?? "rabbitmq";
        var factory = new ConnectionFactory() { 
            HostName = rabbitHost, 
            UserName = "moovadmin", 
            Password = "moovsenha123" };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(queue: "emails_queue",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
    }
    public virtual async Task PublicarMensagemDeEmailAsync(MensagemEmailDTO mensagem)
    {

        await InicializarConexaoAsync();

        var json = JsonSerializer.Serialize(mensagem);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel!.BasicPublishAsync(exchange: string.Empty,
                             routingKey: "emails_queue",
                             body: body);
    }
}