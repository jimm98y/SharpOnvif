using SharpOnvifClient;
using SharpOnvifCommon;
using System;
using System.Linq;

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
    Console.WriteLine($"Camera time: {cameraDateTime}");

    // check if media profile is available
    if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
    {
        var profiles = await client.GetProfilesAsync();
        var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First());
        Console.WriteLine($"Stream URI: {streamUri.Uri}");
    }
}
