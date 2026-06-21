using System.Collections.Generic;
using System.Threading.Tasks;

namespace MomOi.API.Services.Notifications
{
    public interface IPushNotificationService
    {
        Task SendPushNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
    }
}
