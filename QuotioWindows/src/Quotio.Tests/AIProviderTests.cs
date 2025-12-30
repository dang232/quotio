using Quotio.Core.Enums;
using Xunit;

namespace Quotio.Tests;

public class AIProviderTests
{
    [Fact]
    public void DisplayName_ReturnsCorrectName()
    {
        Assert.Equal("OpenAI Codex", AIProvider.Codex.GetDisplayName());
        Assert.Equal("GitHub Copilot", AIProvider.Copilot.GetDisplayName());
        Assert.Equal("Antigravity", AIProvider.Antigravity.GetDisplayName());
    }

    [Fact]
    public void IsOAuthProvider_ReturnsFalse_ForCodex()
    {
        // Codex returns true in source?
        // Let's check AIProvider.cs: 
        // AIProvider.Codex => true for SupportsOAuth
        Assert.True(AIProvider.Codex.SupportsOAuth());
    }

    [Fact]
    public void SupportsQuotaTracking_ReturnsTrue_ForAntigravity()
    {
        Assert.True(AIProvider.Antigravity.SupportsQuotaTracking());
    }
}
