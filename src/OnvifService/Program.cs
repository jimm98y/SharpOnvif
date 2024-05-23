using System.Net;
using CoreWCFService.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OnvifScDeviceMgmt;
using OnvifService.Discovery;
using OnvifService.OnvifImpl;

const string SCHEME_DIGEST = "Digest";

var builder = WebApplication.CreateBuilder();
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
// all endpoints must have [DisableMustUnderstandValidation] for this to work
builder.Services
    .AddAuthentication(SCHEME_DIGEST)
    .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, null);
builder.Services.AddAuthorization(); // this means we require Digest on all endpoints
builder.Services.AddHostedService<DiscoveryService>(); // add Onvif discovery

var app = builder.Build();
app.UseAuthentication();

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<OnvifDeviceMgmtImpl>();
    var httpTransportBinding = new HttpTransportBindingElement
    {
        AuthenticationScheme = AuthenticationSchemes.Digest,
    };
    var textMessageEncodingBinding = new TextMessageEncodingBindingElement
    {
        MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None),
    };  
    var binding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
    serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(binding, "/device_service");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
