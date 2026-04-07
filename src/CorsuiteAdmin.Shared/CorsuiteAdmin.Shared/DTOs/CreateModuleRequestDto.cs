namespace CorsuiteAdmin.Shared.DTOs;

public class CreateModuleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AddedBy { get; set; } = string.Empty;
}
