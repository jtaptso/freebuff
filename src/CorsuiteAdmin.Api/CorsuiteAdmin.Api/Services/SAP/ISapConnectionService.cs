using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Api.Services.SAP;

public interface ISapConnectionService
{
    Task<bool> ConnectAsync(ConnectionInfo connectionInfo);
    Task DisconnectAsync();
    bool IsConnected { get; }
    string? CompanyName { get; }
    string? DatabaseName { get; }
    Task<IEnumerable<SapAddOnInfo>> GetInstalledAddonsAsync();
}

public class ConnectionInfo
{
    public string Server { get; set; } = string.Empty;
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DbUserName { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public string DbType { get; set; } = "dst_MSSQL2022";
    public string? LicenseServer { get; set; }
    
    // DI Server (Remote) connection properties
    public string? DiServerUrl { get; set; }
    public string? Language { get; set; }
    
    // Connection type indicator
    public bool UseDiServer { get; set; }
}

public class SapAddOnInfo
{
    public string AddOnId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? InstallationDate { get; set; }
    public AddOnStatus Status { get; set; }
}

public enum AddOnStatus
{
    Unknown,
    Registered,
    Installed,
    Active,
    Inactive
}
