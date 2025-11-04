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
                        _logger.LogWarning(exception, $"🔄 Attempt {attempt} failed, retrying in {timeSpan.TotalSeconds} seconds");
                    });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await ProcessSingleMessage(message);
            });
        }

        private async Task ProcessSingleMessage(string message)
        {
            try
            {
                var analyticsMessage = JsonSerializer.Deserialize<AnalyticsMessage>(message);
                if (analyticsMessage == null)
                {
                    throw new Exception("Cannot convert message to AnalyticsMessage");
                }

                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                await analyticsService.ProcessAnalyticsMessageAsync(analyticsMessage);

                _logger.LogInformation($"✅ Processed page data: {analyticsMessage.Page}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to process message after all attempts");
                throw;
            }
        }
    }
}