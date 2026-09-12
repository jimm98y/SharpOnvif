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

using SharpOnvifClient.DeviceMgmt;
using SharpOnvifClient.Events;
using SharpOnvifClient.Media;
using SharpOnvifClient.PTZ;
using SharpOnvifClient.Security;
using SharpOnvifCommon;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpOnvifCommon.Onvif;

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

        /// <summary>
        /// Shared by every service client this instance creates, so the HTTP Digest challenge is
        /// negotiated once per endpoint rather than once per call.
        /// </summary>
        protected readonly OnvifClientSettings _settings;

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
            }

            _settings = new OnvifClientSettings
            {
                Credentials = _credentials,
                Authentication = this._authentication,
                DisableExpect100Continue = disableExpect100Continue,
            };

            _onvifUri = onvifUri;
        }

        /// <summary>
        /// Compensates for a camera whose clock is wrong.
        /// <para>
        /// A device rejects a WS-UsernameToken whose Created time drifts too far from its own
        /// clock. Setting the observed difference here makes authentication succeed without
        /// changing the camera.
        /// </para>
        /// </summary>
        public void SetCameraUtcNowOffset(TimeSpan utcNowOffset)
        {
            if (!_authentication.Authentication.HasFlag(DigestAuthentication.WsUsernameToken))
                throw new NotSupportedException("Time offset is only supported for WsUsernameToken authentication");

            _authentication.UtcNowOffset = utcNowOffset;
        }

        /// <summary>
        /// Returns the client for a service endpoint, creating it on first use. Clients are pooled
        /// because each holds the authentication state negotiated with its endpoint.
        /// </summary>
        protected TClient GetOrCreateClient<TClient>(string uri, Func<string, TClient> creator) where TClient : class
        {
            string key = $"{typeof(TClient)}|{uri}";
            lock (_syncRoot)
            {
                object existing;
                if (_clients.TryGetValue(key, out existing))
                    return (TClient)existing;

                TClient client = creator(uri);
                _clients.Add(key, client);
                return client;
            }
        }

        #region Device Management

        public async Task<GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest()).ConfigureAwait(false);
            return deviceInfo;
        }

        /// <summary>
        /// Lists the services the device supports.
        /// </summary>
        /// <remarks>
        /// The Onvif core specification puts GetServices in the PRE_AUTH category, so the client
        /// sends no WS-UsernameToken for it. Some cameras, Vivotek among them, demand
        /// authentication anyway; removing the action from
        /// <see cref="SharpOnvifCommon.Security.OnvifAuthenticationSettings.PreAuthActions"/>
        /// makes the client authenticate it like any other call.
        /// </remarks>
        public virtual async Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var services = await deviceClient.GetServicesAsync(new GetServicesRequest(includeCapability)).ConfigureAwait(false);
            return services;
        }

        /// <summary>
        /// Reads the device's clock. Also a PRE_AUTH action, and useful for discovering the
        /// offset that <see cref="SetCameraUtcNowOffset"/> needs when a camera's clock is wrong.
        /// </summary>
        public async Task<SystemDateTime> GetSystemDateAndTimeAsync()
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var cameraTime = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
            return cameraTime.SystemDateAndTime;
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
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var profiles = await mediaClient.GetProfilesAsync(new GetProfilesRequest()).ConfigureAwait(false);
            return profiles;
        }

        public async Task<MediaUri> GetStreamUriAsync(string profileToken, TransportProtocol protocol = TransportProtocol.RTSP)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var streamUri = await mediaClient.GetStreamUriAsync(new StreamSetup() { Transport = new Transport() { Protocol = protocol } }, profileToken).ConfigureAwait(false);
            return streamUri.MediaUri;
        }

        public async Task<MediaUri> GetSnapshotUriAsync(string profileToken)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var streamUri = await mediaClient.GetSnapshotUriAsync(profileToken).ConfigureAwait(false);
            return streamUri.MediaUri;
        }

        #endregion // Media

        #region Media2

        public async Task<Media2.GetProfilesResponse> GetProfiles2Async()
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var profiles = await mediaClient.GetProfilesAsync(new Media2.GetProfilesRequest()).ConfigureAwait(false);
            return profiles;
        }

        public async Task<Media2.GetStreamUriResponse> GetStreamUri2Async(string profileToken, string protocol)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var streamUri = await mediaClient.GetStreamUriAsync(new Media2.GetStreamUriRequest(protocol, profileToken)).ConfigureAwait(false);
            return streamUri;
        }

        public async Task<Media2.GetSnapshotUriResponse> GetSnapshotUri2Async(string profileToken)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA2).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var streamUri = await mediaClient.GetSnapshotUriAsync(new Media2.GetSnapshotUriRequest(profileToken)).ConfigureAwait(false);
            return streamUri;
        }

        #endregion // Media2

        #region Pull Point subscription

        public async Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInSeconds = 60)
        {
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var eventPortTypeClient = GetOrCreateClient(eventUri, u => new EventPortTypeClient(u, _settings));
            var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                new CreatePullPointSubscriptionRequest()
                {
                    InitialTerminationTime = OnvifHelpers.GetTimeoutInSeconds(initialTerminationTimeInSeconds)
                }).ConfigureAwait(false);
            return subscribeResponse;
        }

        public async Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            var pullPointClient = GetOrCreateClient(subscriptionReferenceAddress, u => new PullPointSubscriptionClient(u, _settings));
            var messages = await pullPointClient.PullMessagesAsync(
                new PullMessagesRequest(
                    OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                    maxMessages,
                    Array.Empty<System.Xml.XmlElement>())).ConfigureAwait(false);
            return messages;
        }

        public async Task<UnsubscribeResponse> PullPointUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var pullPointClient = GetOrCreateClient(subscriptionReferenceAddress, u => new PullPointSubscriptionClient(u, _settings));
            var unsubscribeResponse = await pullPointClient.UnsubscribeAsync(new UnsubscribeRequest()).ConfigureAwait(false);
            return unsubscribeResponse;
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public async Task<SubscribeResponse> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInSeconds = 60)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var notificationProducerClient = GetOrCreateClient(eventUri, u => new NotificationProducerClient(u, _settings));
            var subscriptionResult = await notificationProducerClient.SubscribeAsync(new SubscribeRequest()
            {
                InitialTerminationTime = OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                ConsumerReference = new EndpointReferenceType()
                {
                    Address = new AttributedURIType()
                    {
                        Value = onvifEventListenerUri
                    }
                }
            }).ConfigureAwait(false);
            return subscriptionResult;
        }

        public async Task<RenewResponse> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60)
        {
            var subscriptionManagerClient = GetOrCreateClient(subscriptionReferenceAddress, u => new SubscriptionManagerClient(u, _settings));
            var renewResult = await subscriptionManagerClient.RenewAsync(new RenewRequest()
            {
                TerminationTime = OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
            }).ConfigureAwait(false);
            return renewResult;
        }

        public async Task<UnsubscribeResponse> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var subscriptionManagerClient = GetOrCreateClient(subscriptionReferenceAddress, u => new SubscriptionManagerClient(u, _settings));
            var unsubscribeResult = await subscriptionManagerClient.UnsubscribeAsync(new UnsubscribeRequest()).ConfigureAwait(false);
            return unsubscribeResult;
        }

        #endregion // Basic subscription

        #region PTZ

        public async Task<PTZStatus> GetStatusAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var status = await ptzClient.GetStatusAsync(profileToken).ConfigureAwait(false);
            return status.PTZStatus;
        }

        public Task AbsoluteMoveAsync(string profileToken, float zoom, float zoomSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                null,
                new Vector1D() { x = zoom },
                null,
                new Vector1D() { x = zoomSpeed }
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                null,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                new Vector1D() { x = zoom },
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed }
            );
        }

        private async Task AbsoluteMoveAsync(string profileToken, Vector2D vectorPanTilt, Vector1D vectorZoom, Vector2D speedPanTilt, Vector1D speedZoom)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.AbsoluteMoveAsync(
                profileToken,
                new PTZVector()
                {
                    PanTilt = vectorPanTilt,
                    Zoom = vectorZoom
                },
                new PTZSpeed()
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
                new Vector1D() { x = zoom },
                null,
                new Vector1D() { x = zoomSpeed }
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return RelativeMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                null,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return RelativeMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                new Vector1D() { x = zoom },
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed }
            );
        }

        private async Task RelativeMoveAsync(string profileToken, Vector2D vectorPanTilt, Vector1D vectorZoom, Vector2D speedPanTilt, Vector1D speedZoom)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.RelativeMoveAsync(
                profileToken,
                new PTZVector()
                {
                    PanTilt = vectorPanTilt,
                    Zoom = vectorZoom
                },
                new PTZSpeed()
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
                new Vector1D() { x = zoomSpeed },
                timeout);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(
                profileToken,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null,
                timeout);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(
                profileToken,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed },
                timeout);
        }

        private async Task ContinuousMoveAsync(string profileToken, Vector2D speedPanTilt, Vector1D speedZoom, string timeout = null)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.ContinuousMoveAsync(new ContinuousMoveRequest(
                profileToken,
                new PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                },
                timeout)).ConfigureAwait(false);
        }

        public async Task<GetPresetsResponse> GetPresetsAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var presets = await ptzClient.GetPresetsAsync(new GetPresetsRequest(profileToken)).ConfigureAwait(false);
            return presets;
        }

        public async Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.GotoPresetAsync(
                profileToken,
                presetToken,
                new PTZSpeed()
                {
                    PanTilt = new Vector2D() { x = panSpeed, y = tiltSpeed },
                    Zoom = new Vector1D() { x = zoomSpeed }
                }).ConfigureAwait(false);
        }

        public async Task<string> SetPresetAsync(string profileToken, string presetName)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var result = await ptzClient.SetPresetAsync(new SetPresetRequest(profileToken, presetName, null)).ConfigureAwait(false);
            return result.PresetToken;
        }

        public async Task RemovePresetAsync(string profileToken, string presetToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.RemovePresetAsync(profileToken, presetToken).ConfigureAwait(false);
        }

        public async Task StopAsync(string profileToken, bool panTilt = true, bool zoom = true)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.StopAsync(profileToken, panTilt, zoom).ConfigureAwait(false);
        }

        public async Task<GetConfigurationsResponse> GetConfigurationsAsync()
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var configurations = await ptzClient.GetConfigurationsAsync(new GetConfigurationsRequest()).ConfigureAwait(false);
            return configurations;
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
