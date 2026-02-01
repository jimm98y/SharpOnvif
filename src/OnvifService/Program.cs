// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer;

var builder = WebApplication.CreateBuilder();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddControllers();

builder.Services.AddSingleton<SharpOnvifServer.IUserRepository, OnvifService.Repository.UserRepository>();
builder.Services.AddOnvifDigestAuthentication();
builder.Services.AddOnvifDiscovery(builder.Configuration.GetSection("OnvifDiscovery").Get<SharpOnvifServer.Discovery.OnvifDiscoveryOptions>());

builder.Services.AddSingleton<OnvifService.Onvif.DeviceImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.MediaImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.PTZImpl>();

// events
builder.Services.AddHttpClient();
builder.Services.AddSingleton<SharpOnvifServer.Events.IEventSource, OnvifService.Onvif.EventSourceImpl>();
builder.Services.AddSingleton<SharpOnvifServer.Events.IEventSubscriptionManager<OnvifService.Onvif.SubscriptionManagerImpl>, SharpOnvifServer.Events.DefaultEventSubscriptionManager<OnvifService.Onvif.SubscriptionManagerImpl>>();
builder.Services.AddSingleton<OnvifService.Onvif.EventsImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.RouterSubscriptionManagerImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.RouterPullPointSubscriptionManagerImpl>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

const string URI_EVENTS_SUBSCRIPTION = "/onvif/Events/Subscription";
const string URI_EVENTS_PULLPOINT_SUBSCRIPTION = "/onvif/Events/PullPointSubscription";
app.UseOnvif()
   .UseOnvifEvents(URI_EVENTS_SUBSCRIPTION)
   .UseOnvifEvents(URI_EVENTS_PULLPOINT_SUBSCRIPTION);

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
    serviceMetadataBehavior.HttpsGetEnabled = true;

    // Note: CoreWCF does not allow multiple services on a single endpoint. This means it is not possible to use a single endpoint address "/onvif/device_service" for both DeviceImpl and MediaImpl. 
    // To run it all on a single address, one would have to create OnvifImpl class that implements all the interfaces:
    /*
    public class OnvifImpl : SharpOnvifServer.DeviceMgmt.Device, SharpOnvifServer.Media.Media, SharpOnvifServer.PTZ.PTZ, SharpOnvifServer.Events.NotificationProducer, SharpOnvifServer.Events.EventPortType, SharpOnvifServer.Events.PullPoint
    {
        ...
    }

    const string URI_DEVICE_SERVICE = "/onvif/device_service";
    var onvifBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.OnvifImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.DeviceMgmt.Device>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Media.Media>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.PTZ.PTZ>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.NotificationProducer>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.EventPortType>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.PullPoint>(onvifBinding, URI_DEVICE_SERVICE);

    Note: this would only work as long as there are no collisions, meaning 2 interfaces must not implement the same method
    */

    serviceBuilder.AddService<OnvifService.Onvif.DeviceImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(OnvifBindingFactory.CreateBinding(), "/onvif/device_service");

    serviceBuilder.AddService<OnvifService.Onvif.MediaImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.MediaImpl, SharpOnvifServer.Media.Media>(OnvifBindingFactory.CreateBinding(), "/onvif/media_service");

    serviceBuilder.AddService<OnvifService.Onvif.PTZImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.PTZImpl, SharpOnvifServer.PTZ.PTZ>(OnvifBindingFactory.CreateBinding(), "/onvif/ptz_service");

    const string URI_EVENTS_SERVICE = "/onvif/events_service";
    var eventBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.EventsImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.NotificationProducer>(eventBinding, URI_EVENTS_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.EventPortType>(eventBinding, URI_EVENTS_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.PullPoint>(eventBinding, URI_EVENTS_SERVICE);

    serviceBuilder.AddService<OnvifService.Onvif.RouterSubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.SubscriptionManager>(OnvifBindingFactory.CreateBinding(), URI_EVENTS_SUBSCRIPTION);

    serviceBuilder.AddService<OnvifService.Onvif.RouterPullPointSubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterPullPointSubscriptionManagerImpl, SharpOnvifServer.Events.PullPointSubscription>(OnvifBindingFactory.CreateBinding(), URI_EVENTS_PULLPOINT_SUBSCRIPTION);

    // add more service endpoints
});

app.MapControllers();

app.Run();