using SharpOnvifClient;
using SharpOnvifClient.Discovery;

var devices = await DiscoveryClient.DiscoverAsync("0.0.0.0");
var client = new SimpleOnvifClient(devices.First(x => x.Contains("localhost")), "admin", "password");

var deviceInfo = await client.GetDeviceInformationAsync();
var services = await client.GetServicesAsync(true);
var cameraDateTime = await client.GetSystemDateAndTimeUtcAsync();
var cameraTimeOffset = DateTime.UtcNow.Subtract(cameraDateTime);
var profiles = await client.GetProfilesAsync();
var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First());

Console.WriteLine($"Camera time: {cameraDateTime}, stream URI: {streamUri.Uri}");