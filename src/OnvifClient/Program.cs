using SharpOnvifClient;
using SharpOnvifCommon;
using System;
using System.Linq;

var devices = await DiscoveryClient.DiscoverAsync();
var client = new SimpleOnvifClient(devices.First(x => x.Contains("localhost")), "admin", "password");

var deviceInfo = await client.GetDeviceInformationAsync();
var services = await client.GetServicesAsync(true);
var cameraDateTime = await client.GetSystemDateAndTimeUtcAsync();
var cameraTimeOffset = DateTime.UtcNow.Subtract(cameraDateTime);
Console.WriteLine($"Camera time: {cameraDateTime}");

if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
{
    var profiles = await client.GetProfilesAsync();
    var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First());
    Console.WriteLine($"Stream URI: {streamUri.Uri}");
}