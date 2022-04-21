using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleApnsObject
{
    [JsonProperty(PropertyName = "alert")]
    public AppleApnsAlertObject Alert { get; set; }
}