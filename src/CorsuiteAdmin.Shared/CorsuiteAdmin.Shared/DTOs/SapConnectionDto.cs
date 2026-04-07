namespace CorsuiteAdmin.Shared.DTOs;

public class SapConnectionDto
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

public class SapAddOnInfoDto
{
    public string AddOnId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DatabaseName { get; set; }
    public DateTime? InstallationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SyncResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ModulesFound { get; set; }
    public int ModulesSynced { get; set; }
    public List<SapAddOnInfoDto> Modules { get; set; } = new();
}
