using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpMetrics
{
    public class PerformanceMetricData
    {
        public DateTime StartTime { get; set; }
        public double Duration { get; set; }
        public string ApiName { get; set; }
        public Uri Url { get; set; }
        public string HttpMethod { get; set; }
        public Exception Exception { get; set; }
        public string CorrelationId { get; set; }
        public bool IsSuccess { get; set; }
    }
}
