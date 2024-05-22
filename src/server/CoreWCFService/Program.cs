using System.Net;
using CoreWCFService.Onvif;
using CoreWCFService.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

const string SCHEME_DIGEST = "Digest";

builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services
    .AddAuthentication(SCHEME_DIGEST)
    .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, null);

var app = builder.Build();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    //if (context.Request.Path.StartsWithSegments("/Service.svc"))
    //{
        // Check if currently authenticated
        var authResult = await context.AuthenticateAsync(SCHEME_DIGEST);
        if (authResult.None)
        {
            // If the client hasn't authenticated, send a challenge to the client and complete request
            await context.ChallengeAsync(SCHEME_DIGEST);
            return;
        }
    //}
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
    serviceBuilder.AddServiceEndpoint<OnvifDeviceMgmtImpl, Device>(binding, "/device_service");
    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
