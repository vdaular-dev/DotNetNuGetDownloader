namespace DotNetNuGetDownloader;

public class DllInfo
{
    public string PackageIdentity { get; set; }
    public string FileName { get; set; }
    public string RelativeFilePath { get; set; }
    public string FullFilePath { get; set; }
    public string TargetFrameworkName { get; set; }
    public string TargetFrameworkShortName { get; set; }
    public string TargetVersion { get; set; }
}
