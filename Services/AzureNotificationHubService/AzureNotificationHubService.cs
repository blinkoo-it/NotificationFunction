using Microsoft.Azure.NotificationHubs;

namespace NotificationFunction.Services
{
    public class AzureNotificationHubService : IAzureNotificationHubService
    {
        public NotificationHubClient notificationHub { get; set; }

        public AzureNotificationHubService(NotificationHubClient notificationHub)
        {
            this.notificationHub = notificationHub;
        }
    }
}