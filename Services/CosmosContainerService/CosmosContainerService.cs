using System;
using Microsoft.Azure.Cosmos;

namespace NotificationFunction.Services
{
    public class CosmosContainerService : ICosmosContainerService
    {
        public CosmosClient client { get; set; }
        public Container userContainer { get; set; }
        public CosmosContainerService(CosmosClient cosmosClient)
        {
            // get the database from the client
            Database database = cosmosClient.GetDatabase(Environment.GetEnvironmentVariable("COSMOS_DATABASE"));
            // get the containers and assign them to the corresponding properties
            this.client = cosmosClient;
            this.userContainer = database.GetContainer(Environment.GetEnvironmentVariable("COSMOS_USERINFO_CONTAINER"));
        }
    }
}