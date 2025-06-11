using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnvifService.Repository;
using SharpOnvifServer;
using SharpOnvifServer.Discovery;

var builder = WebApplication.CreateBuilder();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddControllers();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddOnvifDigestAuthentication();
builder.Services.AddOnvifDiscovery(builder.Configuration.GetSection("OnvifDiscovery").Get<OnvifDiscoveryOptions>());

builder.Services.AddSingleton<OnvifService.Onvif.DeviceImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.MediaImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.EventsImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.PTZImpl>();

#warning TODO: Subscription Manager should be instantiated dynamically per connection, now it will work only for a single connection
builder.Services.AddSingleton<OnvifService.Onvif.SubscriptionManagerImpl>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseOnvif();

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;

    serviceBuilder.AddService<OnvifService.Onvif.DeviceImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(OnvifBindingFactory.CreateBinding(), "/onvif/device_service");

    serviceBuilder.AddService<OnvifService.Onvif.MediaImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.MediaImpl, SharpOnvifServer.Media.Media>(OnvifBindingFactory.CreateBinding(), "/onvif/media_service");

    var eventBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.EventsImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.NotificationProducer>(eventBinding, "/onvif/events_service");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.EventPortType>(eventBinding, "/onvif/events_service");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.PullPoint>(eventBinding, "/onvif/events_service");

    var subscriptionBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.SubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.SubscriptionManagerImpl, SharpOnvifServer.Events.SubscriptionManager>(subscriptionBinding, "/onvif/Events/SubManager");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.SubscriptionManagerImpl, SharpOnvifServer.Events.PausableSubscriptionManager>(subscriptionBinding, "/onvif/Events/SubManager");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.SubscriptionManagerImpl, SharpOnvifServer.Events.PullPointSubscription>(subscriptionBinding, "/onvif/Events/SubManager");

    serviceBuilder.AddService<OnvifService.Onvif.PTZImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.PTZImpl, SharpOnvifServer.PTZ.PTZ>(OnvifBindingFactory.CreateBinding(), "/onvif/ptz_service");

    // TODO: add more service endpoints
});

app.MapControllers();

app.Run();