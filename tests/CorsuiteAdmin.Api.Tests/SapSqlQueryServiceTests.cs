using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CorsuiteAdmin.Api.Services.SAP;

namespace CorsuiteAdmin.Api.Tests;

public class SapSqlQueryServiceTests
{
    private readonly Mock<ILogger<SapSqlQueryService>> _loggerMock;
    private readonly Mock<ISqlConnectionValidator> _validatorMock;
    private readonly SapSqlQueryService _service;

    public SapSqlQueryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SapSqlQueryService>>();
        _validatorMock = new Mock<ISqlConnectionValidator>();
        _service = new SapSqlQueryService(_loggerMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task QueryAddonsFromDatabaseAsync_WhenValidationFails_ReturnsEmpty()
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), true, 15))
            .ReturnsAsync(new SqlConnectionValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Connection failed" 
            });

        // Act
        var result = await _service.QueryAddonsFromDatabaseAsync(connectionString);

        // Assert
        Assert.Empty(result);
        _validatorMock.Verify(v => v.ValidateAsync(connectionString, true, 15), Times.Once);
    }

    [Fact]
    public async Task QueryCorsuiteModulesAsync_WhenValidationFails_ReturnsEmpty()
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        var corsuiteDb = "CorsuiteDB";
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), true, 15))
            .ReturnsAsync(new SqlConnectionValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Connection failed" 
            });

        // Act
        var result = await _service.QueryCorsuiteModulesAsync(connectionString, corsuiteDb);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task QueryCorsuiteModulesAsync_WithInvalidDbName_ReturnsEmpty()
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        var invalidDbName = "; DROP TABLE Users;--"; // SQL injection attempt

        // Act
        var result = await _service.QueryCorsuiteModulesAsync(connectionString, invalidDbName);

        // Assert
        Assert.Empty(result);
        // Should not call validator for invalid db names
        _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ValidateConnectionAsync_ProxiesToValidator()
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        var expectedResult = new SqlConnectionValidationResult { IsValid = true };
        _validatorMock
            .Setup(v => v.ValidateAsync(connectionString, true, 15))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.ValidateConnectionAsync(connectionString);

        // Assert
        Assert.Same(expectedResult, result);
    }

    [Theory]
    [InlineData("ValidDB")]
    [InlineData("db_123")]
    [InlineData("DB-Name")]
    public async Task QueryCorsuiteModulesAsync_WithValidDbName_Proceeds(string dbName)
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), true, 15))
            .ReturnsAsync(new SqlConnectionValidationResult { IsValid = true });

        // Act
        var result = await _service.QueryCorsuiteModulesAsync(connectionString, dbName);

        // Assert - validation was called
        _validatorMock.Verify(v => v.ValidateAsync(connectionString, true, 15), Times.Once);
    }

    [Fact]
    public async Task QueryCorsuiteModulesAsync_WithEmptyDbName_Proceeds()
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";
        // When db name is empty, it should still proceed (uses default catalog)
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), true, 15))
            .ReturnsAsync(new SqlConnectionValidationResult { IsValid = true });

        // Act
        var result = await _service.QueryCorsuiteModulesAsync(connectionString, "");

        // Assert - validation should be called even with empty db name
        _validatorMock.Verify(v => v.ValidateAsync(connectionString, true, 15), Times.Once);
    }

    [Theory]
    [InlineData("db;drop table")]
    [InlineData("db' or '1'='1")]
    public async Task QueryCorsuiteModulesAsync_WithSqlInjectionAttempt_ReturnsEmpty(string dbName)
    {
        // Arrange
        var connectionString = "Server=localhost;User Id=sa;Password=pwd;";

        // Act
        var result = await _service.QueryCorsuiteModulesAsync(connectionString, dbName);

        // Assert - should return empty without calling validator
        Assert.Empty(result);
        _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }
}
