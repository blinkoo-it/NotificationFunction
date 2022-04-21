using Microsoft.Azure.Cosmos;

namespace NotificationFunction.Services
{
    public interface ICosmosContainerService
    {
        public CosmosClient client { get; set; }
        public Container userContainer { get; set; }
    }
}