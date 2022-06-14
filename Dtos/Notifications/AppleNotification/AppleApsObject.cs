using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleApsObject
{
    [JsonProperty(PropertyName = "alert")]
    public AppleApsAlertObject Alert { get; set; }
}