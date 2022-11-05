using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using System.Globalization;
using System.Xml.Linq;

namespace DotNetNuGetDownloader;

public class FolderProjectContext : INuGetProjectContext
{
    private readonly ILogger _logger;

    public FolderProjectContext(ILogger logger)
    {
        _logger = logger;
    }

    public PackageExtractionContext PackageExtractionContext { get; set; }

    public ISourceControlManagerProvider SourceControlManagerProvider => null;

    public NuGet.ProjectManagement.ExecutionContext ExecutionContext => null;

    public XDocument OriginalPackagesConfig { get; set; }

    public NuGetActionType ActionType { get; set; }

    public Guid OperationId { get; set; }

    public void Log(MessageLevel level, string message, params object[] args)
    {
        if (args.Length > 0)
            message = string.Format(CultureInfo.CurrentCulture, message, args);

        switch (level)
        {
            case MessageLevel.Info:
                _logger.LogMinimal(message);
                break;

            case MessageLevel.Warning:
                _logger.LogWarning(message);
                break;

            case MessageLevel.Debug:
                _logger.LogDebug(message);
                break;

            case MessageLevel.Error:
                _logger.LogError(message);
                break;
        }
    }

    public void Log(ILogMessage message) => _logger.Log(message);

    public void ReportError(string message) => _logger.LogError(message);

    public void ReportError(ILogMessage message) => _logger.Log(message);

    public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.IgnoreAll;
}
