using Microsoft.EntityFrameworkCore;
using CorsuiteAdmin.Api.Data;
using CorsuiteAdmin.Api.Models;
using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Api.Services;

public class ModuleService : IModuleService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly string _modulesFolder;

    public ModuleService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
        _modulesFolder = Path.Combine(_environment.ContentRootPath, "Modules");
        
        if (!Directory.Exists(_modulesFolder))
        {
            Directory.CreateDirectory(_modulesFolder);
        }
    }

    public async Task<IEnumerable<DllModuleDto>> GetAllModulesAsync()
    {
        var modules = await _context.Modules
            .OrderByDescending(m => m.AddedDate)
            .ToListAsync();
        
        return modules.Select(m => m.ToDto());
    }

    public async Task<DllModuleDto?> GetModuleByIdAsync(Guid id)
    {
        var module = await _context.Modules.FindAsync(id);
        return module?.ToDto();
    }

    public async Task<DllModuleDto> CreateModuleAsync(CreateModuleRequestDto request, IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_modulesFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var module = new DllModule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Version = request.Version,
            FilePath = filePath,
            FileSize = file.Length,
            Description = request.Description,
            Status = ModuleStatus.Active,
            AddedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            AddedBy = request.AddedBy
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        return module.ToDto();
    }

    public async Task<DllModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleRequestDto request)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null) return null;

        module.Name = request.Name;
        module.Version = request.Version;
        module.Description = request.Description;
        module.Status = request.Status;
        module.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return module.ToDto();
    }

    public async Task<DllModuleDto?> UpdateModuleWithFileAsync(Guid id, IFormFile file)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null) return null;

        // Delete old file
        if (File.Exists(module.FilePath))
        {
            File.Delete(module.FilePath);
        }

        // Save new file
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(_modulesFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        module.FilePath = filePath;
        module.FileSize = file.Length;
        module.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return module.ToDto();
    }

    public async Task<bool> DeleteModuleAsync(Guid id)
    {
        var module = await _context.Modules.FindAsync(id);
        if (module == null) return false;

        // Delete file
        if (File.Exists(module.FilePath))
        {
            File.Delete(module.FilePath);
        }

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<DllModuleDto>> SearchModulesAsync(string query)
    {
        var modules = await _context.Modules
            .Where(m => m.Name.Contains(query) || m.Version.Contains(query))
            .OrderByDescending(m => m.AddedDate)
            .ToListAsync();

        return modules.Select(m => m.ToDto());
    }

    public Task<string> GetModulesFolderAsync()
    {
        return Task.FromResult(_modulesFolder);
    }
}
