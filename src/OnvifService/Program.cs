using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnvifService.Onvif;
using OnvifService.Repository;
using SharpOnvifServer;
using SharpOnvifServer.Discovery;
using SharpOnvifServer.Events;

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
builder.Services.AddSingleton<OnvifService.Onvif.PTZImpl>();

// events
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEventSource, EventSourceImpl>();
builder.Services.AddSingleton<IEventSubscriptionManager<SubscriptionManagerImpl>, DefaultEventSubscriptionManager<SubscriptionManagerImpl>>();
builder.Services.AddSingleton<OnvifService.Onvif.EventsImpl>();
builder.Services.AddSingleton<OnvifService.Onvif.RouterSubscriptionManagerImpl>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseOnvif().UseOnvifEvents("/onvif/Events/Subscription");

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;

    serviceBuilder.AddService<OnvifService.Onvif.DeviceImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(OnvifBindingFactory.CreateBinding(), "/onvif/device_service");

    serviceBuilder.AddService<OnvifService.Onvif.MediaImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.MediaImpl, SharpOnvifServer.Media.Media>(OnvifBindingFactory.CreateBinding(), "/onvif/media_service");

    serviceBuilder.AddService<OnvifService.Onvif.PTZImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.PTZImpl, SharpOnvifServer.PTZ.PTZ>(OnvifBindingFactory.CreateBinding(), "/onvif/ptz_service");

    var eventBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.EventsImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.NotificationProducer>(eventBinding, "/onvif/events_service");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.EventPortType>(eventBinding, "/onvif/events_service");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.EventsImpl, SharpOnvifServer.Events.PullPoint>(eventBinding, "/onvif/events_service");

    var subscriptionBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.RouterSubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.SubscriptionManager>(subscriptionBinding, "/onvif/Events/Subscription");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.PausableSubscriptionManager>(subscriptionBinding, "/onvif/Events/Subscription");
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.PullPointSubscription>(subscriptionBinding, "/onvif/Events/Subscription");

    // add more service endpoints
});

app.MapControllers();

app.Run();