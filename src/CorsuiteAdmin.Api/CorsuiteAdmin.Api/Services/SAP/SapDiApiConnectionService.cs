using SAPbobsCOM;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Services.SAP;

/// <summary>
/// SAP Business One DI API connection service using COM interop.
/// Requires SAP Business One DI API to be installed on the server.
/// </summary>
public class SapDiApiConnectionService : ISapConnectionService, IDisposable
{
    private readonly ILogger<SapDiApiConnectionService> _logger;
    private Company? _company;
    private bool _isConnected;
    private string? _companyName;
    private string? _databaseName;

    public SapDiApiConnectionService(ILogger<SapDiApiConnectionService> logger)
    {
        _logger = logger;
    }

    public bool IsConnected => _isConnected;
    public string? CompanyName => _companyName;
    public string? DatabaseName => _databaseName;

    /// <summary>
    /// Creates a new SAP Company instance. Protected virtual for testing.
    /// </summary>
    protected virtual Company CreateCompany()
    {
        return new Company();
    }

    public async Task<bool> ConnectAsync(ConnectionInfo connectionInfo)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Connecting to SAP B1: Server={Server}, CompanyDB={CompanyDB}", 
                    connectionInfo.Server, connectionInfo.CompanyDB);

                // Create Company object using SAPbobsCOM
                _company = CreateCompany();

                _company.Server = connectionInfo.Server;
                _company.CompanyDB = connectionInfo.CompanyDB;
                _company.UserName = connectionInfo.UserName;
                _company.Password = connectionInfo.Password;
                _company.DbUserName = connectionInfo.DbUserName ?? "sa";
                _company.DbPassword = connectionInfo.DbPassword ?? "";
                
                // Use integer values for database server type
                _company.DbServerType = (BoDataServerTypes)GetDbServerType(connectionInfo.DbType);
                
                if (!string.IsNullOrEmpty(connectionInfo.LicenseServer))
                {
                    _company.LicenseServer = connectionInfo.LicenseServer;
                }

                int result = _company.Connect();
                
                if (result != 0)
                {
                    string errorDesc = _company.GetLastErrorDescription();
                    _logger.LogError("SAP B1 Connection failed: {Error}", errorDesc);
                    _company = null;
                    return false;
                }

                _companyName = _company.CompanyName;
                _databaseName = _company.CompanyDB;
                _isConnected = true;

                _logger.LogInformation("Successfully connected to SAP B1 company: {CompanyName}", _companyName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SAP B1 - DI API may not be installed");
                return false;
            }
        });
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            if (_company != null && _isConnected)
            {
                try
                {
                    _company.Disconnect();
                    _logger.LogInformation("Disconnected from SAP B1");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from SAP B1");
                }
                finally
                {
                    _isConnected = false;
                    _companyName = null;
                    _databaseName = null;
                }
            }
        });
    }

    public async Task<IEnumerable<SapAddOnInfo>> GetInstalledAddonsAsync()
    {
        var addons = new List<SapAddOnInfo>();

        if (!_isConnected || _company == null)
        {
            _logger.LogWarning("Not connected to SAP B1");
            return addons;
        }

        return await Task.Run(() =>
        {
            try
            {
                // Query SARI table - User Defined Table for Add-on Registration
                var recordSet = (Recordset)_company.GetBusinessObject(BoObjectTypes.oUserTables);
                
                if (recordSet == null)
                {
                    _logger.LogWarning("Could not create UserTables recordset");
                    return addons;
                }

                string query = @"SELECT U_AddOnID, U_Name, U_Version, U_Description, U_Company, U_Status, U_InstallationDate 
                                  FROM SARI 
                                  ORDER BY U_Name";
                
                recordSet.DoQuery(query);

                int count = 0;
                while (!recordSet.EoF)
                {
                    try
                    {
                        var addon = new SapAddOnInfo
                        {
                            AddOnId = GetFieldValue(recordSet, "U_AddOnID"),
                            Name = GetFieldValue(recordSet, "U_Name"),
                            Version = GetFieldValue(recordSet, "U_Version"),
                            Description = GetFieldValue(recordSet, "U_Description"),
                            DatabaseName = GetFieldValue(recordSet, "U_Company"),
                            Status = ParseStatus(GetFieldValue(recordSet, "U_Status"))
                        };
                        
                        try
                        {
                            var dateStr = GetFieldValue(recordSet, "U_InstallationDate");
                            if (DateTime.TryParse(dateStr, out var installDate))
                            {
                                addon.InstallationDate = installDate;
                            }
                        }
                        catch { }

                        addons.Add(addon);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading add-on record {Count}", count);
                    }
                    
                    recordSet.MoveNext();
                }

                _logger.LogInformation("Found {Count} add-ons from SARI table", count);
                Marshal.ReleaseComObject(recordSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying add-ons from SAP B1");
            }

            return addons;
        });
    }

    protected virtual string GetFieldValue(Recordset rs, string fieldName)
    {
        try
        {
            if (rs.Fields.Count > 0)
            {
                var field = rs.Fields.Item(fieldName);
                return field.Value?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Field {FieldName} not found", fieldName);
        }
        return string.Empty;
    }

    /// <summary>
    /// Maps database type string to integer value. Protected virtual for testing.
    /// </summary>
    protected virtual int GetDbServerType(string dbType)
    {
        return dbType switch
        {
            "dst_MSSQL2022" => 5,
            "dst_MSSQL2019" => 4,
            "dst_MSSQL2017" => 3,
            "dst_MSSQL2016" => 2,
            "dst_MSSQL2014" => 1,
            "dst_HANADB" => 7,
            "dst_MSSQL2012" => 0,
            _ => 0
        };
    }

    /// <summary>
    /// Parses status string to AddOnStatus enum. Protected virtual for testing.
    /// </summary>
    protected virtual AddOnStatus ParseStatus(string? status)
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

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        if (_company != null)
        {
            Marshal.ReleaseComObject(_company);
            _company = null;
        }
    }
}
