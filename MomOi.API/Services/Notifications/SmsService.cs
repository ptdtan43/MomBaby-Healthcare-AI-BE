using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MomOi.API.Services.Notifications
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;
        private bool _isInitialized;

        public SmsService(IConfiguration config, ILogger<SmsService> logger)
        {
            _config = config;
            _logger = logger;
            InitializeTwilio();
        }

        private void InitializeTwilio()
        {
            var accountSid = _config["Twilio:AccountSid"];
            var authToken = _config["Twilio:AuthToken"];

            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
            {
                TwilioClient.Init(accountSid, authToken);
                _isInitialized = true;
            }
            else
            {
                _logger.LogWarning("Twilio credentials are not configured. SMS will not be sent.");
            }
        }

        public async Task SendSmsAsync(string toPhone, string message)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Skipping SMS send because Twilio is not initialized.");
                return;
            }

            var fromPhone = _config["Twilio:FromPhone"];
            if (string.IsNullOrEmpty(fromPhone))
            {
                _logger.LogWarning("Twilio FromPhone is not configured.");
                return;
            }

            try
            {
                // Format phone number to E.164 if necessary (e.g. +84...)
                if (!toPhone.StartsWith("+"))
                {
                    if (toPhone.StartsWith("0"))
                        toPhone = "+84" + toPhone.Substring(1);
                    else
                        toPhone = "+" + toPhone;
                }

                var msg = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(fromPhone),
                    to: new PhoneNumber(toPhone)
                );

                _logger.LogInformation("SMS sent successfully to {ToPhone}, SID: {Sid}", toPhone, msg.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {ToPhone}", toPhone);
            }
        }
    }
}
