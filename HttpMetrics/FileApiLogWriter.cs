namespace HttpMetrics;

public class FileApiLogWriter : IApiLogWriter
{
    private static readonly string LogFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "apilogs.txt");

    public async Task WriteLogAsync(ApiLog log)
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
