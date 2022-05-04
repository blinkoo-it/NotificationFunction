using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleNotificationDto
{
        [JsonProperty(PropertyName = "aps")]
        public AppleApnsObject Aps { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
}