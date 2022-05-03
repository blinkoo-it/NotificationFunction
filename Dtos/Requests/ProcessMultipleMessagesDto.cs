using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using NotificationFunction.Dtos.QueueMessages;

namespace NotificationFunction.Dtos.Requests;

public class ProcessMultipleMessagesDto
{
        [JsonProperty(PropertyName = "messages")]
        [Required(ErrorMessage = "Messages field is required")]
        public List<NotificationHubMessageDto> Messages { get; set; }
}