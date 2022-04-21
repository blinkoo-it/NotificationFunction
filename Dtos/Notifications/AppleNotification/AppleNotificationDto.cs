using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleNotificationDto
{
        [JsonProperty(PropertyName = "apns")]
        public AppleApnsObject Apns { get; set; }
        [JsonProperty("data")]
        public AppleDataObject Data { get; set; }
}