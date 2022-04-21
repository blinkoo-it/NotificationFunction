using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleDataObject
{
    [JsonProperty(PropertyName = "payload")]
    public string Payload { get; set; } = string.Empty;
}