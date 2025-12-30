using Quotio.Core.Enums;

namespace Quotio.Core.Interfaces;

public interface INotificationService
{
    void ShowInfo(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
    void ShowQuotaWarning(AIProvider provider, string account, double percentage);
}
