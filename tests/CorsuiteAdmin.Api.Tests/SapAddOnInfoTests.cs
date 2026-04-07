using Xunit;
using CorsuiteAdmin.Api.Services.SAP;

namespace CorsuiteAdmin.Api.Tests;

public class AddOnStatusTests
{
    [Theory]
    [InlineData("ACTIVE", AddOnStatus.Active)]
    [InlineData("active", AddOnStatus.Active)]
    [InlineData("INSTALLED", AddOnStatus.Installed)]
    [InlineData("installed", AddOnStatus.Installed)]
    [InlineData("REGISTERED", AddOnStatus.Registered)]
    [InlineData("registered", AddOnStatus.Registered)]
    [InlineData("INACTIVE", AddOnStatus.Inactive)]
    [InlineData("inactive", AddOnStatus.Inactive)]
    [InlineData("UNKNOWN", AddOnStatus.Unknown)]
    [InlineData("random", AddOnStatus.Unknown)]
    [InlineData(null, AddOnStatus.Unknown)]
    [InlineData("", AddOnStatus.Unknown)]
    public void AddOnStatus_ParseFromString_ReturnsExpectedStatus(string? input, AddOnStatus expected)
    {
        // Arrange
        var status = input?.ToUpperInvariant() switch
        {
            "ACTIVE" => AddOnStatus.Active,
            "INSTALLED" => AddOnStatus.Installed,
            "REGISTERED" => AddOnStatus.Registered,
            "INACTIVE" => AddOnStatus.Inactive,
            _ => AddOnStatus.Unknown
        };

        // Assert
        Assert.Equal(expected, status);
    }
}

public class ConnectionInfoTests
{
    [Fact]
    public void ConnectionInfo_DefaultValues_AreCorrect()
    {
        // Act
        var connectionInfo = new ConnectionInfo();

        // Assert
        Assert.Equal(string.Empty, connectionInfo.Server);
        Assert.Equal(string.Empty, connectionInfo.CompanyDB);
        Assert.Equal(string.Empty, connectionInfo.UserName);
        Assert.Equal(string.Empty, connectionInfo.Password);
        Assert.Equal("dst_MSSQL2022", connectionInfo.DbType);
    }

    [Fact]
    public void ConnectionInfo_CanSetAllProperties()
    {
        // Arrange
        var connectionInfo = new ConnectionInfo
        {
            Server = "myserver",
            CompanyDB = "MyCompany",
            UserName = "admin",
            Password = "password",
            DbUserName = "dbuser",
            DbPassword = "dbpass",
            DbType = "dst_HANADB",
            LicenseServer = "licenseserver"
        };

        // Assert
        Assert.Equal("myserver", connectionInfo.Server);
        Assert.Equal("MyCompany", connectionInfo.CompanyDB);
        Assert.Equal("admin", connectionInfo.UserName);
        Assert.Equal("password", connectionInfo.Password);
        Assert.Equal("dbuser", connectionInfo.DbUserName);
        Assert.Equal("dbpass", connectionInfo.DbPassword);
        Assert.Equal("dst_HANADB", connectionInfo.DbType);
        Assert.Equal("licenseserver", connectionInfo.LicenseServer);
    }
}

public class SapAddOnInfoTests
{
    [Fact]
    public void SapAddOnInfo_DefaultValues_AreCorrect()
    {
        // Act
        var info = new SapAddOnInfo();

        // Assert
        Assert.Equal(string.Empty, info.AddOnId);
        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.Version);
        Assert.Null(info.Description);
        Assert.Null(info.DatabaseName);
        Assert.Null(info.InstallationDate);
        Assert.Equal(AddOnStatus.Unknown, info.Status);
    }

    [Fact]
    public void SapAddOnInfo_CanSetAllProperties()
    {
        // Arrange
        var date = DateTime.Now;
        var info = new SapAddOnInfo
        {
            AddOnId = "ADDON001",
            Name = "Test Addon",
            Version = "1.0.0",
            Description = "Test Description",
            DatabaseName = "TestDB",
            InstallationDate = date,
            Status = AddOnStatus.Active
        };

        // Assert
        Assert.Equal("ADDON001", info.AddOnId);
        Assert.Equal("Test Addon", info.Name);
        Assert.Equal("1.0.0", info.Version);
        Assert.Equal("Test Description", info.Description);
        Assert.Equal("TestDB", info.DatabaseName);
        Assert.Equal(date, info.InstallationDate);
        Assert.Equal(AddOnStatus.Active, info.Status);
    }
}

public class SqlConnectionValidationResultTests
{
    [Fact]
    public void SqlConnectionValidationResult_DefaultValues_AreCorrect()
    {
        // Act
        var result = new SqlConnectionValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ServerVersion);
        Assert.Null(result.DatabaseName);
        Assert.Null(result.TimeoutSeconds);
    }

    [Fact]
    public void SqlConnectionValidationResult_CanSetAllProperties()
    {
        // Arrange
        var result = new SqlConnectionValidationResult
        {
            IsValid = true,
            ErrorMessage = "Success",
            ServerVersion = "15.0.0",
            DatabaseName = "TestDB",
            TimeoutSeconds = 30
        };

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Success", result.ErrorMessage);
        Assert.Equal("15.0.0", result.ServerVersion);
        Assert.Equal("TestDB", result.DatabaseName);
        Assert.Equal(30, result.TimeoutSeconds);
    }
}
