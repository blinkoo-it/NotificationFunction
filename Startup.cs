using System;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationFunction.Services;

[assembly: FunctionsStartup(typeof(NotificationFunction.Startup))]
namespace NotificationFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });
            // inject service bus client and senders
            builder.Services.AddSingleton<IServiceBusSenderService>((s) => {
                // get the connection string for service bus
                string serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING");
                // create the service bus client
                ServiceBusClient serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
                // now build the service itself
                return new ServiceBusSenderService(serviceBusClient: serviceBusClient);
            });
            // inject the service connecting to azure sdk
            builder.Services.AddSingleton<IAzureNotificationHubService>(o => {
                NotificationHubClient notificationHub = NotificationHubClient.CreateClientFromConnectionString(
                    Environment.GetEnvironmentVariable("NOTIFICATION_HUB_CONNECTION_STRING"),
                    Environment.GetEnvironmentVariable("NOTIFICATION_HUB_NAME")
                );
                return new AzureNotificationHubService(notificationHub: notificationHub);
            });
        }
    }
}