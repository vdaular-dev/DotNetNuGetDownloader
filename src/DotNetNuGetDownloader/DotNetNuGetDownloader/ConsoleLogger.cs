using NuGet.Common;

namespace DotNetNuGetDownloader;

public class ConsoleLogger : LoggerBase
{
    public override void Log(ILogMessage message)
    {
        string prefix = string.Empty;

        switch (message.Level)
        {
            case LogLevel.Debug:
                prefix = "DEBUG";
                break;

            case LogLevel.Verbose:
                prefix = "VERBOSE";
                break;

            case LogLevel.Information:
                prefix = "INFORMATION";
                break;

            case LogLevel.Minimal:
                prefix = "MINIMAL";
                break;

            case LogLevel.Warning:
                prefix = "WARNING";
                break;

            case LogLevel.Error:
                prefix = "ERROR";
                break;
        }

        Console.WriteLine($"{prefix} - {message}");
    }

    public override Task LogAsync(ILogMessage message)
    {
        Log(message);

        return Task.CompletedTask;
    }
}
