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

const string URI_EVENTS_SUBSCRIPTION = "/onvif/Events/Subscription";
app.UseOnvif().UseOnvifEvents(URI_EVENTS_SUBSCRIPTION);

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
    serviceMetadataBehavior.HttpsGetEnabled = true;

    // Note: CoreWCF does not allow multiple services on a single endpoint. This means it is not possible to use a single endpoint address "/onvif/device_service" for both DeviceImpl and MediaImpl. 
    // To run it all on a single address, one would have to create a OnvifImpl base class that implements all the underlying interfaces:
    /*
    public class OnvifImpl : Device, Media, PTZ, NotificationProducer, EventPortType, PullPoint
    {
    ...
    }

    const string URI_DEVICE_SERVICE = "/onvif/device_service";
    var onvifBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.OnvifImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.DeviceMgmt.Device>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Media.Media>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.PTZ.PTZ>(onvifBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.NotificationProducer>(eventBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.EventPortType>(eventBinding, URI_DEVICE_SERVICE);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifImpl, SharpOnvifServer.Events.PullPoint>(eventBinding, URI_DEVICE_SERVICE);
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

    var subscriptionBinding = OnvifBindingFactory.CreateBinding();
    serviceBuilder.AddService<OnvifService.Onvif.RouterSubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.SubscriptionManager>(subscriptionBinding, URI_EVENTS_SUBSCRIPTION);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.PausableSubscriptionManager>(subscriptionBinding, URI_EVENTS_SUBSCRIPTION);
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.RouterSubscriptionManagerImpl, SharpOnvifServer.Events.PullPointSubscription>(subscriptionBinding, URI_EVENTS_SUBSCRIPTION);

    // add more service endpoints
});

app.MapControllers();

app.Run();