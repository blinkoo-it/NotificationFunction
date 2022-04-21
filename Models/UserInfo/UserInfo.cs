using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NotificationFunction.Models.UserInfo
{
    public class UserInfo
    {
        // when using Cosmos client we must set discriminator manually
        [JsonProperty(PropertyName = "discriminator")]
        public string Discriminator { get; set; } = "UserInfo";
        [JsonProperty("id")]
        public string id { get; set; }
        [Key]
        [JsonProperty("userId")]
        public Guid UserId { get; set; }
        [JsonProperty("givenName")]
        public string GivenName { get; set; }
        [JsonProperty("surname")]
        public string Surname { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("profileSlug")]
        public string ProfileSlug { get; set; }
        // this is an object storing the users devices
        // we need to peek in here to know how we should send the push notification
        [JsonProperty(PropertyName = "userDevices")]
        public ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();
    }
}