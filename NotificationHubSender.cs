using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificationFunction.Dtos.Notifications;
using NotificationFunction.Dtos.QueueMessages;
using NotificationFunction.Dtos.Requests;
using NotificationFunction.Helpers;
using NotificationFunction.Models;
using NotificationFunction.Services;

namespace NotificationFunction
{
    public class NotificationHubSender
    {
        private IAzureNotificationHubService _azureNotificationHubService;
        private IServiceBusSenderService _serviceBusSenderService;
        public NotificationHubSender(IServiceBusSenderService serviceBusSenderService, IAzureNotificationHubService azureNotificationHubService)
        {
            _azureNotificationHubService = azureNotificationHubService;
            _serviceBusSenderService = serviceBusSenderService;
        }

        [FunctionName("ProcessMultipleMessagesTrigger")]
        public async Task<IActionResult> ProcessMultipleMessagesTrigger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifications")]
            HttpRequest req,
            ILogger log
        )
        {
            // initalise the response object
            ServiceResponse<MessageCount> serviceResponse = new ServiceResponse<MessageCount>() { Data = null, Message = null, Status = 200, Success = true };
            // get the api key value from env
            string apiKey = Environment.GetEnvironmentVariable("HTTPTRIGGER_API_KEY");
            // get the corresponding header if it was sent
            string apiKeyHeader = req.Headers["x-api-key"];
            // if there is no api key header or api key does not match then return 401
            if (apiKeyHeader == null || apiKeyHeader != apiKey) { 
                log.LogError($"Input: {apiKeyHeader}");
                log.LogError($"Match: {apiKey}");
                log.LogError("Api key is invalid");
                ObjectResult unauthorisedResponse = new ObjectResult("Unauthorized");
                unauthorisedResponse.StatusCode = StatusCodes.Status401Unauthorized;
                return unauthorisedResponse;
            }
            log.LogInformation("api key matches");
            // if the user is authorised then check the incoming data
            // use the helper to validate the body
            InputRequestBody<ProcessMultipleMessagesDto> inputRequestBody = await ValidateRequestBodyHelper.ValidateBodyAsync<ProcessMultipleMessagesDto>(req, log);
            // now check the validity of the incoming data
            if (!inputRequestBody.IsValid) {
                log.LogError("Request body is not valid");
                // attach the corresponding data to our response object
                serviceResponse.Status = 422;
                serviceResponse.Success = false;
                serviceResponse.Message = $"Model is invalid: {string.Join(", ", inputRequestBody.ValidationResults.Select(s => s.ErrorMessage).ToArray())}";
                // send our response object inside the ObjectResult
                ObjectResult unprocessableEntitiyResponse = new ObjectResult(serviceResponse);
                unprocessableEntitiyResponse.StatusCode = StatusCodes.Status422UnprocessableEntity;
                return unprocessableEntitiyResponse;
            }
            log.LogInformation("request body passed validation");
            // NOTE: at this point we still need to validate both 'tags' and 'platforms' array
            // however if 'tags' contains no valid tag we can simply not send the notification
            // if 'platforms' contains no platforms or contains platforms different from 'apns' or 'fcm' then we do not send the notification
            // however we should track how many invalid messages there are and send back the info
            int totalMessages = 0;
            int invalidMessages = 0;
            // prepare a list of strings that will hold all stringified messages
            List<string> queueMessages = new List<string>();
            // iterate all messages
            foreach(NotificationHubMessageDto nhmessage in inputRequestBody.Value.Messages) {
                // validate tags and platforms before sending the message
                bool isPlatformsValid = false;
                bool isTagsValid = false;
                // check that tags has at least one tag
                if (nhmessage.Tags.Length > 0) { isTagsValid = true; }
                // check that platforms has at least one valid platform
                foreach(string platform in nhmessage.Platforms) { 
                    if (platform == "apns" || platform == "fcm") { isPlatformsValid = true; }
                }
                // finally if both flags are true we can proceed by stringifying the message and push it to the message array
                if (isPlatformsValid && isTagsValid) {
                    // serialise the message to string
                    string nhmessageString = JsonConvert.SerializeObject(nhmessage);
                    // add the message to queueMessages
                    queueMessages.Add(nhmessageString);
                } else { invalidMessages +=1; }

                totalMessages += 1;
            }
            // now that we have the counters we can prepare the MessageCount object to attach to our response
            MessageCount messageCount = new MessageCount() { TotalMessages = totalMessages, InvalidMessages = invalidMessages };
            // attach message count whether everything went well or not
            serviceResponse.Data = messageCount;
            // if there is at least one valid message then use the helper and send it to the queue
            if (queueMessages.Count() > 0) {
                // define the upper limit of the delay - this will make sure that messages are going in the queue with different delays
                // this will help ease the load on the function when we have hundreds/thousands of messages
                int delayUpperLimit = 10;
                // for every 500 messages raise delayUpperLimit by 15
                int multiplicator = Convert.ToInt32(queueMessages.Count / 500);
                // if multiplicator is bigger than 0 we have more than 500 messages so we need to add a delay based on the value of multiplicator
                delayUpperLimit += (multiplicator * 15);
                // send a message on the postpreview queue to update the user document with the new post preview
                HelperResponse responseQueue = await MessageSenderHelper.SendMessageBatchAsync(
                    sender: _serviceBusSenderService.pushNotificationSender,
                    messages: queueMessages,
                    delayUpperLimit: delayUpperLimit
                );
                // check if enqueuing failed
                if (responseQueue.Success == false) {
                    serviceResponse.Status = 500;
                    serviceResponse.Success = false;
                    serviceResponse.Message = $"Error while enqueuing messages on Azure ServiceBus queue {Environment.GetEnvironmentVariable("QUEUE_NOTIFICATION")}";
                    // send our response object inside the ObjectResult
                    ObjectResult serverErrorResponse = new ObjectResult(serviceResponse);
                    serverErrorResponse.StatusCode = StatusCodes.Status500InternalServerError;
                    return serverErrorResponse;
                }
            }
            // if everything went well we will end up here
            // NOTE: we considere a "positive" case even the one where we receive no messages OR all invalid messages
            // by returning counts we let the client know about that
            // return response
            return new OkObjectResult(serviceResponse);
        }

        
        [FunctionName("NotificationHubSender")]
        [FixedDelayRetry(10, "00:00:30")]
        public async Task NotificationOnPublication([ServiceBusTrigger("%QUEUE_NOTIFICATION%", Connection = "SERVICE_BUS_CONNECTION_STRING")]
            string queueItem, 
            ILogger log
        )
        {
            // Receive the message, deserialise everything
            NotificationHubMessageDto nhmessage = JsonConvert.DeserializeObject<NotificationHubMessageDto>(queueItem);
            // get the user platforms
            string[] platforms = nhmessage.Platforms;
            // since the user can have two platforms there could be two outcomes to be checked
            List<NotificationOutcome> outcomes = new List<NotificationOutcome>();
            // initalise the status code from notifcation hub
            HttpStatusCode status = HttpStatusCode.InternalServerError;
            // prepare the notification data
            string[] tags = nhmessage.Tags;
            string title = nhmessage.Title;
            string body = nhmessage.Body;
            string campaignId = nhmessage.CampaignId;
            Guid nid = Guid.NewGuid();
            long sentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string notificationType = nhmessage.Type;
            string notificationId = nhmessage.Id; 
            log.LogInformation($"Message will be sent to channel: {tags.First()}");
            // we only need to handle: 
            // iOS -> apns
            // Android -> fcm

            foreach(string pns in platforms) {
                // for each pns of the user initialise an outcome variable
                NotificationOutcome outcome = null;
                // depending on the pns we need to use different sdk methods and the notification will have a different structure
                switch (pns)
                {
                    case "apns":
                        // NOTE: in iOS we must use this object structure:
                        // an "aps" property is mandatory: it contains the actual notification title and body
                        // anything outside of the aps object can be use to send custom information (for example we use "data")
                        // {
                        //     "aps": {
                        //         "alert": {
                        //             "title": <title>,
                        //             "body": <body>
                        //         }
                        //     },
                        //     "nid": "027df646-c0b7-4cc7-9a81-ba8db18f019c",
                        //     "campaignId": <campaignId>
                        //     "sentAt": 1655195051248,
                        //     "type": <type>
                        //     "id": <id>
                        // }
                        // initialise the alert dto
                        AppleNotificationDto alertObject = new AppleNotificationDto()
                        {
                            Aps = new AppleApsObject() { Alert = new AppleApsAlertObject() { Title = title, Body = body } },
                            Nid = nid,
                            CampaignId = campaignId,
                            SentAt = sentAt,
                            Type = notificationType, 
                            Id = notificationId 
                        };
                        // stringify the alert object
                        string alertMessage = JsonConvert.SerializeObject(alertObject);
                        log.LogWarning("Message sent on 'apns' platform");
                        log.LogWarning(alertMessage);
                        // now try to send the notification
                        try {
                            // send the alertMessage to iOS devices at the defined userTag
                            outcome = await _azureNotificationHubService.notificationHub.SendAppleNativeNotificationAsync(
                                jsonPayload: alertMessage, 
                                tags: tags
                            );
                        } catch (Exception ex) {
                            log.LogError($"Error while reaching notification hub: {ex.Message}");
                            outcome = null;
                        }
                        // add outcome to the outcomes list
                        outcomes.Add(outcome);
                        // get out of the switch
                        break;
                    case "fcm":
                        // NOTE: in Android we must use this object structure:
                        // a "notification" property is mandatory: it contains the actual notification title and body
                        // a "data" object property allows to send custom data
                        // NOTE: 04/15/2022 -> at this time the app requires "title" and "body" in the "data" object due to a bug!
                        // if we do not send these then the user will not see "title" and "body" if the app is open
                        // {
                        //     "notification": {
                        //         "title": <title>,
                        //         "body": <body>,
                        //         "nid": "027df646-c0b7-4cc7-9a81-ba8db18f019c",
                        //         "campaignId": <campaignId>
                        //         "sentAt": 1655195051248,
                        //         "type": <type>,
                        //         "id": <id>
                        //     },
                        //     "data": {
                        //         "title": <title>,
                        //         "body": <body>,
                        //         "nid": "027df646-c0b7-4cc7-9a81-ba8db18f019c",
                        //         "campaignId": <campaignId>
                        //         "sentAt": 1655195051248,
                        //         "type": <type>,
                        //         "id": <id>
                        //     }
                        // }
                        AndroidNotificationDto notificationObject = new AndroidNotificationDto()
                        {
                            Notification = new AndroidNotificationObject() 
                            { 
                                Title = title, 
                                Body = body, 
                                Nid = nid,
                                CampaignId = campaignId,
                                SentAt = sentAt,
                                Type = notificationType, 
                                Id = notificationId 
                            },
                            Data = new AndroidDataObject() 
                            { 
                                Title = title, 
                                Body = body, 
                                Nid = nid,
                                CampaignId = campaignId,
                                SentAt = sentAt,
                                Type = notificationType, 
                                Id = notificationId 
                            }
                        };
                        // strigify the object
                        string notificationMessage = JsonConvert.SerializeObject(notificationObject);
                        log.LogWarning($"Message sent on 'fcm' platform and tags {tags.First()}");
                        log.LogWarning(notificationMessage);
                        // send the notification
                        try {
                            // send the alertMessage to iOS devices at the defined userTag
                            outcome = await _azureNotificationHubService.notificationHub.SendFcmNativeNotificationAsync(
                                jsonPayload: notificationMessage, 
                                tags: tags
                            );
                        } catch (Exception ex) {
                            log.LogError($"Error while reaching notification hub: {ex.Message}");
                            outcome = null;
                        }
                        // add outcome to the outcomes list
                        outcomes.Add(outcome);
                        // get out of the switch
                        break;
                }
            }
            // at least one of the notification should produce a positive outcome
            // this will result into the outcome variable to be not equal to null
            // we need to have at least ONE positive outcome
            foreach (NotificationOutcome outcome in outcomes) {
                // NOTE: we initialised the status variable as HttpStatusCode.InternalServerError
                if (outcome != null) {
                    // in this case check that the outcome state is either not abandoned or not unknown
                    // apparently any other case it is OK (according to the examples)
                    if (!((outcome.State == NotificationOutcomeState.Abandoned) || (outcome.State == NotificationOutcomeState.Unknown))) {
                        // if this is the case we can change the status variable
                        status = HttpStatusCode.OK;
                    }
                }
            }
            // in case status remains as 500 then something went wrong, for this reason we should throw and retry later
            if (status != HttpStatusCode.OK) {
                log.LogError($"Error sending notifications via Notification Hub for tag {tags.First()}");
                throw new Exception();
            }
        }
    }
}
