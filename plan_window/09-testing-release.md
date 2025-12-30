# Phase 9: Testing & Release

> **Duration**: 3-5 days  
> **Goal**: Unit tests, installer, documentation

---

## Tasks

- [ ] Unit tests for services
- [ ] Integration tests
- [ ] Create MSIX/MSI installer
- [ ] Documentation

---

## Unit Tests

### QuotaFetcherTests.cs
```csharp
public class AntigravityFetcherTests
{
    [Fact]
    public async Task FetchQuota_ValidToken_ReturnsData()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("*").Respond("application/json", 
            """{"models":{"gemini-pro":{"quotaInfo":{"remainingFraction":0.75}}}}""");
        
        var fetcher = new AntigravityQuotaFetcher(
            new HttpClient(mockHttp), 
            Mock.Of<ILogger<AntigravityQuotaFetcher>>());

        // Act
        var result = await fetcher.FetchQuotaAsync("test-token");

        // Assert
        result.Models.Should().HaveCount(1);
        result.Models[0].Percentage.Should().Be(75);
    }
}
```

### ProxyManagerTests.cs
```csharp
public class ProxyManagerTests
{
    [Fact]
    public async Task Start_WhenStopped_StartsProxy()
    {
        var settings = new SettingsService(Mock.Of<ILogger<SettingsService>>());
        var manager = new CLIProxyManager(
            Mock.Of<ILogger<CLIProxyManager>>(), settings);

        manager.Status.IsRunning.Should().BeFalse();
        // Full test would require actual binary
    }
}
```

---

## Run Tests

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage"
```

---

## Create Installer

### Using MSIX
```powershell
# Install Windows SDK with MSIX tools
dotnet publish src/Quotio.App -c Release -r win-x64 --self-contained

# Create MSIX package (requires Windows SDK)
makeappx pack /d publish /p Quotio.msix
signtool sign /fd SHA256 /f cert.pfx /p password Quotio.msix
```

### Using Inno Setup
```iss
[Setup]
AppName=Quotio
AppVersion=1.0.0
DefaultDirName={autopf}\Quotio
DefaultGroupName=Quotio
OutputDir=..\dist
OutputBaseFilename=QuotioSetup

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recurse

[Icons]
Name: "{group}\Quotio"; Filename: "{app}\Quotio.App.exe"
Name: "{autodesktop}\Quotio"; Filename: "{app}\Quotio.App.exe"

[Run]
Filename: "{app}\Quotio.App.exe"; Flags: nowait postinstall
```

---

## Build Script

### build-release.ps1
```powershell
$version = "1.0.0"
$output = "dist"

# Build
dotnet publish src/Quotio.App -c Release -r win-x64 --self-contained -o publish

# Create installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" scripts/installer.iss

# Create ZIP portable
Compress-Archive -Path publish/* -DestinationPath "$output/Quotio-$version-portable.zip"

Write-Host "Build complete: $output"
```

---

## Documentation

### README.md for Windows
```markdown
# Quotio for Windows

Command center for AI coding assistants.

## Installation
1. Download latest release from GitHub
2. Run QuotioSetup.exe
3. Launch from Start Menu

## Features
- Multi-provider support (Gemini, Claude, Codex, etc.)
- Quota tracking
- Agent configuration
- System tray integration

## Requirements
- Windows 10/11
- .NET 8 Runtime (bundled)
```

---

## Release Checklist

- [ ] All tests passing
- [ ] Version bumped
- [ ] CHANGELOG updated
- [ ] Installer tested
- [ ] GitHub release created
- [ ] Documentation updated

---

## Complete! 🎉

You now have complete implementation plans for:
1. Project Setup
2. Core Models
3. Quota Fetchers
4. Core Services
5. Agent Services
6. UI Foundation
7. UI Screens
8. System Integration
9. Testing & Release
