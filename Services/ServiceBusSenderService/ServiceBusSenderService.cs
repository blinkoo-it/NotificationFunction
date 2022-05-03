using System;
using Azure.Messaging.ServiceBus;

namespace NotificationFunction.Services
{
    public class ServiceBusSenderService : IServiceBusSenderService
    {
        public ServiceBusClient serviceBusClient { get; set; }
        public ServiceBusSender pushNotificationSender { get; set; }
        public ServiceBusSenderService(ServiceBusClient serviceBusClient)
        {
            // attach input service bus client to instance serviceBusClient variable
            this.serviceBusClient = serviceBusClient;
            // at the moment we only need one sender
            string pushNotificationQueue = Environment.GetEnvironmentVariable("QUEUE_NOTIFICATION");
            this.pushNotificationSender = serviceBusClient.CreateSender(pushNotificationQueue);
        }
    }
}