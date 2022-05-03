using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NotificationFunction.Dtos.QueueMessages;

public class NotificationHubMessageDto
{
        [JsonProperty(PropertyName = "title")]
        [Required(ErrorMessage = "Title field is required")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "body")]
        [Required(ErrorMessage = "Body field is required")]
        public string Body { get; set; }
        [JsonProperty(PropertyName = "tags")]
        [Required(ErrorMessage = "Tags field is required")]
        public string[] Tags { get; set; }
        [JsonProperty(PropertyName = "platforms")]
        [Required(ErrorMessage = "Platforms field is required")]
        public string[] Platforms { get; set; }
        [JsonProperty(PropertyName = "type")]
        [Required(ErrorMessage = "Type field is required")]
        [RegularExpression("POST|FEED", ErrorMessage = "Type field should either be 'POST' or 'FEED'")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "id")]
        [Required(ErrorMessage = "id field is required")]
        public string Id { get; set; }
}