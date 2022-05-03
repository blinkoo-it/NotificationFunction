using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using NotificationFunction.Models;

namespace NotificationFunction.Helpers
{
    public class MessageSenderHelper
    {

        public static async Task<HelperResponse> SendMessageBatchAsync(ServiceBusSender sender, List<string> messages, int delayUpperLimit = 10)
        {
            HelperResponse response = new HelperResponse();
            try {
                // create a random number to add a random delay in queue messages
                Random randomDelayGenerator = new Random();
                // create a queue containing the messages and return it to the caller
                Queue<ServiceBusMessage> serviceBusMessages = new Queue<ServiceBusMessage>();
                // for each message in the list of messages
                foreach (string message in messages) {
                    // if the user does not provide a 'delayUpperLimit' it will be set to 10
                    // consequently the delay in seconds for each message will be a random number of seconds between 0 and 10
                    // if the user provide 0, there will be no delay!
                    int delayInSeconds = delayUpperLimit > 0 ? randomDelayGenerator.Next(0, delayUpperLimit) : 0;
                    // create the service bus message from the message
                    ServiceBusMessage serviceBusMessage = new ServiceBusMessage(message);
                    // now add the delay to the message
                    serviceBusMessage.ScheduledEnqueueTime = DateTimeOffset.Now.AddSeconds(delayInSeconds);
                    // finally enqueue the message
                    serviceBusMessages.Enqueue(serviceBusMessage);
                }
                // total number of messages to be sent to the Service Bus queue
                int messageCount = serviceBusMessages.Count;
                // while our local queue still has messages continue 
                while (serviceBusMessages.Count > 0)
                {
                    // start a new batch 
                    using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
                    // add the first message to the batch
                    if (messageBatch.TryAddMessage(serviceBusMessages.Peek())) {
                        // dequeue the message from the .NET queue once the message is added to the batch
                        serviceBusMessages.Dequeue();
                    } else {
                        // if the first message can't fit, then it is too large for the batch
                        throw new Exception($"Message {messageCount - serviceBusMessages.Count} is too large and cannot be sent.");
                    }
                    // add as many messages as possible to the current batch
                    while (serviceBusMessages.Count > 0 && messageBatch.TryAddMessage(serviceBusMessages.Peek())) 
                    {
                        // dequeue the message from the .NET queue as it has been added to the batch
                        serviceBusMessages.Dequeue();
                    }
                    // now, send the batch
                    await sender.SendMessagesAsync(messageBatch);
                }
                // change the response object Success to true
                response.Success = true;
                // return the response object
                return response;
            } catch (Exception ex) {
                // change the response object Success to true
                response.Success = false;
                // add the exception text so we can easily log it
                response.ErrorMessage = ex.Message;
                // return the response object
                return response;
            }
        }
    }
}