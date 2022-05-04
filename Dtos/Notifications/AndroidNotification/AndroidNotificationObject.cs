using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AndroidNotificationObject
{
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty(PropertyName = "body")]
    public string Body { get; set; } = string.Empty;
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}