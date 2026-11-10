using Confluent.Kafka;
using MongoDB.Driver;
using NumismaticClub.Models;
using System.Text.Json;

namespace NumismaticClub.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly RedisCacheService _cacheService;
        private readonly ConfirmationMetricsService _confirmationMetrics;

        public KafkaConsumerService(
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumerService> logger,
            IConfiguration configuration,
            RedisCacheService cacheService,
            ConfirmationMetricsService confirmationMetrics)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
            _confirmationMetrics = confirmationMetrics;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Consumer Service запущен");

            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = "irisps-coins-confirmation-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_configuration["Kafka:ConfirmationResponsesTopic"]);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    _logger.LogInformation($"Получено сообщение: {consumeResult.Message.Value}");

                    var response = JsonSerializer.Deserialize<ConfirmationResponse>(consumeResult.Message.Value);

                    if (response != null && response.Status == "confirmed")
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var mongoDbService = scope.ServiceProvider.GetRequiredService<MongoDbService>();
                        var coinsCollection = mongoDbService.Coins;

                        // Обновляем монету в MongoDB
                        var update = Builders<MongoCoin>.Update
                            .Set(c => c.ConfirmationStatus, "confirmed")
                            .Set(c => c.ConfirmedAt, response.ConfirmedAt);

                        var updateResult = await coinsCollection.UpdateOneAsync(
                            c => c.Id == response.CoinId,
                            update,
                            cancellationToken: stoppingToken);

                        if (updateResult.ModifiedCount > 0)
                        {
                            _logger.LogInformation($"Монета {response.CoinId} подтверждена");

                            // Вычисляем задержку между созданием монеты и подтверждением
                            var coin = await coinsCollection.Find(c => c.Id == response.CoinId).FirstOrDefaultAsync();
                            if (coin != null && coin.CreatedAt != default)
                            {
                                var delay = (response.ConfirmedAt - coin.CreatedAt).TotalSeconds;
                                _confirmationMetrics.RecordConfirmationSuccess(delay);
                            }

                            // Очищаем кэш, чтобы обновлённые данные попали в ответы API
                            await _cacheService.RemoveAsync($"coin_{response.CoinId}");
                            await _cacheService.RemoveAsync("coins_list");
                        }
                        else
                        {
                            _logger.LogWarning($"Монета {response.CoinId} не найдена");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения из Kafka");
                }
            }

            consumer.Close();
        }
    }

    public class ConfirmationResponse
    {
        public string CoinId { get; set; } = string.Empty;
        public DateTime ConfirmedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}