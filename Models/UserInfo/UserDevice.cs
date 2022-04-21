using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NotificationFunction.Models.UserInfo
{
    public class UserDevice
    {
        [Key]
        [JsonProperty(PropertyName = "installationId")]
        public string InstallationId { get; set; }
        
        [JsonProperty(PropertyName = "platform")]
        public string Platform { get; set; }

        [JsonProperty(PropertyName = "pushChannel")]
        public string PushChannel { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public List<UserDeviceTag> Tags { get; set; } = new List<UserDeviceTag>();
    }
}