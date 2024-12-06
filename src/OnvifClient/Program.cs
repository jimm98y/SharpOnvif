using SharpOnvifClient;
using SharpOnvifCommon;
using System;
using System.Linq;
using System.Threading.Tasks;

var devices = await DiscoveryClient.DiscoverAsync();
var device = devices.FirstOrDefault(x => x.Contains("localhost"));

if (device == null)
{
    Console.WriteLine("Please run OnvifService on the localhost as Administrator, or use a different camera URL and credentials.");
}
else
{
    var client = new SimpleOnvifClient(device, "admin", "password");

    var deviceInfo = await client.GetDeviceInformationAsync();
    var services = await client.GetServicesAsync(true);
    var cameraDateTime = await client.GetSystemDateAndTimeUtcAsync();
    var cameraTimeOffset = DateTime.UtcNow.Subtract(cameraDateTime);
    client.SetCameraUtcNowOffset(cameraTimeOffset);
    Console.WriteLine($"Camera time: {cameraDateTime}");

    // check if media profile is available
    if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
    {
        var profiles = await client.GetProfilesAsync();
        var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First());
        Console.WriteLine($"Stream URI: {streamUri.Uri}");
    }

    // check if ptz profile is available
    if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.PTZ) != null)
    {
        var profiles = await client.GetProfilesAsync();
        await client.AbsoluteMoveAsync(profiles.Profiles.First().token, 1f, 1f, 1f, 1f, 1f, 1f);

        var currentPosition = await client.GetStatusAsync(profiles.Profiles.First().token);
        Console.WriteLine($"Current pan: {currentPosition.Position.PanTilt.x}");
        Console.WriteLine($"Current tilt: {currentPosition.Position.PanTilt.y}");
        Console.WriteLine($"Current zoom: {currentPosition.Position.Zoom.x}");
    }

    // check if event profile is available
    if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.EVENTS) != null)
    {
        // basic events vs pull point subscription
        bool useBasicEvents = false; // = false;

        if (useBasicEvents)
            await BasicEventSubscription(client);
        else
            await PullPointEventSubscription(client);
    }
}

static async Task PullPointEventSubscription(SimpleOnvifClient client)
{
    var subscription = await client.PullPointSubscribeAsync();
    while (true)
    {
        var messages = await client.PullPointPullMessagesAsync(subscription);

        foreach (var ev in messages.NotificationMessage)
        {
            if (OnvifEvents.IsMotionDetected(ev) != null)
                Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
            else if (OnvifEvents.IsTamperDetected(ev) != null)
                Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
        }
    }
}

static async Task BasicEventSubscription(SimpleOnvifClient client)
{
    // we must run as an Administrator for the Basic subscription to work
    var eventListener = new SimpleOnvifEventListener();
    eventListener.Start((int cameraID, string ev) =>
    {
        if (OnvifEvents.IsMotionDetected(ev) != null)
            Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
        else if (OnvifEvents.IsTamperDetected(ev) != null)
            Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
    });

    var subscriptionResponse = await client.BasicSubscribeAsync(eventListener.GetOnvifEventListenerUri());

    while (true)
    {
        await Task.Delay(1000 * 60 * 4);
        var result = await client.BasicSubscriptionRenewAsync(subscriptionResponse);
    }
}