using Microsoft.AspNetCore.Mvc;
using CorsuiteAdmin.Api.Services.SAP;
using CorsuiteAdmin.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SapController : ControllerBase
{
    private readonly ISapConnectionService _sapConnection;
    private readonly ISapSqlQueryService _sqlQuery;
    private readonly ICorsuiteFileScannerService _fileScanner;
    private readonly ILogger<SapController> _logger;

    public SapController(
        ISapConnectionService sapConnection,
        ISapSqlQueryService sqlQuery,
        ICorsuiteFileScannerService fileScanner,
        ILogger<SapController> logger)
    {
        _sapConnection = sapConnection;
        _sqlQuery = sqlQuery;
        _fileScanner = fileScanner;
        _logger = logger;
    }

    [HttpPost("connect")]
    public async Task<ActionResult<bool>> Connect([FromBody] SapConnectionDto connection)
    {
        try
        {
            var connectionInfo = new CorsuiteAdmin.Api.Services.SAP.ConnectionInfo
            {
                Server = connection.Server,
                CompanyDB = connection.CompanyDB,
                UserName = connection.UserName,
                Password = connection.Password,
                DbUserName = connection.DbUserName,
                DbPassword = connection.DbPassword,
                DbType = connection.DbType,
                LicenseServer = connection.LicenseServer
            };

            var result = await _sapConnection.ConnectAsync(connectionInfo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to SAP B1");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("disconnect")]
    public async Task<ActionResult> Disconnect()
    {
        try
        {
            await _sapConnection.DisconnectAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from SAP B1");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("status")]
    public ActionResult GetConnectionStatus()
    {
        return Ok(new
        {
            IsConnected = _sapConnection.IsConnected,
            CompanyName = _sapConnection.CompanyName,
            DatabaseName = _sapConnection.DatabaseName
        });
    }

    [HttpGet("addons")]
    public async Task<ActionResult<IEnumerable<SapAddOnInfoDto>>> GetInstalledAddons()
    {
        try
        {
            if (!_sapConnection.IsConnected)
            {
                return BadRequest(new { message = "Not connected to SAP B1" });
            }

            var addons = await _sapConnection.GetInstalledAddonsAsync();
            var dtos = addons.Select(a => new SapAddOnInfoDto
            {
                AddOnId = a.AddOnId,
                Name = a.Name,
                Version = a.Version,
                Description = a.Description,
                DatabaseName = a.DatabaseName,
                InstallationDate = a.InstallationDate,
                Status = a.Status.ToString()
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting installed add-ons");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("validate")]
    public async Task<ActionResult<SqlConnectionValidationResult>> ValidateConnection(
        [FromBody] string connectionString)
    {
        try
        {
            var result = await _sqlQuery.ValidateConnectionAsync(connectionString);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating connection");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("query-addons")]
    public async Task<ActionResult<IEnumerable<SapAddOnInfoDto>>> QueryAddonsFromDatabase(
        [FromBody] string connectionString)
    {
        try
        {
            var addons = await _sqlQuery.QueryAddonsFromDatabaseAsync(connectionString);
            var dtos = addons.Select(a => new SapAddOnInfoDto
            {
                AddOnId = a.AddOnId,
                Name = a.Name,
                Version = a.Version,
                Description = a.Description,
                DatabaseName = a.DatabaseName,
                InstallationDate = a.InstallationDate,
                Status = a.Status.ToString()
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying add-ons from database");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("query-corsuite")]
    public async Task<ActionResult<IEnumerable<SapAddOnInfoDto>>> QueryCorsuiteModules(
        [FromBody] QueryCorsuiteRequest request)
    {
        try
        {
            var modules = await _sqlQuery.QueryCorsuiteModulesAsync(
                request.ConnectionString, request.CorsuiteDbName);
            
            var dtos = modules.Select(m => new SapAddOnInfoDto
            {
                AddOnId = m.AddOnId,
                Name = m.Name,
                Version = m.Version,
                Description = m.Description,
                DatabaseName = m.DatabaseName,
                InstallationDate = m.InstallationDate,
                Status = m.Status.ToString()
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Corsuite modules");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("scan-files")]
    public async Task<ActionResult<IEnumerable<SapAddOnInfoDto>>> ScanCorsuiteFiles()
    {
        try
        {
            var modules = await _fileScanner.ScanCorsuiteFoldersAsync();
            var dtos = modules.Select(m => new SapAddOnInfoDto
            {
                AddOnId = m.AddOnId,
                Name = m.Name,
                Version = m.Version,
                Description = m.Description,
                DatabaseName = m.DatabaseName,
                InstallationDate = null,
                Status = m.Status.ToString()
            });

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Corsuite files");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("possible-paths")]
    public ActionResult<IEnumerable<string>> GetPossibleCorsuitePaths()
    {
        return Ok(_fileScanner.GetPossibleCorsuitePaths().ToList());
    }
}

public class QueryCorsuiteRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string CorsuiteDbName { get; set; } = string.Empty;
}
