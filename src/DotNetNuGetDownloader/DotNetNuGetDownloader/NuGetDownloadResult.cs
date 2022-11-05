namespace DotNetNuGetDownloader;

public class NuGetDownloadResult
{
    public NuGetContext Context { get; set; }
    public List<string> PackageAssemblyFiles { get; set; }
    public List<RuntimeDll> RuntimeDlls { get; set; }
    public List<DllInfo> InstalledDlls { get; set; }
    public List<string> InstalledPackages { get; set; }
}
