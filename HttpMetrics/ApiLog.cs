namespace HttpMetrics
{
    public class ApiLog
    {
        public string CorrelationId { get; set; }
        public string ApplicationName { get; set; }
        public string Verb { get; set; }
        public string Api { get; set; }
        public string Url { get; set; }
        public bool IsSuccessful { get; set; }
        public double TimeTakenInMs { get; set; }

        private readonly Dictionary<string, object> _customValues = new();

        public void SetValue(string key, object value)
        {
            _customValues[key] = value;
        }

        public IReadOnlyDictionary<string, object> GetValues() => _customValues;

        public override string ToString()
        {
            return $"{DateTime.UtcNow:o} | CorrelationId={CorrelationId}, " +
                   $"App={ApplicationName}, Verb={Verb}, Api={Api}, Url={Url}, " +
                   $"Success={IsSuccessful}, TimeTakenMs={TimeTakenInMs}, " +
                   $"Extras={string.Join(",", _customValues)}";
        }
    }
}
