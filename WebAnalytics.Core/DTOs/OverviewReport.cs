using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Core.DTOs
{
    public class OverviewReport
    {
        public int TotalUsers { get; set; }
        public int TotalSessions { get; set; }
        public int TotalViews { get; set; }
        public decimal AveragePerformance { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
