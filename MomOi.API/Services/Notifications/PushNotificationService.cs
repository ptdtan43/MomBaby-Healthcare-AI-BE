using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MomOi.API.Services.Notifications
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PushNotificationService> _logger;
        private bool _isInitialized;

        public PushNotificationService(IConfiguration config, ILogger<PushNotificationService> logger)
        {
            _config = config;
            _logger = logger;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    _isInitialized = true;
                    return;
                }

                var credentialPath = _config["Firebase:CredentialPath"];
                if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
                {
                    _logger.LogWarning("Firebase credential path is invalid or missing. Push notifications will be disabled.");
                    return;
                }

                #pragma warning disable CS0618 // Type or member is obsolete
                var credential = GoogleCredential.FromFile(credentialPath);
                #pragma warning restore CS0618 // Type or member is obsolete

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FirebaseApp.");
            }
        }

        public async Task SendPushNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Skipping push notification because Firebase is not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(fcmToken))
            {
                _logger.LogWarning("Skipping push notification because FCM token is empty.");
                return;
            }

            try
            {
                var message = new Message()
                {
                    Token = fcmToken,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    }
                };

                // Add custom data if needed (must be dictionary of string, string)
                if (data != null && data is Dictionary<string, string> dictData)
                {
                    message.Data = dictData;
                }

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Successfully sent message: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification to {Token}", fcmToken);
            }
        }
    }
}
