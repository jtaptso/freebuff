using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using CorsuiteAdmin.Shared.DTOs;

namespace CorsuiteAdmin.Web.Services;

public class ModuleService
{
    private readonly HttpClient _http;
    private readonly ILogger<ModuleService> _logger;

    public ModuleService(HttpClient http, ILogger<ModuleService> logger)
    {
        _http = http;
        _logger = logger;
    }

    // ==================== Module CRUD Operations ====================
    
    public async Task<IEnumerable<DllModuleDto>> GetAllModulesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<DllModuleDto>>("api/modules") 
                   ?? Enumerable.Empty<DllModuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching modules");
            return Enumerable.Empty<DllModuleDto>();
        }
    }

    public async Task<DllModuleDto?> GetModuleAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<DllModuleDto>($"api/modules/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching module {ModuleId}", id);
            return null;
        }
    }

    public async Task<DllModuleDto?> CreateModuleAsync(CreateModuleRequestDto request, IBrowserFile? file = null)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(request.Name), "Name");
            content.Add(new StringContent(request.Version), "Version");
            content.Add(new StringContent(request.AddedBy), "AddedBy");
            
            if (!string.IsNullOrEmpty(request.Description))
            {
                content.Add(new StringContent(request.Description), "Description");
            }

            if (file != null)
            {
                var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "File", file.Name);
            }

            var response = await _http.PostAsync("api/modules", content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DllModuleDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating module");
            return null;
        }
    }

    public async Task<DllModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleRequestDto request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/modules/{id}", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DllModuleDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module {ModuleId}", id);
            return null;
        }
    }

    public async Task<bool> UpdateModuleWithFileAsync(Guid id, IBrowserFile file)
    {
        try
        {
            var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "File", file.Name);

            var response = await _http.PutAsync($"api/modules/{id}/file", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module file {ModuleId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteModuleAsync(Guid id)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/modules/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting module {ModuleId}", id);
            return false;
        }
    }

    public async Task<IEnumerable<DllModuleDto>> SearchModulesAsync(string query)
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<DllModuleDto>>($"api/modules/search?q={query}") 
                   ?? Enumerable.Empty<DllModuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching modules");
            return Enumerable.Empty<DllModuleDto>();
        }
    }

    public async Task<string> GetModulesFolderAsync()
    {
        try
        {
            return await _http.GetStringAsync("api/modules/folder");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting modules folder");
            return string.Empty;
        }
    }

    // ==================== SAP Integration Methods ====================

    public async Task<bool> ConnectToSapAsync(SapConnectionDto connection)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/sap/connect", connection);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to SAP B1");
            return false;
        }
    }

    public async Task DisconnectFromSapAsync()
    {
        try
        {
            await _http.PostAsync("api/sap/disconnect", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from SAP B1");
        }
    }

    public async Task<SapConnectionStatus?> GetSapConnectionStatusAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<SapConnectionStatus>("api/sap/status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SAP connection status");
            return null;
        }
    }

    public async Task<IEnumerable<SapAddOnInfoDto>> GetSapAddonsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<SapAddOnInfoDto>>("api/sap/addons") 
                   ?? Enumerable.Empty<SapAddOnInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SAP add-ons");
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
    }

    public async Task<IEnumerable<SapAddOnInfoDto>> QueryAddonsFromDatabaseAsync(string connectionString)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/sap/query-addons", connectionString);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<SapAddOnInfoDto>>() 
                       ?? Enumerable.Empty<SapAddOnInfoDto>();
            }
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying add-ons from database");
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
    }

    public async Task<IEnumerable<SapAddOnInfoDto>> QueryCorsuiteModulesAsync(string connectionString, string dbName)
    {
        try
        {
            var request = new { ConnectionString = connectionString, CorsuiteDbName = dbName };
            var response = await _http.PostAsJsonAsync("api/sap/query-corsuite", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<SapAddOnInfoDto>>() 
                       ?? Enumerable.Empty<SapAddOnInfoDto>();
            }
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Corsuite modules");
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
    }

    public async Task<IEnumerable<SapAddOnInfoDto>> ScanCorsuiteFilesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<SapAddOnInfoDto>>("api/sap/scan-files") 
                   ?? Enumerable.Empty<SapAddOnInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Corsuite files");
            return Enumerable.Empty<SapAddOnInfoDto>();
        }
    }

    public async Task<IEnumerable<string>> GetPossibleCorsuitePathsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<string>>("api/sap/possible-paths") 
                   ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting possible Corsuite paths");
            return Enumerable.Empty<string>();
        }
    }
}

public class SapConnectionStatus
{
    public bool IsConnected { get; set; }
    public string? CompanyName { get; set; }
    public string? DatabaseName { get; set; }
}
