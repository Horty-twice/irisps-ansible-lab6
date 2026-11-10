using System.Text.Json;
using Confluent.Kafka;
using MongoDB.Driver;
using UserConfirmationService.Models;

namespace UserConfirmationService.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "irisps-user-confirmation-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"]
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(_configuration["Kafka:ConfirmationRequestsTopic"]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                var request = JsonSerializer.Deserialize<ConfirmationRequest>(consumeResult.Message.Value);

                if (request is not null)
                {
                    _logger.LogInformation("Получен запрос для монеты {CoinId} от пользователя {UserId}",
                        request.CoinId, request.UserId);

                    // Обновляем RegisteredObjects в MongoDB
                    using var scope = _serviceProvider.CreateScope();
                    var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                    var usersCollection = mongoDbService.Users;

                    var filter = Builders<User>.Filter.Eq(u => u.Id, request.UserId);
                    var update = Builders<User>.Update.Inc(u => u.RegisteredObjects, 1);

                    var updateResult = await usersCollection.UpdateOneAsync(filter, update, cancellationToken: stoppingToken);

                    if (updateResult.ModifiedCount > 0)
                    {
                        _logger.LogInformation("RegisteredObjects для пользователя {UserId} увеличен", request.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Пользователь {UserId} не найден", request.UserId);
                    }

                    // Отправляем подтверждение в основной проект
                    var response = new ConfirmationResponse
                    {
                        CoinId = request.CoinId,
                        ConfirmedAt = DateTime.UtcNow,
                        Status = "confirmed"
                    };

                    var responseJson = JsonSerializer.Serialize(response);

                    await producer.ProduceAsync(_configuration["Kafka:ConfirmationResponsesTopic"],
                        new Message<string, string>
                        {
                            Key = request.CoinId,
                            Value = responseJson
                        }, stoppingToken);

                    _logger.LogInformation("Подтверждение отправлено для монеты {CoinId}", request.CoinId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer остановлен");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения");
            }
        }

        consumer.Close();
        _logger.LogInformation("KafkaConsumerService завершил работу");
    }

    private class ConfirmationRequest
    {
        public string CoinId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    private class ConfirmationResponse
    {
        public string CoinId { get; set; } = string.Empty;
        public DateTime ConfirmedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}