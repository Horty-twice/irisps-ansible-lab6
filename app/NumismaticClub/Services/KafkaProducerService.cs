using Confluent.Kafka;
using System.Text.Json;

namespace NumismaticClub.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _requestsTopic;

        public KafkaProducerService(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
            _requestsTopic = configuration["Kafka:ConfirmationRequestsTopic"];
        }

        public async Task SendConfirmationRequestAsync(string coinId, string userId)
        {
            var message = new
            {
                CoinId = coinId,
                UserId = userId
            };
            var json = JsonSerializer.Serialize(message);

            await _producer.ProduceAsync(_requestsTopic, new Message<string, string>
            {
                Key = coinId,
                Value = json
            });
        }
    }
}