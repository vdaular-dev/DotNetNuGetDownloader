namespace DotNetNuGetDownloader;

public class NuGetContext
{
    public string TargetFramework { get; }
    public string TargetFrameworkShortName { get; }
    public string TargetVersion { get; }
    public string Rid { get; set; }
    public List<string> SupportedRids { get; set; }
    public string Folder { get; }
    public string PackageName { get; }
    public string PackageVersion { get; }

    public NuGetContext(
        string targetFramework,
        string targetFrameworkShortName,
        string targetVersion,
        string folder,
        string packageName,
        string packageVersion,
        string rid,
        List<string> supportedRids
    )
    {
        TargetFramework = targetFramework;
        TargetFrameworkShortName = targetFrameworkShortName;
        TargetVersion = targetVersion;
        Rid = rid;
        SupportedRids = supportedRids;
        Folder = folder;
        PackageName = packageName;
        PackageVersion = packageVersion;
    }
}
