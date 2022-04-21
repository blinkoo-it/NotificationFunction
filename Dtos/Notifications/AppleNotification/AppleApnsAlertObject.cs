using System;
using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;
public class AppleApnsAlertObject
{
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty(PropertyName = "body")]
    public string Body { get; set; } = string.Empty;
}