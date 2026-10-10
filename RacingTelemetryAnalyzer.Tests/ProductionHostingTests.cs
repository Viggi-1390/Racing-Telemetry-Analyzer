using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace RacingTelemetryAnalyzer.Tests;

public class ProductionHostingTests
{
    private string GetFrontendDistPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "frontend", "dist");
            if (Directory.Exists(candidate)) return candidate;

            var subCandidate = Path.Combine(dir.FullName, "RacingTelemetryAnalyzer", "frontend", "dist");
            if (Directory.Exists(subCandidate)) return subCandidate;

            dir = dir.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "frontend", "dist");
    }

    [Fact]
    public void FrontendDist_IndexHtml_ExistsAndContainsReactRoot()
    {
        // Arrange
        var distPath = GetFrontendDistPath();

        // Assert directory exists
        Assert.True(Directory.Exists(distPath), $"frontend/dist directory must exist at: {distPath}");

        var indexPath = Path.Combine(distPath, "index.html");
        Assert.True(File.Exists(indexPath), "index.html must exist in frontend/dist");

        var html = File.ReadAllText(indexPath);
        Assert.Contains("<div id=\"root\"></div>", html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("Racing Telemetry Analyzer", html);
    }

    [Fact]
    public void FrontendDist_AssetsDirectory_ContainsCompiledJsAndCss()
    {
        // Arrange
        var distPath = GetFrontendDistPath();
        var assetsPath = Path.Combine(distPath, "assets");

        // Assert
        Assert.True(Directory.Exists(assetsPath), "frontend/dist/assets must exist");

        var files = Directory.GetFiles(assetsPath);
        Assert.NotEmpty(files);
        Assert.True(files.Any(f => f.EndsWith(".js")), "Must contain at least one compiled JS bundle");
        Assert.True(files.Any(f => f.EndsWith(".css")), "Must contain at least one compiled CSS stylesheet");
    }

    [Fact]
    public void PhysicalFileProvider_SuccessfullyResolvesDistFiles()
    {
        // Arrange
        var distPath = GetFrontendDistPath();
        var provider = new PhysicalFileProvider(distPath);

        // Act
        var fileInfo = provider.GetFileInfo("index.html");

        // Assert
        Assert.NotNull(fileInfo);
        Assert.True(fileInfo.Exists);
        Assert.Equal("index.html", fileInfo.Name);
        Assert.True(fileInfo.Length > 0);
    }

    [Theory]
    [InlineData("/api/vehicles", true)]
    [InlineData("/api/tracks", true)]
    [InlineData("/api/sessions", true)]
    [InlineData("/api/telemetry/latest", true)]
    [InlineData("/api/nonexistent", true)]
    [InlineData("/", false)]
    [InlineData("/tracks", false)]
    [InlineData("/telemetry", false)]
    [InlineData("/garage", false)]
    public void SpaFallback_RouteFilter_IdentifiesApiRoutesAccurately(string path, bool isApi)
    {
        // Arrange
        var pathString = new PathString(path);

        // Act
        bool startsWithApi = pathString.StartsWithSegments("/api");

        // Assert
        Assert.Equal(isApi, startsWithApi);
    }
}
