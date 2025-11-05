using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAnalytics.Core.DTOs;

namespace WebAnalytics.Infrastructure.IServices
{
    public interface IAnalyticsService
    {
        Task ProcessAnalyticsMessageAsync(AnalyticsMessage message);
        Task UpdateDailyStatsAsync(DateTime date);
    }
}
