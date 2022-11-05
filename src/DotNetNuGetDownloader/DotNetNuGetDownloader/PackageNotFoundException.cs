namespace DotNetNuGetDownloader;

public class PackageNotFoundException : Exception
{
    public PackageNotFoundException(string message)
        : base(message)
    {
    }
}
