using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Infrastructure.MessageBroker;

namespace WebAnalytics.Infrastructure.Services
{
    namespace WebAnalytics.Infrastructure.Services
    {
        public interface IDataIngestionService
        {
            Task<bool> IngestFromJsonFilesAsync(string gaFilePath, string psiFilePath);
        }

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
                    _logger.LogInformation("🚀 Starting data ingestion from JSON files");

                    // Read JSON files
                    var gaData = await ReadJsonFileAsync<List<GoogleAnalyticsData>>(gaFilePath);
                    var psiData = await ReadJsonFileAsync<List<PageSpeedData>>(psiFilePath);

                    // Combine data
                    var combinedRecords = CombineData(gaData, psiData);

                    // Send each record to RabbitMQ
                    foreach (var record in combinedRecords)
                    {
                        _rabbitMQService.PublishMessage(record);
                        _logger.LogInformation($"📤 Sent page data: {record.Page}");
                    }

                    _logger.LogInformation($"✅ Successfully sent {combinedRecords.Count} records");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Data ingestion failed");
                    return false;
                }
            }

            private async Task<T> ReadJsonFileAsync<T>(string filePath)
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<T>(json) ?? throw new Exception($"Invalid file: {filePath}");
            }

            private List<AnalyticsMessage> CombineData(List<GoogleAnalyticsData> gaData, List<PageSpeedData> psiData)
            {
                var combined = new List<AnalyticsMessage>();

                foreach (var ga in gaData)
                {
                    var psi = psiData.FirstOrDefault(p => p.Page == ga.Page && p.Date == ga.Date);
                    if (psi != null)
                    {
                        combined.Add(new AnalyticsMessage
                        {
                            Page = ga.Page,
                            Date = DateTime.Parse(ga.Date),
                            Users = ga.Users,
                            Sessions = ga.Sessions,
                            Views = ga.Views,
                            PerformanceScore = psi.PerformanceScore,
                            LCPms = psi.LCP_ms
                        });
                    }
                }

                return combined;
            }
        }
    }
}
