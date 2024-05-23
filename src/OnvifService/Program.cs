using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OnvifService.Repository;
using SharpOnvifServer;
using SharpOnvifServer.Security;

var builder = WebApplication.CreateBuilder();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();

builder.Services.AddOnvifDigestAuthentication();
builder.Services.AddOnvifDiscovery();

var app = builder.Build();
app.UseAuthentication();

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<OnvifService.Onvif.OnvifDeviceMgmtImpl>();
    serviceBuilder.AddServiceEndpoint<OnvifService.Onvif.OnvifDeviceMgmtImpl, OnvifScDeviceMgmt.Device>(OnvifHelpers.CreateOnvifBinding(), "/device_service");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
