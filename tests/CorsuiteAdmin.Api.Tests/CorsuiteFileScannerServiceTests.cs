using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CorsuiteAdmin.Api.Services.SAP;

namespace CorsuiteAdmin.Api.Tests;

public class CorsuiteFileScannerServiceTests
{
    private readonly Mock<ILogger<CorsuiteFileScannerService>> _loggerMock;
    private readonly CorsuiteFileScannerService _service;

    public CorsuiteFileScannerServiceTests()
    {
        _loggerMock = new Mock<ILogger<CorsuiteFileScannerService>>();
        _service = new CorsuiteFileScannerService(_loggerMock.Object);
    }

    [Fact]
    public void GetPossibleCorsuitePaths_ReturnsPaths()
    {
        // Act
        var paths = _service.GetPossibleCorsuitePaths().ToList();

        // Assert - should return at least some paths
        Assert.NotNull(paths);
    }

    [Fact]
    public async Task ScanFolderAsync_WithNonExistentPath_ReturnsEmpty()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await _service.ScanFolderAsync(nonExistentPath);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ScanCorsuiteFoldersAsync_ReturnsEnumerable()
    {
        // Act
        var result = await _service.ScanCorsuiteFoldersAsync();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GetPossibleCorsuitePaths_ContainsPaths()
    {
        // Act
        var paths = _service.GetPossibleCorsuitePaths().ToList();

        // Assert - should contain some valid paths
        Assert.NotEmpty(paths);
        Assert.All(paths, p => Assert.NotNull(p));
    }
}
