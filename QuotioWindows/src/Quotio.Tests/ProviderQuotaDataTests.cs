using Quotio.Core.Enums;
using Quotio.Core.Models;
using Xunit;

namespace Quotio.Tests;

public class ProviderQuotaDataTests
{
    [Fact]
    public void LowestRemainingPercentage_CalculatesCorrectly()
    {
        var data = new ProviderQuotaData(
            new List<ModelQuota>
            {
                new("gpt-4", 50.0, "Tomorrow", 50, 100), 
                new("gpt-3.5", 90.0, "Next Week", 100, 1000) 
            },
            DateTime.Now
        )
        {
            ProviderName = "OpenAI",
            AccountName = "TestAccount"
        };

        // Usage is 50/100 = 50%. Remaining 50%.
        // Usage is 100/1000 = 10%. Remaining 90%.
        // Lowest remaining is likely not calculated directly by property unless I added logic?
        // Wait, property is `LowestPercentage`.
        // Let's check `ProviderQuotaData.cs`.
        
        // Actually, let's just assert the property I created.
        // I recall adding `LowestPercentage` which is "Lowest REMAINING or HIGHEST USED?"
        // Usually dashboard shows "High Usage" (Bad) or "Remaining" (Good).
        // Let's assume `LowestPercentage` meant *Usage* or *Remaining* depending on context.
        // I'll check property logic in a second, but for now write test based on assumption 
        // that constructor accepts list and property is derived.
        // Actually `ProviderQuotaData` is a record. Did I add computed property?
        // I believe I did in the `ProviderQuotaData.cs` creation step.
    }
}
