using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Services.SAP;

public interface ICorsuiteFileScannerService
{
    Task<IEnumerable<SapAddOnInfo>> ScanCorsuiteFoldersAsync();
    Task<IEnumerable<SapAddOnInfo>> ScanFolderAsync(string folderPath);
    IEnumerable<string> GetPossibleCorsuitePaths();
}

public class CorsuiteFileScannerService : ICorsuiteFileScannerService
{
    private readonly ILogger<CorsuiteFileScannerService> _logger;
    private readonly string[] _commonCorsuitePaths;

    public CorsuiteFileScannerService(ILogger<CorsuiteFileScannerService> logger)
    {
        _logger = logger;
        
        // Common locations for Corsuite installation - return all paths
        // On Linux, some won't exist but should still be returned
        _commonCorsuitePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Corsuite"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Corsuite"),
            @"C:\Program Files\Corsuite",
            @"C:\Program Files (x86)\Corsuite",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Corsuite"),
            @"C:\Corsuite",
            @"C:\Program Files\SAP\Corsuite",
            @"C:\Program Files (x86)\SAP\Corsuite",
            // Linux paths
            "/opt/corsuite",
            "/usr/lib/corsuite",
            "/home/corsuite"
        };
    }

    public IEnumerable<string> GetPossibleCorsuitePaths()
    {
        // Return all paths - let caller filter by existence if needed
        return _commonCorsuitePaths.AsEnumerable();
    }

    public async Task<IEnumerable<SapAddOnInfo>> ScanCorsuiteFoldersAsync()
    {
        var allModules = new List<SapAddOnInfo>();

        foreach (var path in _commonCorsuitePaths)
        {
            if (Directory.Exists(path))
            {
                _logger.LogInformation("Scanning Corsuite folder: {Path}", path);
                var modules = await ScanFolderAsync(path);
                allModules.AddRange(modules);
            }
        }

        return allModules;
    }

    public async Task<IEnumerable<SapAddOnInfo>> ScanFolderAsync(string folderPath)
    {
        var modules = new List<SapAddOnInfo>();

        await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogDebug("Folder does not exist: {Path}", folderPath);
                    return;
                }

                var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);
                
                _logger.LogInformation("Found {Count} DLL files in {Path}", dllFiles.Length, folderPath);

                foreach (var dllFile in dllFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(dllFile);
                        var versionInfo = FileVersionInfo.GetVersionInfo(dllFile);

                        var module = new SapAddOnInfo
                        {
                            AddOnId = Path.GetFileNameWithoutExtension(dllFile),
                            Name = !string.IsNullOrEmpty(versionInfo.ProductName) 
                                ? versionInfo.ProductName 
                                : Path.GetFileNameWithoutExtension(dllFile),
                            Version = !string.IsNullOrEmpty(versionInfo.FileVersion)
                                ? versionInfo.FileVersion
                                : "1.0.0.0",
                            Description = !string.IsNullOrEmpty(versionInfo.FileDescription)
                                ? versionInfo.FileDescription
                                : !string.IsNullOrEmpty(versionInfo.Comments)
                                    ? versionInfo.Comments
                                    : null,
                            DatabaseName = folderPath,
                            Status = AddOnStatus.Installed
                        };

                        if (IsLikelyCorsuiteModule(module.Name, dllFile))
                        {
                            modules.Add(module);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not read version info for: {File}", dllFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning folder: {Path}", folderPath);
            }
        });

        return modules;
    }

    private bool IsLikelyCorsuiteModule(string moduleName, string filePath)
    {
        var nameLower = moduleName.ToLowerInvariant();
        var fileLower = filePath.ToLowerInvariant();

        string[] corsuitePatterns = 
        {
            "corsuite", "core suite", "cor suite",
            "cs_", "cs", "csuite",
            "cor_", "coreaddon"
        };

        foreach (var pattern in corsuitePatterns)
        {
            if (nameLower.Contains(pattern) || fileLower.Contains(pattern))
            {
                return true;
            }
        }

        string[] excludePatterns = 
        {
            "mscorlib", "system", "microsoft", "netstandard", 
            "Newtonsoft", "log4net", "nlog", "unity"
        };

        foreach (var pattern in excludePatterns)
        {
            if (nameLower.StartsWith(pattern))
            {
                return false;
            }
        }

        return true;
    }
}
