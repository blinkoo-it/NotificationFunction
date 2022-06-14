using System;
using Newtonsoft.Json;

namespace NotificationFunction.Dtos.Notifications;

public class AndroidDataObject
{
    // note: this is temporary!
    // title and body should be taken from the notification object
    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty(PropertyName = "body")]
    public string Body { get; set; } = string.Empty;
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