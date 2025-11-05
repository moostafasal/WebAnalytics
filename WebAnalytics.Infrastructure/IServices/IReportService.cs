using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAnalytics.Core.DTOs;

namespace WebAnalytics.Infrastructure.IServices
{
    public interface IReportService
    {
        Task<OverviewReport> GetOverviewReportAsync();
        Task<List<PageReport>> GetPageReportsAsync();
        Task<object> GetHealthMetricsAsync();
    }
}
