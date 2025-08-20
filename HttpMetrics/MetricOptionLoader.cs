using System.Text.Json;

namespace HttpMetrics
{
    public static class MetricOptionLoader
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static MetricOption FromEnvironment()
        {
            var json = Environment.GetEnvironmentVariable("CLIENT_METRIC_OPTIONS") ?? "{}";
            return JsonSerializer.Deserialize<MetricOption>(json, jsonSerializerOptions);
        }
    }
}
