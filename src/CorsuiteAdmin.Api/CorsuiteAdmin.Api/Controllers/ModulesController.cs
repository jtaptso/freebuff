using Microsoft.AspNetCore.Mvc;
using CorsuiteAdmin.Api.Services;
using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;
    private readonly ILogger<ModulesController> _logger;

    public ModulesController(IModuleService moduleService, ILogger<ModulesController> logger)
    {
        _moduleService = moduleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DllModuleDto>>> GetAllModules()
    {
        var modules = await _moduleService.GetAllModulesAsync();
        return Ok(modules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DllModuleDto>> GetModule(Guid id)
    {
        var module = await _moduleService.GetModuleByIdAsync(id);
        if (module == null)
            return NotFound(new { message = "Module not found" });
        
        return Ok(module);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DllModuleDto>>> SearchModules([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });
        
        var modules = await _moduleService.SearchModulesAsync(q);
        return Ok(modules);
    }

    [HttpPost]
    public async Task<ActionResult<DllModuleDto>> CreateModule([FromForm] CreateModuleRequestDto request, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "DLL file is required" });

        if (!file.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .dll files are allowed" });

        try
        {
            var module = await _moduleService.CreateModuleAsync(request, file);
            return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating module");
            return StatusCode(500, new { message = "Error creating module", error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DllModuleDto>> UpdateModule(Guid id, [FromBody] UpdateModuleRequestDto request)
    {
        try
        {
            var module = await _moduleService.UpdateModuleAsync(id, request);
            if (module == null)
                return NotFound(new { message = "Module not found" });
            
            return Ok(module);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module");
            return StatusCode(500, new { message = "Error updating module", error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/file")]
    public async Task<ActionResult<DllModuleDto>> UpdateModuleFile(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "DLL file is required" });

        if (!file.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .dll files are allowed" });

        try
        {
            var module = await _moduleService.UpdateModuleWithFileAsync(id, file);
            if (module == null)
                return NotFound(new { message = "Module not found" });
            
            return Ok(module);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module file");
            return StatusCode(500, new { message = "Error updating module file", error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteModule(Guid id)
    {
        try
        {
            var result = await _moduleService.DeleteModuleAsync(id);
            if (!result)
                return NotFound(new { message = "Module not found" });
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting module");
            return StatusCode(500, new { message = "Error deleting module", error = ex.Message });
        }
    }

    [HttpGet("folder")]
    public async Task<ActionResult<string>> GetModulesFolder()
    {
        var folder = await _moduleService.GetModulesFolderAsync();
        return Ok(new { path = folder });
    }
}
