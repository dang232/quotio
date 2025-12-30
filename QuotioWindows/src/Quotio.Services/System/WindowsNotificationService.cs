using Microsoft.Toolkit.Uwp.Notifications;
using Quotio.Core.Enums;
using Quotio.Core.Interfaces;

namespace Quotio.Services.System;

public class WindowsNotificationService : INotificationService
{
    public void ShowInfo(string title, string message)
    {
        ShowToast(title, message);
    }

    public void ShowWarning(string title, string message)
    {
        ShowToast(title, message, "⚠️");
    }

    public void ShowError(string title, string message)
    {
        ShowToast(title, message, "❌");
    }

    public void ShowQuotaWarning(AIProvider provider, string account, double percentage)
    {
        ShowToast("Quota Warning", $"{provider} ({account}) is at {percentage:F1}% usage!", "⚠️");
    }

    private void ShowToast(string title, string message, string? prefix = null)
    {
        var text = prefix != null ? $"{prefix} {message}" : message;
        
        new ToastContentBuilder()
            .AddText(title)
            .AddText(text)
            .Show();
    }
}
