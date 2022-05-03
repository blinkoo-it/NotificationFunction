using Azure.Messaging.ServiceBus;

namespace NotificationFunction.Services
{
    public interface IServiceBusSenderService
    {
        ServiceBusClient serviceBusClient { get; set; }
        ServiceBusSender pushNotificationSender { get; set; }
    }
}