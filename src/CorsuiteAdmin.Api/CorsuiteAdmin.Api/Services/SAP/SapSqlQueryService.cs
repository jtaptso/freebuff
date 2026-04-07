using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Services.SAP;

public interface ISapSqlQueryService
{
    Task<IEnumerable<SapAddOnInfo>> QueryAddonsFromDatabaseAsync(string connectionString);
    Task<IEnumerable<SapAddOnInfo>> QueryCorsuiteModulesAsync(string connectionString, string corsuiteDbName);
    Task<SqlConnectionValidationResult> ValidateConnectionAsync(string connectionString);
}

public class SapSqlQueryService : ISapSqlQueryService
{
    private readonly ILogger<SapSqlQueryService> _logger;
    private readonly ISqlConnectionValidator _connectionValidator;
    private const int DefaultQueryTimeout = 30;

    public SapSqlQueryService(
        ILogger<SapSqlQueryService> logger,
        ISqlConnectionValidator connectionValidator)
    {
        _logger = logger;
        _connectionValidator = connectionValidator;
    }

    public async Task<SqlConnectionValidationResult> ValidateConnectionAsync(string connectionString)
    {
        return await _connectionValidator.ValidateAsync(connectionString, testConnect: true, timeoutSeconds: 15);
    }

    public async Task<IEnumerable<SapAddOnInfo>> QueryAddonsFromDatabaseAsync(string connectionString)
    {
        var addons = new List<SapAddOnInfo>();

        var validation = await _connectionValidator.ValidateAsync(connectionString, testConnect: true, timeoutSeconds: 15);
        if (!validation.IsValid)
        {
            _logger.LogError("SQL connection validation failed: {Error}", validation.ErrorMessage);
            return addons;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                SELECT 
                    U_AddOnID AS AddOnId,
                    U_Name AS Name,
                    U_Version AS Version,
                    U_Description AS Description,
                    U_Company AS DatabaseName,
                    U_InstallDate AS InstallationDate,
                    U_Status AS Status
                FROM SARI WITH (NOLOCK)
                ORDER BY U_Name";

            await using var command = new SqlCommand(query, connection)
            {
                CommandTimeout = DefaultQueryTimeout
            };
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                addons.Add(new SapAddOnInfo
                {
                    AddOnId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Version = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                    DatabaseName = reader.IsDBNull(4) ? null : reader.GetString(4),
                    InstallationDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Status = ParseStatus(reader.IsDBNull(6) ? null : reader.GetString(6))
                });
            }

            _logger.LogInformation("Found {Count} add-ons from SARI table", addons.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying add-ons from SAP B1 database");
        }

        return addons;
    }

    public async Task<IEnumerable<SapAddOnInfo>> QueryCorsuiteModulesAsync(string connectionString, string? corsuiteDbName)
    {
        var modules = new List<SapAddOnInfo>();

        // Validate dbName FIRST before any connection validation
        if (!string.IsNullOrWhiteSpace(corsuiteDbName) && !IsValidDatabaseName(corsuiteDbName))
        {
            _logger.LogError("Invalid Corsuite database name: {DbName}", corsuiteDbName);
            return modules;
        }

        // Now validate the connection
        var validation = await _connectionValidator.ValidateAsync(connectionString, testConnect: true, timeoutSeconds: 15);
        if (!validation.IsValid)
        {
            _logger.LogError("SQL connection validation failed: {Error}", validation.ErrorMessage);
            return modules;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrWhiteSpace(corsuiteDbName))
            {
                builder.InitialCatalog = corsuiteDbName;
            }

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            string[] possibleTables = 
            {
                "[@COR_SUITE_MODULES]",
                "[@CS_MODULES]",
                "[@SWA_MODULES]",
                "[@CORSUITE_MODULES]",
                "[dbo].[@COR_MODULE]",
                "[dbo].[CS_AddOns]"
            };

            foreach (var table in possibleTables)
            {
                try
                {
                    if (!IsValidTableName(table))
                    {
                        _logger.LogWarning("Skipping potentially unsafe table name: {Table}", table);
                        continue;
                    }

                    string query = $"SELECT Code, Name, U_Version, U_Description FROM {table} WITH (NOLOCK)";
                    await using var command = new SqlCommand(query, connection)
                    {
                        CommandTimeout = DefaultQueryTimeout
                    };
                    await using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        modules.Add(new SapAddOnInfo
                        {
                            AddOnId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                            Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Version = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            DatabaseName = corsuiteDbName ?? string.Empty,
                            Status = AddOnStatus.Registered
                        });
                    }

                    if (modules.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} Corsuite modules from {Table}", modules.Count, table);
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (modules.Count == 0)
            {
                _logger.LogWarning("No Corsuite module tables found in database {DbName}", corsuiteDbName ?? "default");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Corsuite modules from database");
        }

        return modules;
    }

    private bool IsValidDatabaseName(string dbName)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(dbName, @"^[a-zA-Z0-9_\-]+$");
    }

    private bool IsValidTableName(string tableName)
    {
        if (string.IsNullOrEmpty(tableName)) return false;
        
        return System.Text.RegularExpressions.Regex.IsMatch(
            tableName, 
            @"^(\[\w+\]\.\[\w+\]|\[\w+\])$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private AddOnStatus ParseStatus(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AddOnStatus.Active,
            "INSTALLED" => AddOnStatus.Installed,
            "REGISTERED" => AddOnStatus.Registered,
            "INACTIVE" => AddOnStatus.Inactive,
            _ => AddOnStatus.Unknown
        };
    }
}
