using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAnalytics.Core.DTOs;
using WebAnalytics.Core.Entities;
using WebAnalytics.Infrastructure.Data;
using WebAnalytics.Infrastructure.IServices;

namespace WebAnalytics.Infrastructure.Services
{


    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessAnalyticsMessageAsync(AnalyticsMessage message)
        {
            try
            {
                // ✅ Ensure date is stored as UTC
                message.Date = message.Date.Kind == DateTimeKind.Utc
                    ? message.Date
                    : message.Date.ToUniversalTime();

                var dateOnly = message.Date.Date;

                // ✅ Check for duplicate based on date-only
                var exists = await _context.RawData
                    .AnyAsync(r => r.Page == message.Page && r.Date.Date == dateOnly);

                if (!exists)
                {
                    var rawData = new RawData
                    {
                        Date = dateOnly, // already UTC normalized
                        Page = message.Page,
                        Users = message.Users,
                        Sessions = message.Sessions,
                        Views = message.Views,
                        PerformanceScore = message.PerformanceScore,
                        LCPms = message.LCPms,
                        ReceivedAt = DateTime.UtcNow // always UTC
                    };

                    await _context.RawData.AddAsync(rawData);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"💾 Saved raw data for page: {message.Page}");
                }
                else
                {
                    _logger.LogInformation($"ℹ️ Page data {message.Page} already exists for {dateOnly:yyyy-MM-dd}");
                }

                // Update daily statistics
                await UpdateDailyStatsAsync(dateOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to process page data: {message.Page}");
                throw;
            }
        }

        public async Task UpdateDailyStatsAsync(DateTime date)
        {
            try
            {
                var dateOnly = date.Date;

                // Calculate statistics for the specific date
                var dayRecords = await _context.RawData
                    .Where(r => r.Date.Date == dateOnly)
                    .ToListAsync();

                if (dayRecords.Any())
                {
                    var dailyStat = await _context.DailyStats
                        .FirstOrDefaultAsync(ds => ds.Date == dateOnly);

                    if (dailyStat == null)
                    {
                        dailyStat = new DailyStats
                        {
                            Date = dateOnly,
                            TotalUsers = dayRecords.Sum(r => r.Users),
                            TotalSessions = dayRecords.Sum(r => r.Sessions),
                            TotalViews = dayRecords.Sum(r => r.Views),
                            AvgPerformance = dayRecords.Average(r => r.PerformanceScore),
                            LastUpdatedAt = DateTime.UtcNow
                        };
                        await _context.DailyStats.AddAsync(dailyStat);
                    }
                    else
                    {
                        dailyStat.TotalUsers = dayRecords.Sum(r => r.Users);
                        dailyStat.TotalSessions = dayRecords.Sum(r => r.Sessions);
                        dailyStat.TotalViews = dayRecords.Sum(r => r.Views);
                        dailyStat.AvgPerformance = dayRecords.Average(r => r.PerformanceScore);
                        dailyStat.LastUpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"📊 Updated daily stats for: {dateOnly:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update daily stats for: {date:yyyy-MM-dd}");
                throw;
            }
        }
    }
}
