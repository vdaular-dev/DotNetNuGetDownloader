namespace DotNetNuGetDownloader;

public class RuntimeDll
{
    public string PackageIdentity { get; set; }
    public string FileName { get; set; }
    public string RelativeFilePath { get; set; }
    public string FullFilePath { get; set; }
    public string RID { get; set; }
    public string TargetFramework { get; set; }
    public string TargetFrameworkShortName { get; set; }
    public string TargetVersion { get; set; }
    public bool IsSupported { get; set; }
    public bool IsRecomended { get; set; }

    public bool IsNative
    {
        get
        {
            return string.Equals(TargetFramework, "native", StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public bool IsLib
    {
        get
        {
            return !IsNative;
        }
    }
}
