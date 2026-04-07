using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace CorsuiteAdmin.Api.Services.SAP;

/// <summary>
/// Health check that verifies SAP Business One DI API is available on the server.
/// This checks if the COM component can be instantiated, not if it's connected to a company.
/// </summary>
public class SapDiApiHealthCheck : IHealthCheck
{
    private readonly ILogger<SapDiApiHealthCheck> _logger;

    public SapDiApiHealthCheck(ILogger<SapDiApiHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running SAP B1 DI API health check...");

            // Try to create a Company object to verify DI API is installed
            // This doesn't connect to any company, just verifies the COM component is available
            var company = new Company();
            
            // If we get here, the DI API COM component is available
            _logger.LogInformation("SAP B1 DI API is available and ready");
            
            // Release the COM object immediately since we don't need it
            System.Runtime.InteropServices.Marshal.ReleaseComObject(company);

            return Task.FromResult(HealthCheckResult.Healthy(
                "SAP B1 DI API is installed and available"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP B1 DI API health check failed - DI API may not be installed");
            
            // Provide detailed failure information
            var failureMessage = "SAP B1 DI API is not available. " +
                "Please ensure SAP Business One DI API is installed on this server. " +
                $"Error: {ex.Message}";

            return Task.FromResult(HealthCheckResult.Unhealthy(
                failureMessage,
                ex));
        }
    }
}

/// <summary>
/// Startup health check that runs once at application startup to verify SAP B1 DI API availability.
/// Logs a warning if DI API is not installed but doesn't block application startup.
/// </summary>
public class SapDiApiStartupCheck
{
    private readonly ILogger<SapDiApiStartupCheck> _logger;

    public SapDiApiStartupCheck(ILogger<SapDiApiStartupCheck> logger)
    {
        _logger = logger;
    }

    public void Check()
    {
        try
        {
            _logger.LogInformation("Checking SAP B1 DI API availability at startup...");
            
            // Try to create Company object
            var company = new Company();
            
            _logger.LogInformation("✅ SAP B1 DI API is available - COM component ready");
            
            // Release immediately
            System.Runtime.InteropServices.Marshal.ReleaseComObject(company);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "⚠️ SAP B1 DI API is NOT available. DI API connection method will not work. " +
                "Error: {ErrorMessage}. " +
                "To enable DI API connections, install SAP Business One DI API on this server.",
                ex.Message);
        }
    }
}
