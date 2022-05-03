# NotificationFunction

## How the service works
The service is made of two functions:
1. an `HTTP trigger` that expects a request carrying a json body. We expect the body to contain multiple sub-objects each representing a notifcation that will be sent to a **specific user**. The body will have this structure:
    ```json
    {
        "messages": [
            <messageObject>,
            <messageObject>,
            ...
            ...
            ...
        ]
    }
    ```
    To add a layer of security to the http trigger, we require each request to have the header `x-api-key` containing the api key value. If the header is not sent or the api key value does not match, the messages will not be processed by the service.
    
2. a `ServiceBus trigger` that processes one message and interact with `Microsoft Notifications Hub` to send the push notification to the user/s using the tag/channel specified in the message itself.

## Message structure

In our first implementation we decided that the service will send a notification to a specific user. For this reason each message object, will have to contain **all** of the information necessary to send the push notification to the user, even if there is redundancy within the information sent on each message.

This is the structure we expect:
```json
{
    "title": string,
    "body": string,
    "tag": string,
    "platforms": string[],
    "type": string,
    "id": string?,
    "filters": string[]?
}
```
1. `"title"` *(mandatory)* is a string containing the title of the notification, this will be displayed on the user device.
2. `"body"` *(mandatory)* is a string containing the body of the notification, this will also be displayed on the user device.
3. `"tag"` *(mandatory)* is a string which determines who will receive the push notification. **NOTE**: in our system we need to specify the `profileSlug` *(mandatory)* value of a user to send a push notification to that user. So in our case `tag` must contain the `profileSlug` value. Failing to provide the correct value for a user will result in a failure to send the notification to that user.
4. `"platforms"` *(mandatory)* is an array of strings representing the OS of the devices of the user. **NOTE**: since our app is available only on `iOS` and `Android`, the user can have at most two differrent type of devices, hence the `platforms` array can only contain **one or two** elements. 
For `Android` we expect to receive the string `"fcm"`, for `"iOS"` we expect to receive `"apns"`. If the user actively uses device on both platforms we expect to receive both `"apns"` and `"fcm"` in the array.
5. `"type"` *(mandatory)* is a string representing a label that will "tell" the mobile app what action needs to be performed after receiving the push notification. It will be `"POST"` if we want to open a particular video in app. It will be `"FEED"` if we want to open a particular filtered feed (NOT YET IMPLEMENTED IN APP ATM).
6. `"id"` is a field contaiing the `keyId` value of a particular post we want to open in app. **NOTE**: this is mandatory only if `"type": "POST"`, viceversa it can be left blank or not provided.
7. `"filters"` is field containing an array of strings, each representing a `feedListId` (filter). Providing multiple elements will direct the user to a filtered feed obtained by combining all of the filters. **NOTE**: this is mandatory only if `"type": "FEED"`, viceversa it can be left blank or not provided.

## Examples
Send a push notification to open a specific video:
```json
{
    "title": "Nuovo video virale per te",
    "body": "Ciao Daniele, abbiamo pensato che questo nuovo video possa piacerti: scopri anche tu le terme di Saturnia",
    "tag": "danieleguerzoni-8a01ea0a",
    "platforms": [ "fcm" ],
    "type": "POST",
    "id": "2802aeed962e845d48d8a44d6f3394629740b3af83e2bab54f6fc5dab3edf3f0"
}
```

Send a push notification to open a filtered feed:
```json
{
    "title": "Scopri la Toscana",
    "body": "Ciao Daniele, scopri le opportunita' di relax che la Toscana ha da offrirti",
    "tag": "danieleguerzoni-8a01ea0a",
    "platforms": [ "fcm" ],
    "type": "FEED",
    "filters": [ "IT_16", "THM-012" ]
}
```