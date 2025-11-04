using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Core.DTOs
{
    public class PageSpeedData
    {
        public string Date { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public decimal PerformanceScore { get; set; }
        public int LCP_ms { get; set; }
    }
}
