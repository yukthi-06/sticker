using System;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace StickerApp.Services
{
    public class NotificationService
    {
        private readonly LoggerService _logger;
        private readonly JsonStorageService _storage;

        public NotificationService(LoggerService logger, JsonStorageService storage)
        {
            _logger = logger;
            _storage = storage;
        }

        public void ShowToast(string title, string content)
        {
            if (!_storage.Settings.NotificationEnabled)
            {
                return;
            }

            try
            {
                // Simple Toast XML
                var xml = $@"
                <toast>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text>{title}</text>
                            <text>{content}</text>
                        </binding>
                    </visual>
                </toast>";

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                var toast = new ToastNotification(xmlDoc);
                
                // For unpackaged apps, we show toast using local app ID
                ToastNotificationManager.CreateToastNotifier("StickerApp").Show(toast);
                
                _logger.LogInfo($"Toast shown: {title} - {content}");
            }
            catch (Exception ex)
            {
                // Portable app might run unpackaged, log the toast error but don't fail
                _logger.LogError("Failed to show toast notification", ex);
            }
        }
    }
}
