namespace HttpMetrics
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "apilogs.txt");

        public static async Task WriteLogAsync(ApiLog log)
        {
            try
            {
                var line = log.ToString();
                await File.AppendAllTextAsync(LogFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to write log: {ex.Message}");
            }
        }
    }
}
