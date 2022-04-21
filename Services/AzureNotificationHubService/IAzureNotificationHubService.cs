using Microsoft.Azure.NotificationHubs;

namespace NotificationFunction.Services
{
    public interface IAzureNotificationHubService
    {
        public NotificationHubClient notificationHub { get; set; }
    }
}