using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text.Json;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Infrastructure.MessageBroker;
using WebAnalytics.Infrastructure.Services;

namespace WebAnalytics.Consumer
{
    public class Worker : BackgroundService
    {
        private readonly RabbitMQService _rabbitMQService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(
            RabbitMQService rabbitMQService,
            IServiceProvider serviceProvider,
            ILogger<Worker> logger)
        {
            _rabbitMQService = rabbitMQService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Worker started receiving data from RabbitMQ");

            _rabbitMQService.StartConsuming("analytics.raw.q", async message =>
            {
                await ProcessMessageWithRetry(message, maxRetries: 3);
            });

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessMessageWithRetry(string message, int maxRetries)
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: maxRetries,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, attempt, context) =>
                    {
                        _logger.LogWarning(exception, "🔄 Attempt {Attempt} failed, retrying in {Seconds} seconds", attempt, timeSpan.TotalSeconds);
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {

                await ProcessSingleMessage(message);
            });
        }

        private async Task ProcessSingleMessage(string message)
        {
            _logger.LogInformation("📨 Received message: {MessageLength} characters", message.Length);

            // 👇 أضف السطر ده هنا قبل أي Deserialize
            _logger.LogError("📦 RAW MESSAGE: " + message);

            try
            {
                _logger.LogInformation("📨 Received message: {MessageLength} characters", message.Length);

                // Simple JSON deserialization with case-insensitive option
                var options = new JsonSerializerOptions

                {
                    PropertyNameCaseInsensitive = true  // This fixes the "lcPms" vs "LCPms" issue
                };

                var analyticsMessage = JsonSerializer.Deserialize<AnalyticsMessage>(message, options);

                if (analyticsMessage == null)
                {
                    throw new Exception("Invalid message format - cannot deserialize");
                }

                _logger.LogInformation("🔍 Processing: {Page} - {Date:yyyy-MM-dd} - LCPms: {LCPms}",
                    analyticsMessage.Page, analyticsMessage.Date, analyticsMessage.LCPms);

                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                await analyticsService.ProcessAnalyticsMessageAsync(analyticsMessage);

                _logger.LogInformation("✅ Successfully processed: {Page}", analyticsMessage.Page);
                _logger.LogInformation("Received message: {Message}", message);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to process message");
                _logger.LogInformation("Received message: {Message}", message);

                throw;
            }
        }
    }
}