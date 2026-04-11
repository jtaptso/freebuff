using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace CorsuiteAdmin.Api.Services.SAP;

/// <summary>
/// SAP Business One DI Server connection service using SOAP over HTTP.
/// Connects to a remote DI Server instead of using local COM (SAPbobsCOM).
/// Requires DI Server to be running and accessible over the network.
/// </summary>
public class SapDiServerConnectionService : ISapConnectionService, IDisposable
{
    private readonly ILogger<SapDiServerConnectionService> _logger;
    private readonly HttpClient _httpClient;
    private string? _sessionId;
    private string? _companyName;
    private string? _databaseName;
    private string? _serverUrl;
    private bool _isConnected;
    private readonly object _lock = new();

    public SapDiServerConnectionService(ILogger<SapDiServerConnectionService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _isConnected;
            }
        }
    }

    public string? CompanyName
    {
        get
        {
            lock (_lock)
            {
                return _companyName;
            }
        }
    }

    public string? DatabaseName
    {
        get
        {
            lock (_lock)
            {
                return _databaseName;
            }
        }
    }

    public async Task<bool> ConnectAsync(ConnectionInfo connectionInfo)
    {
        // Determine if using remote DI Server or local COM
        if (string.IsNullOrEmpty(connectionInfo.DiServerUrl))
        {
            _logger.LogWarning("No DI Server URL provided. Use SapDiApiConnectionService for local COM.");
            return false;
        }

        try
        {
            _serverUrl = connectionInfo.DiServerUrl;
            _logger.LogInformation("Connecting to SAP B1 DI Server: {Url}, CompanyDB={CompanyDB}",
                _serverUrl, connectionInfo.CompanyDB);

            // Build SOAP login request
            var soapRequest = BuildLoginSoapRequest(
                connectionInfo.CompanyDB,
                connectionInfo.UserName,
                connectionInfo.Password,
                connectionInfo.Language ?? "en"
            );

            var response = await _httpClient.PostAsync(_serverUrl, soapRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DI Server login failed with status: {Status}", response.StatusCode);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _sessionId = ParseLoginResponse(responseContent);

            if (string.IsNullOrEmpty(_sessionId))
            {
                _logger.LogError("DI Server login returned empty session ID");
                return false;
            }

            lock (_lock)
            {
                _databaseName = connectionInfo.CompanyDB;
                _companyName = connectionInfo.CompanyDB;
                _isConnected = true;
            }

            _logger.LogInformation("Successfully connected to SAP B1 DI Server. SessionId={SessionId}", 
                _sessionId.Length > 8 ? _sessionId.Substring(0, 8) : _sessionId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error connecting to DI Server at {Url}", connectionInfo.DiServerUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SAP B1 DI Server");
            return false;
        }
    }

    private StringContent BuildLoginSoapRequest(string companyDB, string userName, string password, string language)
    {
        var soapXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <soap:Body>
    <Login xmlns=""http://www.sap.com/SAPBusinessOne/DI/"">
      <DBName>{XmlEscape(companyDB)}</DBName>
      <UserName>{XmlEscape(userName)}</UserName>
      <Password>{XmlEscape(password)}</Password>
      <Language>{XmlEscape(language)}</Language>
    </Login>
  </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "http://www.sap.com/SAPBusinessOne/DI/Login");
        return content;
    }

    private string? ParseLoginResponse(string responseXml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(responseXml);
            
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("di", "http://www.sap.com/SAPBusinessOne/DI/");
            
            var loginResult = doc.SelectSingleNode("//di:LoginResult", nsManager);
            return loginResult?.InnerText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse DI Server login response");
            return null;
        }
    }

    public async Task DisconnectAsync()
    {
        string? sessionToLogout = null;
        
        lock (_lock)
        {
            if (_isConnected && !string.IsNullOrEmpty(_sessionId))
            {
                sessionToLogout = _sessionId;
            }
        }

        if (sessionToLogout != null && !string.IsNullOrEmpty(_serverUrl))
        {
            try
            {
                var soapRequest = BuildLogoutSoapRequest(sessionToLogout);
                await _httpClient.PostAsync(_serverUrl, soapRequest);
                _logger.LogInformation("Disconnected from SAP B1 DI Server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from DI Server");
            }
        }

        lock (_lock)
        {
            _isConnected = false;
            _sessionId = null;
            _companyName = null;
            _databaseName = null;
        }
    }

    private StringContent BuildLogoutSoapRequest(string sessionId)
    {
        var soapXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <Logout xmlns=""http://www.sap.com/SAPBusinessOne/DI/"">
      <SessionID>{XmlEscape(sessionId)}</SessionID>
    </Logout>
  </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "http://www.sap.com/SAPBusinessOne/DI/Logout");
        return content;
    }

    public async Task<IEnumerable<SapAddOnInfo>> GetInstalledAddonsAsync()
    {
        var addons = new List<SapAddOnInfo>();

        string? sessionId;
        string? serverUrl;

        lock (_lock)
        {
            if (!_isConnected || string.IsNullOrEmpty(_sessionId))
            {
                _logger.LogWarning("Not connected to SAP B1 DI Server");
                return addons;
            }
            sessionId = _sessionId;
            serverUrl = _serverUrl;
        }

        if (string.IsNullOrEmpty(serverUrl))
        {
            return addons;
        }

        try
        {
            string query = @"SELECT U_AddOnID, U_Name, U_Version, U_Description, U_Company, U_Status, U_InstallationDate 
                             FROM SARI 
                             ORDER BY U_Name";

            var soapRequest = BuildExecuteSqlSoapRequest(sessionId, query);
            var response = await _httpClient.PostAsync(serverUrl, soapRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ExecuteSQL failed with status: {Status}", response.StatusCode);
                return addons;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var results = ParseExecuteSqlResponse(responseContent);

            int count = 0;
            foreach (var row in results)
            {
                try
                {
                    var addon = new SapAddOnInfo
                    {
                        AddOnId = GetRowValue(row, "U_AddOnID"),
                        Name = GetRowValue(row, "U_Name"),
                        Version = GetRowValue(row, "U_Version"),
                        Description = GetRowValue(row, "U_Description"),
                        DatabaseName = GetRowValue(row, "U_Company"),
                        Status = ParseStatus(GetRowValue(row, "U_Status"))
                    };

                    var dateStr = GetRowValue(row, "U_InstallationDate");
                    if (DateTime.TryParse(dateStr, out var installDate))
                    {
                        addon.InstallationDate = installDate;
                    }

                    addons.Add(addon);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading add-on record {Count}", count);
                }
            }

            _logger.LogInformation("Found {Count} add-ons from SARI table via DI Server", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying add-ons from DI Server");
        }

        return addons;
    }

    private StringContent BuildExecuteSqlSoapRequest(string sessionId, string query)
    {
        var soapXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <ExecuteSQL xmlns=""http://www.sap.com/SAPBusinessOne/DI/"">
      <SessionID>{XmlEscape(sessionId)}</SessionID>
      <Query>{XmlEscape(query)}</Query>
    </ExecuteSQL>
  </soap:Body>
</soap:Envelope>";

        var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "http://www.sap.com/SAPBusinessOne/DI/ExecuteSQL");
        return content;
    }

    private List<Dictionary<string, string>> ParseExecuteSqlResponse(string responseXml)
    {
        var results = new List<Dictionary<string, string>>();
        
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(responseXml);
            
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("di", "http://www.sap.com/SAPBusinessOne/DI/");
            
            var resultNodes = doc.SelectNodes("//di:row", nsManager);
            
            if (resultNodes != null)
            {
                foreach (XmlNode node in resultNodes)
                {
                    var row = new Dictionary<string, string>();
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        row[child.Name] = child.InnerText;
                    }
                    results.Add(row);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse ExecuteSQL response");
        }

        return results;
    }

    private string GetRowValue(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private AddOnStatus ParseStatus(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AddOnStatus.Active,
            "INSTALLED" => AddOnStatus.Installed,
            "REGISTERED" => AddOnStatus.Registered,
            "INACTIVE" => AddOnStatus.Inactive,
            _ => AddOnStatus.Unknown
        };
    }

    private static string XmlEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}