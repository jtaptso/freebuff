using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CorsuiteAdmin.Api.Services.SAP;

namespace CorsuiteAdmin.Api.Tests;

public class SqlConnectionValidatorTests
{
    private readonly Mock<ILogger<SqlConnectionValidator>> _loggerMock;
    private readonly SqlConnectionValidator _validator;

    public SqlConnectionValidatorTests()
    {
        _loggerMock = new Mock<ILogger<SqlConnectionValidator>>();
        _validator = new SqlConnectionValidator(_loggerMock.Object);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_WithValidConnectionString_ReturnsValid()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=password;";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_WithEmptyString_ReturnsInvalid()
    {
        // Arrange
        var connectionString = "";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_WithWhitespace_ReturnsInvalid()
    {
        // Arrange
        var connectionString = "   ";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_WithoutServer_ReturnsInvalid()
    {
        // Arrange
        var connectionString = "Database=TestDB;User Id=sa;Password=password;";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Data Source", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_WithInvalidFormat_ReturnsInvalid()
    {
        // Arrange
        var connectionString = "NotAValidConnectionString;";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid connection string format", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateFormatOnlyAsync_ExtractsDatabaseName()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=MyDatabase;User Id=sa;Password=pwd;";

        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("MyDatabase", result.DatabaseName);
    }

    [Fact]
    public async Task ValidateAsync_WithTestConnectFalse_DoesNotConnect()
    {
        // Arrange
        var connectionString = "Server=nonexistent;Database=Test;User Id=sa;Password=pwd;";

        // Act
        var result = await _validator.ValidateAsync(connectionString, testConnect: false);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ServerVersion);
    }

    [Theory]
    [InlineData("Server=localhost;TrustServerCertificate=true;", true)]
    [InlineData("Server=.;", true)]
    [InlineData("Server=localhost;Integrated Security=true;", true)]
    public async Task ValidateFormatOnlyAsync_VariousValidFormats_ReturnsValid(string connectionString, bool expected)
    {
        // Act
        var result = await _validator.ValidateFormatOnlyAsync(connectionString);

        // Assert
        Assert.Equal(expected, result.IsValid);
    }
}
