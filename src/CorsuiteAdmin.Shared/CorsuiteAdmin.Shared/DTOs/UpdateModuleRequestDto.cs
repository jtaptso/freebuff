namespace CorsuiteAdmin.Shared.DTOs;

public class UpdateModuleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ModuleStatus Status { get; set; }
}
