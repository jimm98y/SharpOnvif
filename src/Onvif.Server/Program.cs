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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer;
using SharpOnvifServer.Dispatch;

var builder = WebApplication.CreateBuilder();

builder.Services.AddControllers();

builder.Services.AddSingleton<SharpOnvifServer.IUserRepository, OnvifService.Repository.UserRepository>();
builder.Services.AddOnvifDigestAuthentication(builder.Configuration.GetSection("DigestAuthenticationOptions"));
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

const string URI_DEVICE_SERVICE = "/onvif/device_service";
const string URI_EVENTS_SERVICE = "/onvif/events_service";
const string URI_EVENTS_SUBSCRIPTION = "/onvif/Events/Subscription";
const string URI_EVENTS_PULLPOINT_SUBSCRIPTION = "/onvif/Events/PullPointSubscription";

// Every service is published on the one address a real device uses. Requests are routed by
// their SOAP action, so several services can share a URL - which is how Onvif devices behave,
// and something the CoreWCF bindings this replaces could not express.
app.MapOnvifService<OnvifService.Onvif.DeviceImpl>(URI_DEVICE_SERVICE);
app.MapOnvifService<OnvifService.Onvif.MediaImpl>(URI_DEVICE_SERVICE);
app.MapOnvifService<OnvifService.Onvif.PTZImpl>(URI_DEVICE_SERVICE);

// The subscription managers keep their own addresses: Onvif hands a client a reference like
// /onvif/Events/PullPointSubscription/aV9xN2sMv1Qb0Zt8/, and MapOnvifService routes that segment
// through to the implementation.
app.MapOnvifService<OnvifService.Onvif.EventsImpl>(URI_EVENTS_SERVICE);
app.MapOnvifService<OnvifService.Onvif.RouterSubscriptionManagerImpl>(URI_EVENTS_SUBSCRIPTION);
app.MapOnvifService<OnvifService.Onvif.RouterPullPointSubscriptionManagerImpl>(URI_EVENTS_PULLPOINT_SUBSCRIPTION);

app.MapControllers();

app.Run();
