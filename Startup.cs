using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
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

            // inject the service connecting to azure sdk
            builder.Services.AddSingleton<IAzureNotificationHubService>(o => {
                NotificationHubClient notificationHub = NotificationHubClient.CreateClientFromConnectionString(
                    Environment.GetEnvironmentVariable("NOTIFICATION_HUB_CONNECTION_STRING"),
                    Environment.GetEnvironmentVariable("NOTIFICATION_HUB_NAME")
                );
                return new AzureNotificationHubService(notificationHub: notificationHub);
            });

            // inject the service connecting to cosmos client
            builder.Services.AddSingleton<ICosmosContainerService>((s) =>
            {
                // get the env variables for the connection
                string accountEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
                string accountKey = Environment.GetEnvironmentVariable("COSMOS_KEY");
                // build the client via CosmosClientBuilder
                CosmosClientBuilder cosmosClientBuilder = new CosmosClientBuilder(
                    accountEndpoint: accountEndpoint,
                    authKeyOrResourceToken: accountKey
                );
                CosmosClient cosmosClient = cosmosClientBuilder.WithConnectionModeDirect().Build();
                // use the cosmosClient to build an instance of our CosmosClientService which comes with preconfigured containers
                return new CosmosContainerService(cosmosClient: cosmosClient);
            });
        }
    }
}