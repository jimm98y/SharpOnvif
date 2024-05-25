using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using OnvifService.Repository;
using SharpOnvifServer;

var builder = WebApplication.CreateBuilder();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddControllers();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddOnvifDigestAuthentication();
builder.Services.AddOnvifDiscovery();

builder.Services.AddSingleton((sp) => { return new OnvifService.Onvif.DeviceImpl(sp.GetService<IServer>()); });
builder.Services.AddSingleton((sp) => { return new OnvifService.Onvif.MediaImpl(sp.GetService<IServer>()); });
builder.Services.AddSingleton((sp) => { return new OnvifService.Onvif.NotificationProducerImpl(sp.GetService<IServer>()); });
builder.Services.AddSingleton((sp) => { return new OnvifService.Onvif.SubscriptionManagerImpl(sp.GetService<IServer>()); });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;

    serviceBuilder.AddService<OnvifService.Onvif.DeviceImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(OnvifBindingFactory.CreateBinding(), "/onvif/device_service");

    serviceBuilder.AddService<OnvifService.Onvif.MediaImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.MediaImpl, SharpOnvifServer.Media.Media>(OnvifBindingFactory.CreateBinding(), "/onvif/media_service");

    serviceBuilder.AddService<OnvifService.Onvif.NotificationProducerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.NotificationProducerImpl, SharpOnvifServer.Events.NotificationProducer>(OnvifBindingFactory.CreateBinding(), "/onvif/events_service");
    serviceBuilder.AddService<OnvifService.Onvif.SubscriptionManagerImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.SubscriptionManagerImpl, SharpOnvifServer.Events.SubscriptionManager>(OnvifBindingFactory.CreateBinding(), "/onvif/events_service");

    // TODO: add more service endpoints
});

app.MapControllers();

app.Run();