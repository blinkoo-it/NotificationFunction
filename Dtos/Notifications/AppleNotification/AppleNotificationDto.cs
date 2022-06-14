using System;
using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AppleNotificationDto
{
        [JsonProperty(PropertyName = "aps")]
        public AppleApsObject Aps { get; set; }
        [JsonProperty(PropertyName = "nid")]
        public Guid Nid { get; set; }
        [JsonProperty(PropertyName = "campaignId")]
        public string CampaignId { get; set; }
        [JsonProperty(PropertyName = "sentAt")]
        public long SentAt { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
}