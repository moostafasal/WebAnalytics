using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace WebAnalytics.Infrastructure.MessageBroker
{
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                Port = 5672
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            SetupInfrastructure();
            _logger.LogInformation("Connected to RabbitMQ and infrastructure created");
        }

        private void SetupInfrastructure()
        {
            // Direct Exchange instead of Fanout
            _channel.ExchangeDeclare(
                exchange: "analytics.raw",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null
            );

            // Main Queue with DLQ settings
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "analytics.dlq" }
            };

            _channel.QueueDeclare(
                queue: "analytics.raw.q",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            // Bind queue with routing key
            _channel.QueueBind(
                queue: "analytics.raw.q",
                exchange: "analytics.raw",
                routingKey: "analytics.data"
            );

            // Dead Letter Exchange
            _channel.ExchangeDeclare(
                exchange: "analytics.dlq",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null
            );

            // Dead Letter Queue
            _channel.QueueDeclare(
                queue: "analytics.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Bind DLQ
            _channel.QueueBind(
                queue: "analytics.dlq",
                exchange: "analytics.dlq",
                routingKey: "analytics.data"
            );

            _logger.LogInformation("Created Direct exchange and queues with routing keys");
        }

        public void PublishMessage<T>(T message)
        {
            try
            {
                string jsonMessage;

                if (message is string strMessage)
                {
                    jsonMessage = strMessage;
                }
                else
                {
                    jsonMessage = JsonSerializer.Serialize(message);
                }

                var body = Encoding.UTF8.GetBytes(jsonMessage);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: "analytics.raw",
                    routingKey: "analytics.data",
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation($"Sent message with routing key: analytics.data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to RabbitMQ");
                throw;
            }
        }

        public void StartConsuming(string queueName, Func<string, Task> messageHandler)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation($"Received message from {queueName}");

                try
                {
                    await messageHandler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Successfully processed and acknowledged message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message, sending to DLQ");
                    _channel.BasicNack(ea.DeliveryTag, false, false);

                    await SendToDLQ(message, ex.Message);
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation($"Started listening to queue: {queueName}");
        }

        private async Task SendToDLQ(string originalMessage, string error)
        {
            try
            {
                var dlqMessage = new
                {
                    OriginalMessage = originalMessage,
                    Error = error,
                    FailedAt = DateTime.UtcNow
                };

                var message = JsonSerializer.Serialize(dlqMessage);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: "analytics.dlq",
                    routingKey: "analytics.data",
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogWarning($"Sent failed message to DLQ: {error}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to DLQ");
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connections");
            }
        }
    }
}