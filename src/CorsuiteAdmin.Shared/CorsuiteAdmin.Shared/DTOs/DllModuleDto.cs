namespace CorsuiteAdmin.Shared.DTOs;

public class DllModuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public ModuleStatus Status { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string AddedBy { get; set; } = string.Empty;
}

public enum ModuleStatus
{
    Active,
    Inactive,
    Error
}
