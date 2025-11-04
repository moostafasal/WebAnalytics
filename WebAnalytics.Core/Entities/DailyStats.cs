using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Core.Entities
{
    public class DailyStats
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSessions { get; set; }
        public int TotalViews { get; set; }
        public double AvgPerformance { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
