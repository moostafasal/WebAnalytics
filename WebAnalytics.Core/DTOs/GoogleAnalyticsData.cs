using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebAnalytics.Core.DTOs
{
    public class GoogleAnalyticsData
    {

        public string Date { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int Views { get; set; }
    }
}
