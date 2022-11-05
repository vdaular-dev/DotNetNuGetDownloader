﻿using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Packaging;
using System.IO.Compression;
using NuGet.Protocol.Core.Types;
using Microsoft.Extensions.DependencyModel;
using NuGet.ProjectManagement;

namespace DotNetNuGetDownloader;

public class ModuleFolderNuGetProject : FolderNuGetProject
{
    private const string ModuleAssemblyFilesFileName = "moduleAssemblyFiles.txt";

    private readonly string _root;
    private readonly IPackageSearchMetadata _moduleNuGetPackage;
    private readonly NuGetFramework _targetFramework;
    private readonly bool _onlyDownload;
    private readonly bool _filterOurRefFiles;
    private CompatibilityProvider _compProvider;
    private FrameworkReducer _reducer;

    public string Rid { get; set; }
    public List<string> SupportedRids { get; }
    public List<DllInfo> InstalledDlls { get; } = new();
    public List<RuntimeDll> RuntimeDlls { get; } = new();
    public List<string> InstalledPackages { get; } = new();

    public ModuleFolderNuGetProject(
        string root,
        IPackageSearchMetadata moduleNuGetPackage,
        NuGetFramework targetFramework,
        bool onlyDownload = false,
        string targetRid = null,
        bool filterOurRefFiles = false
    ) : base(root, new PackagePathResolver(root), targetFramework)
    {
        _root = root;
        _moduleNuGetPackage = moduleNuGetPackage;
        _targetFramework = targetFramework;
        _onlyDownload = onlyDownload;
        _filterOurRefFiles = filterOurRefFiles;
        _compProvider = new CompatibilityProvider(new DefaultFrameworkNameProvider());
        _reducer = new FrameworkReducer(new DefaultFrameworkNameProvider(), _compProvider);
        SupportedRids = GetSupportedRids(targetRid);
    }

