using System.Net;
using CoreWCFService.onvif;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static CoreWCFService.HttpRequestExtensions;

var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services
    .AddAuthentication("Digest")
    .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>("Digest", null);

var app = builder.Build();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    // Only check for basic auth when path is for the TransportWithMessageCredential endpoint only
    if (context.Request.Path.StartsWithSegments("/Service.svc"))
    {
        // Check if currently authenticated
        var authResult = await context.AuthenticateAsync("Digest");
        if (authResult.None)
        {
            // If the client hasn't authenticated, send a challenge to the client and complete request
            await context.ChallengeAsync("Digest");
            return;
        }
    }
    // Call the next delegate/middleware in the pipeline.
    // Either the request was authenticated of it's for a path which doesn't require basic auth
    await next(context);
});


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
    serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(binding, "/Service.svc");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
