using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Core.Entities;
using WebAnalytics.Infrastructure.Data;

namespace WebAnalytics.Infrastructure.Services
{
    public interface IReportService
    {
        Task<OverviewReport> GetOverviewReportAsync();
        Task<List<PageReport>> GetPageReportsAsync();

        Task<object> GetHealthMetricsAsync();
    }

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

                var overview = new OverviewReport
                {
                    TotalUsers = dailyStats.Sum(ds => ds.TotalUsers),
                    TotalSessions = dailyStats.Sum(ds => ds.TotalSessions),
                    TotalViews = dailyStats.Sum(ds => ds.TotalViews),
                    AveragePerformance = dailyStats.Any() ? dailyStats.Average(ds => ds.AvgPerformance) : 0,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("📊 Generated overview report");
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

                _logger.LogInformation($"📄 Generated page reports ({pageStats.Count} pages)");
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