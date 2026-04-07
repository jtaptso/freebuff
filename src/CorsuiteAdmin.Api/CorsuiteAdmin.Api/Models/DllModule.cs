using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Api.Models;

public class DllModule
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

    public DllModuleDto ToDto() => new DllModuleDto
    {
        Id = Id,
        Name = Name,
        Version = Version,
        FilePath = FilePath,
        FileSize = FileSize,
        Description = Description,
        Status = Status,
        AddedDate = AddedDate,
        ModifiedDate = ModifiedDate,
        AddedBy = AddedBy
    };

    public static DllModule FromDto(DllModuleDto dto) => new DllModule
    {
        Id = dto.Id,
        Name = dto.Name,
        Version = dto.Version,
        FilePath = dto.FilePath,
        FileSize = dto.FileSize,
        Description = dto.Description,
        Status = dto.Status,
        AddedDate = dto.AddedDate,
        ModifiedDate = dto.ModifiedDate,
        AddedBy = dto.AddedBy
    };
}
