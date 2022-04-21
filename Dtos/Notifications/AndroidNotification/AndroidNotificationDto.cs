using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AndroidNotificationDto
{
        [JsonProperty(PropertyName = "notification")]
        public AndroidNotificationObject Notification { get; set; }
        [JsonProperty("data")]
        public AndroidDataObject Data { get; set; }
}