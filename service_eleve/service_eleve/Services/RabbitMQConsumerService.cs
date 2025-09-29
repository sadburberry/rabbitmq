using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace service_eleve.Services;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumerService> _logger;

    public RabbitMQConsumerService(
        IConnection connection,
        ILogger<RabbitMQConsumerService> logger)
    {
        _connection = connection;
        _logger = logger;

        // Create channel and setup
        _channel = _connection.CreateModel();

        // Declare exchange (same as producer)
        _channel.ExchangeDeclare(exchange: "classe-events", type: ExchangeType.Fanout, durable: true);

        // Create temporary queue
        var queueName = _channel.QueueDeclare().QueueName;

        // Bind queue to exchange
        _channel.QueueBind(
            queue: queueName,
            exchange: "classe-events",
            routingKey: "");

        _logger.LogInformation("✅ RabbitMQ Consumer démarré - Queue: {QueueName}", queueName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("📨 Message reçu: {Message}", message);

                // Process the message
                var eventData = JsonSerializer.Deserialize<ClasseEvent>(message);

                if (eventData != null)
                {
                    _logger.LogInformation(
                        "🎯 Événement traité: {EventType} - Classe: {ClasseNom} (ID: {ClasseId})",
                        eventData.TypeEvenement, eventData.Nom, eventData.ClasseId);

                    // Ici vous pouvez:
                    // - Mettre à jour un cache local
                    // - Notifier d'autres services
                    // - Logger l'événement
                    // - etc.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du traitement du message RabbitMQ");
            }
        };

        // Start consuming
        _channel.BasicConsume(
            queue: _channel.CurrentQueue,
            autoAck: true,
            consumer: consumer
        );

        _logger.LogInformation("👂 En écoute des événements RabbitMQ...");

        // Keep service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

public class ClasseEvent
{
    public int ClasseId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Niveau { get; set; } = string.Empty;
    public int Capacite { get; set; }
    public string TypeEvenement { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
}