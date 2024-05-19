using System.Net;
using CoreWCFService.onvif;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

var app = builder.Build();

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<OnvifDeviceMgmtImpl>();
    var httpTransportBinding = new HttpTransportBindingElement
    {
        AuthenticationScheme = AuthenticationSchemes.Anonymous
    };
    var textMessageEncodingBinding = new TextMessageEncodingBindingElement
    {
        MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
    };  
    var binding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
    serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(binding, "/Service.svc");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
