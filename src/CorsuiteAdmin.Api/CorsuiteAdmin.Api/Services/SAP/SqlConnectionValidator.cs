using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Services.SAP;

/// <summary>
/// Result of SQL connection validation
/// </summary>
public class SqlConnectionValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ServerVersion { get; set; }
    public string? DatabaseName { get; set; }
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Service to validate SQL connection strings before use.
/// Provides security checks and connectivity verification.
/// </summary>
public interface ISqlConnectionValidator
{
    /// <summary>
    /// Validates a SQL connection string format and tests connectivity.
    /// </summary>
    Task<SqlConnectionValidationResult> ValidateAsync(
        string connectionString, 
        bool testConnect = true, 
        int timeoutSeconds = 10);

    /// <summary>
    /// Validates connection string format only (no connectivity test).
    /// </summary>
    Task<SqlConnectionValidationResult> ValidateFormatOnlyAsync(string connectionString);
}

public class SqlConnectionValidator : ISqlConnectionValidator
{
    private readonly ILogger<SqlConnectionValidator> _logger;

    public SqlConnectionValidator(ILogger<SqlConnectionValidator> logger)
    {
        _logger = logger;
    }

    public async Task<SqlConnectionValidationResult> ValidateAsync(
        string connectionString, 
        bool testConnect = true, 
        int timeoutSeconds = 10)
    {
        // First validate format
        var formatResult = await ValidateFormatOnlyAsync(connectionString);
        if (!formatResult.IsValid)
        {
            return formatResult;
        }

        // Test connectivity if requested
        if (testConnect)
        {
            return await TestConnectionAsync(connectionString, timeoutSeconds);
        }

        return new SqlConnectionValidationResult 
        { 
            IsValid = true, 
            TimeoutSeconds = timeoutSeconds,
            DatabaseName = formatResult.DatabaseName
        };
    }

    public Task<SqlConnectionValidationResult> ValidateFormatOnlyAsync(string connectionString)
    {
        var result = new SqlConnectionValidationResult();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result.IsValid = false;
            result.ErrorMessage = "Connection string cannot be empty or whitespace";
            return Task.FromResult(result);
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            // Ensure minimum required fields
            if (string.IsNullOrWhiteSpace(builder.DataSource))
            {
                result.IsValid = false;
                result.ErrorMessage = "Connection string must specify a Data Source (Server)";
                return Task.FromResult(result);
            }

            // Check for potentially dangerous patterns
            if (connectionString.Contains("AttachDbFilename=", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Connection string contains AttachDbFilename which may be a security concern");
            }

            result.DatabaseName = builder.InitialCatalog;
            result.IsValid = true;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Invalid connection string format: {ex.Message}";
            _logger.LogWarning(ex, "Invalid connection string format");
        }

        return Task.FromResult(result);
    }

    private async Task<SqlConnectionValidationResult> TestConnectionAsync(
        string connectionString, 
        int timeoutSeconds)
    {
        var result = new SqlConnectionValidationResult { TimeoutSeconds = timeoutSeconds };

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = timeoutSeconds
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            result.IsValid = true;
            result.ServerVersion = connection.ServerVersion;
            result.DatabaseName = builder.InitialCatalog;

            _logger.LogInformation("SQL connection validated. Server: {Server}, DB: {Database}", 
                builder.DataSource, builder.InitialCatalog);
        }
        catch (SqlException ex)
        {
            result.IsValid = false;
            result.ErrorMessage = ex.Number switch
            {
                18456 => "Login failed. Invalid username or password.",
                4060 => "Database does not exist or access denied.",
                17 => "SQL Server not found. Check server name and network connectivity.",
                -1 => "Connection timeout. Server may be unreachable or firewall blocking.",
                _ => $"Connection failed: {ex.Message}"
            };

            _logger.LogWarning(ex, "SQL connection validation failed");
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during SQL connection validation");
        }

        return result;
    }
}
