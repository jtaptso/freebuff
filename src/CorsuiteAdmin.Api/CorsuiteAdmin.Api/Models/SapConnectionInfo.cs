namespace CorsuiteAdmin.Api.Models;

public class SapConnectionInfo
{
    public string Server { get; set; } = string.Empty;
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DbUserName { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public string DbType { get; set; } = "dst_MSSQL2022";
    public string? LicenseServer { get; set; }
}
