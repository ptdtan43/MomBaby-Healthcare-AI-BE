using System.Threading.Tasks;

namespace MomOi.API.Services.Notifications
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
