using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificationFunction.Dtos.Notifications;
using NotificationFunction.Dtos.QueueMessages;
using NotificationFunction.Models.UserInfo;
using NotificationFunction.Services;

namespace NotificationFunction
{
    public class NotificationHubSender
    {
        private ICosmosContainerService _cosmosContainerService;
        private IAzureNotificationHubService _azureNotificationHubService;
        public NotificationHubSender(ICosmosContainerService cosmosContainerService, IAzureNotificationHubService azureNotificationHubService)
        {
            _cosmosContainerService = cosmosContainerService;
            _azureNotificationHubService = azureNotificationHubService;
        }
        
        [FunctionName("NotificationHubSender")]
        [FixedDelayRetry(10, "00:01:00")]
        public async Task NotificationOnPublication([ServiceBusTrigger("%QUEUE_UPLOAD_NOTIFICATION%", Connection = "SERVICE_BUS_CONNECTION_STRING")]
            string queueItem, 
            ILogger log
        )
        {
            // These are all 'dev' user ids just for debug purposes
            // NOTE: remove these when this app is ready for production
            // e0a72cc2-94a7-4ba8-8c3b-9b446486108f <- Daniele
            // c5a25167-e61e-400f-930f-52da1122749d <- Sofia
            // 05a79036-5017-482c-92a4-992667cb516c <- Federico ?
            // f016910f-f03b-4805-8474-8eed83713f65 <- Massimo
            // ebbd0128-4d47-4ff2-8804-20cad9ccdb4d <- Smeraldo
            // ff4c8dbb-e6e4-4cf4-a436-b2c04dff2cef <- Alessandro
            // Receive the message, deserialise everything
            PublishedPostMessageDto publishedPostMessageDto = JsonConvert.DeserializeObject<PublishedPostMessageDto>(queueItem);
            // initialise a new variable just holding userId
            Guid userId = publishedPostMessageDto.UserId;
            // we need to make sure the user exists, since we need to know which user devices
            UserInfo userInfo;
            // get the document from the db
            try {
                userInfo = await _cosmosContainerService.userContainer.ReadItemAsync<UserInfo>(
                    id: $"UserInfo|{userId}", 
                    partitionKey: new PartitionKey(userId.ToString())
                );
            } catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                // this is here in case the document is not in our db even if the user has a userId
                // THIS SHOULD NOT HAPPEN EVER
                // If it ever happens just return so that the message is not re-inserted in the queue
                Console.WriteLine("record not found");
                return;
            }
            log.LogInformation($"User: {userInfo.GivenName} {userInfo.Surname}, displayName: {userInfo.ProfileSlug}, registered devices: {userInfo.UserDevices.Count()}");
            // we need get the devices of the user from his/her document
            List<UserDevice> devices = userInfo.UserDevices.ToList();
            // from the devices we are interested in the pns (platform) associated to each device
            // once we know the platform/s we can use the proper notification structure and sdk method
            List<string> userPns = new List<string>();
            // iterate the devices and get pns 
            foreach (UserDevice ud in devices) {
                if (!userPns.Contains(ud.Platform)) { 
                    log.LogInformation($"{userInfo.ProfileSlug} has a {ud.Platform} platform");
                    userPns.Add(ud.Platform); 
                }
            }
            // the user might have multiple devices on both pns!
            // for this reason there could be two outcomes to be checked
            List<NotificationOutcome> outcomes = new List<NotificationOutcome>();
            // initalise the status code from notifcation hub
            HttpStatusCode status = HttpStatusCode.InternalServerError;
            // the channel - in this case it is  the profile slug
            string userTag = userInfo.ProfileSlug;
            // 
            string title = "Blinkoo says hello!";
            string body = $"Hello {userInfo.GivenName}! Your post has been now published!";
            log.LogInformation($"Message will be sent to channel: {userTag}");
            // we only need to handle: 
            // iOS -> apns
            // Android -> fcm

            foreach(string pns in userPns) {
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
                        //     "data": {
                        //         "payload": <anything>
                        //     }
                        // }
                        // initialise the alert dto
                        AppleNotificationDto alertObject = new AppleNotificationDto()
                        {
                            Apns = new AppleApnsObject() { Alert = new AppleApnsAlertObject() { Title = title, Body = body } },
                            Data = new AppleDataObject() { Payload = "" }
                        };
                        // stringify the alert object
                        string alertMessage = JsonConvert.SerializeObject(alertObject);
                        // string alert = "{\"aps\":{\"alert\": {\"title\":\"Blinkoo dice ciao\",\"body\":\"ciao\"}}, \"data\": {\"payload\": \"ciao\"}}";
                        log.LogDebug(alertMessage);
                        try {
                            // send the alertMessage to iOS devices at the defined userTag
                            outcome = await _azureNotificationHubService.notificationHub.SendAppleNativeNotificationAsync(alertMessage, userTag);
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
                        //         "body": <body>
                        //     },
                        //     "data": {
                        //         "title": <title>,
                        //         "body": <body>
                        //     }
                        // }
                        AndroidNotificationDto notificationObject = new AndroidNotificationDto()
                        {
                            Notification = new AndroidNotificationObject() { Title = title, Body = body },
                            Data = new AndroidDataObject() { Title = title, Body = body }
                        };
                        // strigify the object
                        string notificationMessage = JsonConvert.SerializeObject(notificationObject);
                        // string notif = "{\"notification\":{\"title\":\"Blinkoo dice ciao\",\"body\":\"" + message + "\"}, \"data\" : {\"title\":\"Blinkoo dice ciao\",\"body\":\"" + message + "\"}}";
                        log.LogDebug(notificationMessage);
                        try {
                            // send the alertMessage to iOS devices at the defined userTag
                            outcome = await _azureNotificationHubService.notificationHub.SendFcmNativeNotificationAsync(notificationMessage, userTag);
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
                log.LogError($"Error sending notifications via Notification Hub for user {userInfo.DisplayName}");
                throw new Exception();
            }
        }
    }
}
