using System;
using Newtonsoft.Json;

namespace NotificationFunction.Dtos.QueueMessages;

public class PublishedPostMessageDto
{
        [JsonProperty(PropertyName = "userId")]
        public Guid UserId { get; set; }
}