using System.Threading.Tasks;

namespace MomOi.API.Services.Notifications
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhone, string message);
    }
}
