namespace HttpMetrics
{
    public class MetricOption
    {
        public string[]? AllowedHosts { get; set; }
        public Route[]? Routes { get; set; }
    }

    public class Route
    {
        public string? Template { get; set; }
        public string? Name { get; set; }
    }
}
