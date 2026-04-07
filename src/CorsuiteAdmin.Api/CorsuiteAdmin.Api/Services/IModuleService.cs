using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Api.Services;

public interface IModuleService
{
    Task<IEnumerable<DllModuleDto>> GetAllModulesAsync();
    Task<DllModuleDto?> GetModuleByIdAsync(Guid id);
    Task<DllModuleDto> CreateModuleAsync(CreateModuleRequestDto request, IFormFile file);
    Task<DllModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleRequestDto request);
    Task<DllModuleDto?> UpdateModuleWithFileAsync(Guid id, IFormFile file);
    Task<bool> DeleteModuleAsync(Guid id);
    Task<IEnumerable<DllModuleDto>> SearchModulesAsync(string query);
    Task<string> GetModulesFolderAsync();
}
