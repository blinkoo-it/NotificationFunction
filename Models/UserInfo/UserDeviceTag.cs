using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NotificationFunction.Models.UserInfo
{
    public class UserDeviceTag
    {
        [Key]
        [JsonProperty(PropertyName = "tag")]
        public string Tag { get; set; }
    }
}