    public override async Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken token) => await base.GetInstalledPackagesAsync(token);

    public override async Task<bool> InstallPackageAsync(
        PackageIdentity packageIdentity,
        DownloadResourceResult downloadResourceResult,
        INuGetProjectContext nuGetProjectContext,
        CancellationToken token
    )
    {
        var result = await base.InstallPackageAsync(packageIdentity, downloadResourceResult, nuGetProjectContext, token);

        if (_onlyDownload)
            return result;

        InstalledPackages.Add(packageIdentity.ToString());

        using (var zipArchive = new ZipArchive(downloadResourceResult.PackageStream))
        {
            var zipArchiveEntries = zipArchive.Entries
                .Where(e => e.Name.EndsWith(".dll") || e.Name.EndsWith(".exe")).ToList();

            await HandleManagedDlls(packageIdentity, zipArchiveEntries);
            HandleRuntimeDlls(packageIdentity, zipArchiveEntries);
        }

        return result;
    }

    private async Task HandleManagedDlls(PackageIdentity packageIdentity, List<ZipArchiveEntry> zipArchiveEntries)
    {
        var entriesWithTargetFramework = zipArchiveEntries
                .Select(e => new { TargetFramework = NuGetFramework.Parse(e.FullName.Split('/')[1]), Entry = e }).ToList();

        if (_filterOurRefFiles)
            entriesWithTargetFramework = entriesWithTargetFramework.Where(e => !string.Equals(e.Entry.FullName.Split('/')[0], "ref")).ToList();

        var compatibleEntries = entriesWithTargetFramework.Where(e => _compProvider.IsCompatible(_targetFramework, e.TargetFramework)).ToList();
        var mostCompatibleFramework = _reducer.GetNearest(_targetFramework, compatibleEntries.Select(x => x.TargetFramework));

        if (mostCompatibleFramework == null)
            return;

        var matchingEntries = entriesWithTargetFramework.Where(e => e.TargetFramework == mostCompatibleFramework).ToList();

        if (matchingEntries.Any())
        {
            var moduleAssemblies = new List<string>();

            foreach (var entry in matchingEntries)
            {
                ZipFileExtensions.ExtractToFile(entry.Entry, Path.Combine(_root, entry.Entry.Name), overwrite: true);

                var installedDllInfo = new DllInfo
                {
                    RelativeFilePath = Path.Combine(packageIdentity.ToString(), entry.Entry.FullName),
                    FullFilePath = Path.Combine(_root, packageIdentity.ToString(), entry.Entry.FullName),
                    FileName = entry.Entry.Name,
                    TargetFrameworkName = entry.TargetFramework.ToString(),
                    TargetFrameworkShortName = entry.TargetFramework.GetShortFolderName(),
                    PackageIdentity = packageIdentity.Id,
                    TargetVersion = entry.TargetFramework.Version.ToString()
                };

                InstalledDlls.Add(installedDllInfo);

                if (packageIdentity.Id == _moduleNuGetPackage.Identity.Id)
                    moduleAssemblies.Add(entry.Entry.FullName);
            }

            await File.WriteAllLinesAsync(Path.Combine(_root, ModuleAssemblyFilesFileName), moduleAssemblies);
        }
    }

    public void HandleRuntimeDlls(PackageIdentity packageIdentity, List<ZipArchiveEntry> zipArchiveEntries)
    {
        var runtimeEntries = zipArchiveEntries
            .Where(e => string.Equals(e.FullName.Split('/')[0], "runtimes", StringComparison.InvariantCultureIgnoreCase))
            .Select(e => new { Rid = e.FullName.Split('/')[1], Entry = e }).ToList();

        foreach (var runtime in runtimeEntries)
        {
            var runtimeDll = new RuntimeDll()
            {
                FileName = runtime.Entry.Name,
                FullFilePath = Path.Combine(_root, packageIdentity.ToString(), runtime.Entry.FullName),
                RelativeFilePath = Path.Combine(packageIdentity.ToString(), runtime.Entry.FullName),
                PackageIdentity = packageIdentity.Id
            };

            var runtimeDllDetails = ParseRuntimeDllDetails(runtime.Entry.FullName);
            runtimeDll.RID = runtimeDllDetails.Rid;
            runtimeDll.TargetFramework = runtimeDllDetails.Target;
            runtimeDll.TargetFrameworkShortName = runtimeDllDetails.TargetShortName;
            runtimeDll.TargetVersion = runtimeDllDetails.TargetVersion;

            RuntimeDlls.Add(runtimeDll);
        }

        var supportedRuntimeDlls = RuntimeDlls.Where(x => SupportedRids.Contains(x.RID)).ToList();
        var runtimeLibFiles = supportedRuntimeDlls.Where(x => x.IsLib).GroupBy(x => x.FileName).ToList();

        foreach (var fileGroup in runtimeLibFiles)
        {
            var targetFrameworks = fileGroup.Select(x => NuGetFramework.ParseFrameworkName(x.TargetFramework, new DefaultFrameworkNameProvider())).ToList();

            var compatibleFrameworks = targetFrameworks.Where(x => _compProvider.IsCompatible(_targetFramework, x)).ToList();

            foreach (var runtimeDll in fileGroup)
                if (compatibleFrameworks.Any(x => string.Equals(x.DotNetFrameworkName, runtimeDll.TargetFramework)))
                    runtimeDll.IsSupported = true;

            var mostMatching = _reducer.GetNearest(_targetFramework, targetFrameworks);

            if (mostMatching == null)
                continue;

            foreach (var runtimeDll in fileGroup)
                if (string.Equals(runtimeDll.TargetFramework, mostMatching.DotNetFrameworkName))
                    runtimeDll.IsRecomended = true;
        }

        var runtimeNativeFiles = supportedRuntimeDlls.Where(x => x.IsNative).GroupBy(x => x.FileName).ToList();

        foreach (var fileGroup in runtimeNativeFiles)
        {
            foreach (var runtimeDll in fileGroup)
                runtimeDll.IsSupported = true;

            // The Rids are already ordered from best match to the least matching
            var recommendedFound = false;

            foreach (var supportedRid in SupportedRids)
            {
                foreach (var runtimeDll in fileGroup)
                {
                    if (string.Equals(runtimeDll.RID, supportedRid))
                    {
                        runtimeDll.IsRecomended = true;
                        recommendedFound = true;

                        break;
                    }

                    if (recommendedFound)
                        break;
                }
            }
        }
    }

    private (string Rid, string Target, string TargetShortName, string TargetVersion) ParseRuntimeDllDetails(string path)
    {
        var parts = path.Split('/');
        var rid = parts[1];
        var target = parts[2];

        if (string.Equals(target, "native", StringComparison.InvariantCultureIgnoreCase))
        {
            target = "native";

            return (rid, target, null, null);
        }

        var libPath = parts[3];
        var tf = NuGetFramework.ParseFolder(libPath);

        return (rid, tf.DotNetFrameworkName, libPath, tf.Version.ToString());
    }

    private List<string> GetSupportedRids(string targetRid)
    {
        Rid = string.IsNullOrWhiteSpace(targetRid) ? Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier() : targetRid;

        var dependencyContext = DependencyContext.Default;
        var fallbacks = dependencyContext.RuntimeGraph
            .Single(x => string.Equals(x.Runtime, Rid, StringComparison.InvariantCultureIgnoreCase));

        var result = new List<string>();

        foreach (var runtimeFallback in fallbacks.Fallbacks)
            result.Add(runtimeFallback);

        return result;
    }

    public async Task<string[]> GetModuleAssemblyFilesAsync() => await File.ReadAllLinesAsync(Path.Combine(_root, ModuleAssemblyFilesFileName));

    public override async Task<bool> UninstallPackageAsync(PackageIdentity packageIdentity, INuGetProjectContext nuGetProjectContext, CancellationToken token) =>
        await base.UninstallPackageAsync(packageIdentity, nuGetProjectContext, token);
}
