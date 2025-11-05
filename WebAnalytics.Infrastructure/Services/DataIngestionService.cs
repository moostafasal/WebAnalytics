using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Infrastructure.IServices;
using WebAnalytics.Infrastructure.MessageBroker;

namespace WebAnalytics.Infrastructure.Services
{

    public class DataIngestionService : IDataIngestionService
    {
        private readonly RabbitMQService _rabbitMQService;
        private readonly ILogger<DataIngestionService> _logger;

        public DataIngestionService(RabbitMQService rabbitMQService, ILogger<DataIngestionService> logger)
        {
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        public async Task<bool> IngestFromJsonFilesAsync(string gaFilePath, string psiFilePath)
        {
            try
            {
                _logger.LogInformation("Starting data ingestion from JSON files");

                var gaData = await ReadJsonFileAsync<List<GoogleAnalyticsData>>(gaFilePath);
                var psiData = await ReadJsonFileAsync<List<PageSpeedData>>(psiFilePath);

                _logger.LogInformation("Loaded {GaCount} GA records and {PsiCount} PSI records",
                    gaData?.Count ?? 0, psiData?.Count ?? 0);

                var combinedRecords = CombineData(gaData, psiData);

                _logger.LogInformation("Preparing to send {Count} messages to RabbitMQ", combinedRecords.Count);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                foreach (var record in combinedRecords)
                {
                    try
                    {
                        var message = JsonSerializer.Serialize(record, jsonOptions);
                        _rabbitMQService.PublishMessage(message);
                        _logger.LogInformation("Sent: {Page} - {Date:yyyy-MM-dd}", record.Page, record.Date);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send: {Page}", record.Page);
                    }
                }

                _logger.LogInformation("Successfully sent {Count} records", combinedRecords.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data ingestion failed");
                return false;
            }
        }
        private async Task<T> ReadJsonFileAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var result = JsonSerializer.Deserialize<T>(json, options);

            if (result == null)
                throw new InvalidDataException($"Invalid JSON in file: {filePath}");

            return result;
        }

        private List<AnalyticsMessage> CombineData(List<GoogleAnalyticsData> gaData, List<PageSpeedData> psiData)
        {
            var combined = new List<AnalyticsMessage>();

            if (gaData == null || psiData == null)
                return combined;

            foreach (var ga in gaData)
            {
                if (ga == null) continue;

                if (string.IsNullOrWhiteSpace(ga.Date) || string.IsNullOrWhiteSpace(ga.Page))
                {
                    _logger.LogWarning("Skipping invalid GA record: Date='{Date}', Page='{Page}'", ga.Date, ga.Page);
                    continue;
                }

                var psi = psiData.FirstOrDefault(p =>
                    p != null &&
                    !string.IsNullOrWhiteSpace(p.Date) &&
                    !string.IsNullOrWhiteSpace(p.Page) &&
                    p.Date.Trim() == ga.Date.Trim() &&
                    p.Page.Trim() == ga.Page.Trim());

                if (psi != null)
                {
                    if (DateTime.TryParse(ga.Date.Trim(), out DateTime parsedDate))
                    {
                        combined.Add(new AnalyticsMessage
                        {
                            Page = ga.Page.Trim(),
                            Date = parsedDate,
                            Users = ga.Users,
                            Sessions = ga.Sessions,
                            Views = ga.Views,
                            PerformanceScore = psi.PerformanceScore,
                            LCPms = psi.LCP_ms
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse date: '{Date}' for page: {Page}", ga.Date, ga.Page);
                    }
                }
            }

            _logger.LogInformation("Combined {Count} records from {GaCount} GA and {PsiCount} PSI records",
                combined.Count, gaData.Count, psiData.Count);

            return combined;
        }
    }
}