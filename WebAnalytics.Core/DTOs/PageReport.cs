using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Core.DTOs
{
    public class PageReport
    {
        public string Page { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int TotalSessions { get; set; }
        public int TotalViews { get; set; }
        public decimal AveragePerformance { get; set; }
        public int AverageLCPms { get; set; }
    }
}
