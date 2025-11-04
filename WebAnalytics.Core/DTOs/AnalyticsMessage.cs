using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Core.DTOs
{
    public class AnalyticsMessage
    {
        public string Page { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int Views { get; set; }
        public decimal PerformanceScore { get; set; }
        public int LCPms { get; set; }
    }
}
