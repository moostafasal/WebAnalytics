using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAnalytics.Infrastructure.MessageBroker
{
    public class RabbitMQService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly RabbitMQ.Client.IModel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:Username"] ?? "admin",
                Password = configuration["RabbitMQ:Password"] ?? "admin123",

            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            SetupInfrastructure();
            _logger.LogInformation("✅ Connected to RabbitMQ and infrastructure created");
        }

        private void SetupInfrastructure()
        {
            // Main Exchange
            _channel.ExchangeDeclare("analytics.raw", ExchangeType.Fanout, durable: true);

            // Main Queue with DLQ settings
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "analytics.dlq" }
            };

            _channel.QueueDeclare("analytics.raw.q", durable: true, exclusive: false, autoDelete: false, arguments: args);
            _channel.QueueBind("analytics.raw.q", "analytics.raw", "");

            // Dead Letter Queue
            _channel.QueueDeclare("analytics.dlq", durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("✅ Created queues: analytics.raw.q and analytics.dlq");
        }

        //public void PublishMessage<T>(T message)
        //{
        //    try
        //    {
        //        var jsonMessage = JsonSerializer.Serialize(message);
        //        var body = Encoding.UTF8.GetBytes(jsonMessage);

        //        _channel.BasicPublish(
        //            exchange: "analytics.raw",
        //            routingKey: "",
        //            basicProperties: null,
        //            body: body
        //        );

        //        _logger.LogInformation($"📤 Sent message: {typeof(T).Name}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "❌ Failed to send message to RabbitMQ");
        //        throw;
        //    }
        //}
        public void PublishMessage<T>(T message)
        {
            try
            {
                string jsonMessage;

                // 👇 لو اللي جاي أصلاً String (يعني JSON جاهز)، ما نعملوش Serialize تاني
                if (message is string strMessage)
                {
                    jsonMessage = strMessage;
                }
                else
                {
                    jsonMessage = JsonSerializer.Serialize(message);
                }

                var body = Encoding.UTF8.GetBytes(jsonMessage);

                _channel.BasicPublish(
                    exchange: "analytics.raw",
                    routingKey: "",
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation($"📤 Sent message: {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send message to RabbitMQ");
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

                _logger.LogInformation($"📥 Received message from {queueName}");

                try
                {
                    await messageHandler(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("✅ Successfully processed and acknowledged message");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to process message, sending to DLQ");
                    _channel.BasicNack(ea.DeliveryTag, false, false);

                    // Send to DLQ
                    await SendToDLQ(message, ex.Message);
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation($"👂 Started listening to queue: {queueName}");
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

                _channel.BasicPublish("", "analytics.dlq", null, body);
                _logger.LogWarning($"📮 Sent failed message to DLQ: {error}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send message to DLQ");
            }
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
