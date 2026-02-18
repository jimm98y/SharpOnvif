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

using SharpOnvifClient.Behaviors;
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifClient.Events;
using SharpOnvifClient.Media;
using SharpOnvifClient.PTZ;
using SharpOnvifClient.Security;
using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace SharpOnvifClient
{
    /// <summary>
    /// Simple Onvif client implements the basic Onvif device operations such as device information, media profiles, stream URI, PTZ operations and event subscriptions.
    /// </summary>
    public class SimpleOnvifClient : IDisposable
    {
        private bool _disposedValue;

        protected readonly string _onvifUri;
        public string OnvifUri {  get { return _onvifUri; } }

        protected Dictionary<string, string> _supportedServices;

        protected object _syncRoot = new object();
        protected readonly Dictionary<string, object> _clients = new Dictionary<string, object>();
        protected readonly System.Net.NetworkCredential _credentials;
        protected readonly DigestAuthenticationSchemeOptions _authentication;
        protected readonly IEndpointBehavior _legacyAuth;
        protected readonly IEndpointBehavior _disableExpect100ContinueBehavior;

        /// <summary>
        /// Creates an instance of <see cref="SimpleOnvifClient"/>.
        /// </summary>
        /// <param name="onvifUri">Onvif URI.</param>
        /// <param name="disableExpect100Continue">Disables the default Expect: 100-continue HTTP header.</param>
        public SimpleOnvifClient(string onvifUri, bool disableExpect100Continue = true) : this(onvifUri, null, null, new DigestAuthenticationSchemeOptions(DigestAuthentication.None), disableExpect100Continue)
        { }

        /// <summary>
        /// Creates an instance of <see cref="SimpleOnvifClient"/>.
        /// </summary>
        /// <param name="onvifUri">Onvif URI.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="disableExpect100Continue">Disables the default Expect: 100-continue HTTP header.</param>
        public SimpleOnvifClient(string onvifUri, string userName, string password, bool disableExpect100Continue = true) : this(onvifUri, userName, password, new DigestAuthenticationSchemeOptions(DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest), disableExpect100Continue)
        { }

        /// <summary>
        /// Creates an instance of <see cref="SimpleOnvifClient"/>.
        /// </summary>
        /// <param name="onvifUri">Onvif URI.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="authentication">Type of the authentication to use: <see cref="DigestAuthentication"/>.</param>
        /// <param name="disableExpect100Continue">Disables the default Expect: 100-continue HTTP header.</param>
        /// <exception cref="ArgumentNullException">Thrown when onvifUri is empty.</exception>
        public SimpleOnvifClient(string onvifUri, string userName, string password, DigestAuthenticationSchemeOptions authentication, bool disableExpect100Continue = true)
        {
            if (string.IsNullOrWhiteSpace(onvifUri))
                throw new ArgumentNullException(nameof(onvifUri));

            if(!onvifUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !onvifUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Onvif URI must start with http:// or https://");

            this._authentication = authentication ?? new DigestAuthenticationSchemeOptions();

            if (this._authentication.Authentication != DigestAuthentication.None)
            {
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("User name or password must not be empty!");

                _credentials = new System.Net.NetworkCredential(userName, password);

                if (this._authentication.Authentication.HasFlag(DigestAuthentication.WsUsernameToken))
                {
                    _legacyAuth = new WsUsernameTokenBehavior(_credentials);
                }
            }

            if (disableExpect100Continue)
            {
                _disableExpect100ContinueBehavior = new DisableExpect100ContinueBehavior();
            }

            _onvifUri = onvifUri;
        }

        public void SetCameraUtcNowOffset(TimeSpan utcNowOffset)
        {
            if (_authentication.Authentication.HasFlag(DigestAuthentication.WsUsernameToken))
            {
                ((IHasUtcOffset)_legacyAuth).UtcNowOffset = utcNowOffset;
            }
            else
            {
                throw new NotSupportedException("Time offset is only supported for WsUsernameToken authentication");
            }
        }

        protected TChannel GetOrCreateClient<TChannel>(string uri, Func<string, TChannel> creator) where TChannel : class
        {
            string key = $"{typeof(TChannel)}|{uri}";
            lock (_syncRoot)
            {
                if (_clients.ContainsKey(key))
                {
                    return (TChannel)_clients[key];
                }
                else
                {
                    var client = creator(uri);
                    DisableExpect100ContinueBehaviorExtensions.SetDisableExpect100Continue(client, _disableExpect100ContinueBehavior);
                    client = OnvifAuthenticationExtensions.SetOnvifAuthentication(client, _credentials, _authentication, _legacyAuth);
                    _clients.Add(key, client);
                    return client;
                }
            }
        }

        #region Device Management

        public async Task<GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            var deviceClient = GetOrCreateClient<Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest()).ConfigureAwait(false);
            return deviceInfo;
        }

        public virtual async Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            // PRE_AUTH action http://www.onvif.org/ver10/device/wsdl/GetServices
            if(_authentication.PreAuthActions == null || _authentication.PreAuthActions.Contains("http://www.onvif.org/ver10/device/wsdl/GetServices"))
            { 
                using (var deviceClient = new DeviceClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(_onvifUri)))
                {
                    deviceClient.SetDisableExpect100Continue(_disableExpect100ContinueBehavior);

                    var services = await deviceClient.GetServicesAsync(includeCapability).ConfigureAwait(false);
                    return services;
                }
            }
            else
            {
                // According to the Onvif Core specification, GetServices is in the PRE_AUTH category and should not require authentication.
                // However, some cameras (Vivotec) do not follow this specification and require authentication for GetServices. This method
                //  allows to get services with authentication if needed.
                var deviceClient = GetOrCreateClient<Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
                var services = await deviceClient.GetServicesAsync(new GetServicesRequest(includeCapability)).ConfigureAwait(false);
                return services;
            }
        }

        public async Task<SystemDateTime> GetSystemDateAndTimeAsync()
        {
            // PRE_AUTH action http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime
            if (_authentication.PreAuthActions == null || _authentication.PreAuthActions.Contains("http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime"))
            {
                using (var deviceClient = new DeviceClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(_onvifUri)))
                {
                    deviceClient.SetDisableExpect100Continue(_disableExpect100ContinueBehavior);

                    var cameraTime = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
                    return cameraTime;
                }
            }
            else
            {
                var deviceClient = GetOrCreateClient<Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
                var services = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
                return services;
            }
        }

        public async Task<System.DateTime> GetSystemDateAndTimeUtcAsync()
        {
            var cameraTime = await GetSystemDateAndTimeAsync().ConfigureAwait(false);
            var cameraDateTime = new System.DateTime(
                cameraTime.UTCDateTime.Date.Year,
                cameraTime.UTCDateTime.Date.Month,
                cameraTime.UTCDateTime.Date.Day,
                cameraTime.UTCDateTime.Time.Hour,
                cameraTime.UTCDateTime.Time.Minute,
                cameraTime.UTCDateTime.Time.Second,
                DateTimeKind.Utc
                );
            return cameraDateTime;
        }

        #endregion // Device Management

        #region Media

        public async Task<GetProfilesResponse> GetProfilesAsync()
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media.Media>(mediaUri, (u) => new MediaClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var profiles = await mediaClient.GetProfilesAsync(new GetProfilesRequest()).ConfigureAwait(false);
            return profiles;
        }

        public async Task<MediaUri> GetStreamUriAsync(string profileToken, TransportProtocol protocol = TransportProtocol.RTSP)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media.Media>(mediaUri, (u) => new MediaClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var streamUri = await mediaClient.GetStreamUriAsync(new StreamSetup() { Transport = new Transport() {  Protocol = protocol } }, profileToken).ConfigureAwait(false);
            return streamUri;
        }

        public async Task<MediaUri> GetSnapshotUriAsync(string profileToken)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media.Media>(mediaUri, (u) => new MediaClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var streamUri = await mediaClient.GetSnapshotUriAsync(profileToken).ConfigureAwait(false);
            return streamUri;
        }

        #endregion // Media

        #region Media2

        public async Task<Media2.GetProfilesResponse> GetProfiles2Async()
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media2.Media2>(mediaUri, (u) => new Media2.Media2Client(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var profiles = await mediaClient.GetProfilesAsync(new Media2.GetProfilesRequest()).ConfigureAwait(false);
            return profiles;
        }

        public async Task<Media2.GetStreamUriResponse> GetStreamUri2Async(string profileToken, string protocol)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media2.Media2>(mediaUri, (u) => new Media2.Media2Client(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var streamUri = await mediaClient.GetStreamUriAsync(new Media2.GetStreamUriRequest(protocol, profileToken)).ConfigureAwait(false);
            return streamUri;
        }

        public async Task<Media2.GetSnapshotUriResponse> GetSnapshotUri2Async(string profileToken)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<Media2.Media2>(mediaUri, (u) => new Media2.Media2Client(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var streamUri = await mediaClient.GetSnapshotUriAsync(new Media2.GetSnapshotUriRequest(profileToken)).ConfigureAwait(false);
            return streamUri;
        }

        #endregion // Media2

        #region Pull Point subscription

        public async Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInSeconds = 60)
        {
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var eventPortTypeClient = GetOrCreateClient<EventPortType>(eventUri, (u) => new EventPortTypeClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                new CreatePullPointSubscriptionRequest()
                {
                    InitialTerminationTime = OnvifHelpers.GetTimeoutInSeconds(initialTerminationTimeInSeconds)
                }).ConfigureAwait(false);
            return subscribeResponse;
        }

        public async Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            var pullPointClient = GetOrCreateClient<PullPointSubscription>(subscriptionReferenceAddress, (u) => new PullPointSubscriptionClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var messages = await pullPointClient.PullMessagesAsync(
                new PullMessagesRequest(
                    OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                    maxMessages,
                    Array.Empty<System.Xml.XmlElement>())).ConfigureAwait(false);
            return messages;
        }

        public async Task<UnsubscribeResponse1> PullPointUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var pullPointClient = GetOrCreateClient<PullPointSubscription>(subscriptionReferenceAddress, (u) => new PullPointSubscriptionClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var unsubscribeResponse = await pullPointClient.UnsubscribeAsync(new UnsubscribeRequest(new Unsubscribe())).ConfigureAwait(false);
            return unsubscribeResponse;
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public async Task<SubscribeResponse1> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInSeconds = 60)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var notificationProducerClient = GetOrCreateClient<NotificationProducer>(eventUri, (u) => new NotificationProducerClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var subscriptionResult = await notificationProducerClient.SubscribeAsync(new SubscribeRequest(new Subscribe()
            {
                InitialTerminationTime = OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                ConsumerReference = new EndpointReferenceType()
                {
                    Address = new AttributedURIType()
                    {
                        Value = onvifEventListenerUri
                    }
                }
            })).ConfigureAwait(false);
            return subscriptionResult;
        }

        public async Task<RenewResponse1> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60)
        {
            var subscriptionManagerClient = GetOrCreateClient<SubscriptionManager>(subscriptionReferenceAddress, (u) => new SubscriptionManagerClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var renewResult = await subscriptionManagerClient.RenewAsync(new RenewRequest(new Renew()
            {
                TerminationTime = OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
            })).ConfigureAwait(false);
            return renewResult;
        }

        public async Task<UnsubscribeResponse1> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var subscriptionManagerClient = GetOrCreateClient<SubscriptionManager>(subscriptionReferenceAddress, (u) => new SubscriptionManagerClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var unsubscribeResult = await subscriptionManagerClient.UnsubscribeAsync(new UnsubscribeRequest(new Unsubscribe())).ConfigureAwait(false);
            return unsubscribeResult;
        }

        #endregion // Basic subscription

        #region PTZ

        public async Task<PTZStatus> GetStatusAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var status = await ptzClient.GetStatusAsync(profileToken).ConfigureAwait(false);
            return status;
        }

        public Task AbsoluteMoveAsync(string profileToken, float zoom, float zoomSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                null,
                new PTZ.Vector1D() { x = zoom },
                null,
                new PTZ.Vector1D() { x = zoomSpeed }
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = pan, y = tilt },
                null,
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                null
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = pan, y = tilt },
                new PTZ.Vector1D() { x = zoom },
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                new PTZ.Vector1D() { x = zoomSpeed }
            );
        }

        private async Task AbsoluteMoveAsync(string profileToken, PTZ.Vector2D vectorPanTilt, PTZ.Vector1D vectorZoom, PTZ.Vector2D speedPanTilt, PTZ.Vector1D speedZoom)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.AbsoluteMoveAsync(
                profileToken,
                new PTZVector()
                {
                    PanTilt = vectorPanTilt,
                    Zoom = vectorZoom
                },
                new PTZ.PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                }).ConfigureAwait(false);
        }

        public Task RelativeMoveAsync(string profileToken, float zoom, float zoomSpeed)
        {
            return RelativeMoveAsync(
                profileToken,
                null,
                new PTZ.Vector1D() { x = zoom },
                null,
                new PTZ.Vector1D() { x = zoomSpeed }
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return RelativeMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = pan, y = tilt },
                null,
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                null
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return RelativeMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = pan, y = tilt },
                new PTZ.Vector1D() { x = zoom },
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                new PTZ.Vector1D() { x = zoomSpeed }
            );
        }

        private async Task RelativeMoveAsync(string profileToken, PTZ.Vector2D vectorPanTilt, PTZ.Vector1D vectorZoom, PTZ.Vector2D speedPanTilt, PTZ.Vector1D speedZoom)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.RelativeMoveAsync(
                profileToken,
                new PTZVector()
                {
                    PanTilt = vectorPanTilt,
                    Zoom = vectorZoom
                },
                new PTZ.PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                }).ConfigureAwait(false);
        }

        public Task ContinuousMoveAsync(string profileToken, float zoomSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(
                profileToken,
                null,
                new PTZ.Vector1D() { x = zoomSpeed },
                timeout);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                null,
                timeout);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(
                profileToken,
                new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                new PTZ.Vector1D() { x = zoomSpeed },
                timeout);
        }

        private async Task ContinuousMoveAsync(string profileToken, PTZ.Vector2D speedPanTilt, PTZ.Vector1D speedZoom, string timeout = null)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.ContinuousMoveAsync(new ContinuousMoveRequest(
                profileToken,
                new PTZ.PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                },
                timeout)).ConfigureAwait(false);
        }

        public async Task<GetPresetsResponse> GetPresetsAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var presets = await ptzClient.GetPresetsAsync(new GetPresetsRequest(profileToken)).ConfigureAwait(false);
            return presets;
        }

        public async Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.GotoPresetAsync(
                profileToken,
                presetToken,
                new PTZ.PTZSpeed()
                {
                    PanTilt = new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                    Zoom = new PTZ.Vector1D() { x = zoomSpeed }
                }).ConfigureAwait(false);
        }

        public async Task<string> SetPresetAsync(string profileToken, string presetName)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            var result = await ptzClient.SetPresetAsync(new SetPresetRequest(profileToken, presetName, null)).ConfigureAwait(false);
            return result.PresetToken;
        }

        public async Task RemovePresetAsync(string profileToken, string presetToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.RemovePresetAsync(profileToken, presetToken).ConfigureAwait(false);
        }

        public async Task StopAsync(string profileToken, bool panTilt = true, bool zoom = true)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(_onvifUri), new EndpointAddress(u)));
            await ptzClient.StopAsync(profileToken, panTilt, zoom).ConfigureAwait(false);
        }

        #endregion PTZ

        #region Utility

        protected virtual async Task<string> GetServiceUriAsync(string ns)
        {
            if (_supportedServices == null)
            {
                GetServicesResponse services = await GetServicesAsync().ConfigureAwait(false);
                Dictionary<string, string> supportedServices = new Dictionary<string, string>();
                foreach (var service in services.Service)
                {
                    supportedServices.Add(service.Namespace.ToLowerInvariant(), service.XAddr);
                }

                _supportedServices = supportedServices;
            }

            string uri;
            if (_supportedServices.TryGetValue(ns, out uri))
                return uri;
            else
                throw new NotSupportedException($"The device does not support {ns} service!");
        }

        #endregion // Utility

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    var clients = _clients.Values.ToArray();
                    foreach (var client in clients)
                    {
                        var disposableClient = client as IDisposable;
                        if (disposableClient != null)
                        {
                            disposableClient.Dispose();
                        }
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable
    }
}
