using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Core.Entities;
using WebAnalytics.Infrastructure.Data;
using WebAnalytics.Infrastructure.IServices;

namespace WebAnalytics.Infrastructure.Services
{


    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OverviewReport> GetOverviewReportAsync()
        {
            try
            {
                var dailyStats = await _context.DailyStats.ToListAsync();

                _logger.LogInformation($"📅 Found {dailyStats.Count} DailyStats records in DB");

                if (!dailyStats.Any())
                {
                    _logger.LogWarning("⚠️ No DailyStats data found!");
                }

                var overview = new OverviewReport
                {
                    TotalUsers = dailyStats.Sum(ds => ds.TotalUsers),
                    TotalSessions = dailyStats.Sum(ds => ds.TotalSessions),
                    TotalViews = dailyStats.Sum(ds => ds.TotalViews),
                    AveragePerformance = dailyStats.Any() ? dailyStats.Average(ds => ds.AvgPerformance) : 0,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("✅ Overview report generated successfully.");
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate overview report");
                throw;
            }
        }

        public async Task<List<PageReport>> GetPageReportsAsync()
        {
            try
            {
                var total = await _context.RawData.CountAsync();
                _logger.LogInformation($"📦 Found {total} RawData records in DB");

                if (total == 0)
                {
                    _logger.LogWarning("⚠️ No RawData records found, returning empty report list.");
                    return new List<PageReport>();
                }

                var sampleData = await _context.RawData.Take(3).ToListAsync();
                foreach (var row in sampleData)
                {
                    _logger.LogInformation($"🧩 Sample => Page: {row.Page}, Users: {row.Users}, Sessions: {row.Sessions}, Views: {row.Views}, Perf: {row.PerformanceScore}");
                }

                var pageStats = await _context.RawData
                    .GroupBy(r => r.Page)
                    .Select(g => new PageReport
                    {
                        Page = g.Key,
                        TotalUsers = g.Sum(r => r.Users),
                        TotalSessions = g.Sum(r => r.Sessions),
                        TotalViews = g.Sum(r => r.Views),
                        AveragePerformance = g.Average(r => r.PerformanceScore),
                        AverageLCPms = (int)g.Average(r => r.LCPms)
                    })
                    .ToListAsync();

                _logger.LogInformation($"📄 Generated {pageStats.Count} page reports successfully.");
                return pageStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate page reports");
                throw;
            }
        }

        public async Task<object> GetHealthMetricsAsync()
        {
            try
            {
                var metrics = new
                {
                    TotalRecords = await _context.RawData.CountAsync(),
                    TotalDays = await _context.DailyStats.CountAsync(),
                    UniquePages = await _context.RawData.Select(r => r.Page).Distinct().CountAsync(),
                    DataFrom = await _context.RawData.MinAsync(r => r.Date),
                    DataTo = await _context.RawData.MaxAsync(r => r.Date),
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation("🩺 Health metrics fetched successfully.");
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to fetch metrics");
                throw;
            }
        }
    }
}
