using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AndroidDataObject
{
    // note: this is temporary!
    // title and body should be taken from the notification object
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty(PropertyName = "body")]
    public string Body { get; set; } = string.Empty;
}