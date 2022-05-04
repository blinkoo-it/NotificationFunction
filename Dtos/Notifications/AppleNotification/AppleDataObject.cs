using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleDataObject
{

    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}