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
using SharpOnvifCommon;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public SimpleOnvifClient(string onvifUri, bool disableExpect100Continue = true) : this(onvifUri, null, null, new OnvifAuthenticationSettings(DigestAuthentication.None), disableExpect100Continue)
        { }

        /// <summary>
        /// Creates an instance of <see cref="SimpleOnvifClient"/>.
        /// </summary>
        /// <param name="onvifUri">Onvif URI.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="disableExpect100Continue">Disables the default Expect: 100-continue HTTP header.</param>
        public SimpleOnvifClient(string onvifUri, string userName, string password, bool disableExpect100Continue = true) : this(onvifUri, userName, password, new OnvifAuthenticationSettings(DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest), disableExpect100Continue)
        { }

        /// <summary>
        /// Creates an instance of <see cref="SimpleOnvifClient"/>.
        /// </summary>
        /// <param name="onvifUri">Onvif URI.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="authentication">How to authenticate, or null to send no credentials.</param>
        /// <param name="disableExpect100Continue">Disables the default Expect: 100-continue HTTP header.</param>
        /// <exception cref="ArgumentNullException">Thrown when onvifUri is empty.</exception>
        public SimpleOnvifClient(string onvifUri, string userName, string password, OnvifAuthenticationSettings authentication, bool disableExpect100Continue = true)
        {
            if (string.IsNullOrWhiteSpace(onvifUri))
                throw new ArgumentNullException(nameof(onvifUri));

            if(!onvifUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !onvifUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Onvif URI must start with http:// or https://");

            // Used as it was given, not copied. Copying would hand back a plain
            // OnvifAuthenticationSettings, quietly discarding whatever a caller derived from it -
            // and a generated client does not copy this either, so the two agree.
            authentication = authentication ?? new OnvifAuthenticationSettings();

            if (authentication.Options.Authentication != DigestAuthentication.None)
            {
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("User name or password must not be empty!");

                _credentials = new System.Net.NetworkCredential(userName, password);
            }

            _settings = new OnvifClientSettings
            {
                Credentials = _credentials,
                Authentication = authentication,
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
            // Only Onvif's own authentication stamps a time, and only the older scheme carries
            // one; anything else has nothing for an offset to correct.
            OnvifAuthenticationSettings onvif = _settings.Authentication as OnvifAuthenticationSettings;

            if (onvif == null || !onvif.Options.Offers(DigestAuthentication.WsUsernameToken))
                throw new NotSupportedException("Time offset is only supported for WsUsernameToken authentication");

            _settings.UtcNowOffset = utcNowOffset;
        }

        /// <summary>
        /// Returns the client for a service endpoint, creating it on first use. Clients are pooled
        /// because each holds the authentication state negotiated with its endpoint.
        /// </summary>
        protected TClient GetOrCreateClient<TClient>(string uri, Func<string, TClient> creator) where TClient : class
        {
            return GetOrCreateClient(uri, uri, creator);
        }

        /// <summary>
        /// The same, for a client that is pooled under something other than its address - a pull
        /// point, whose HTTP timeout depends on how long the caller asked the device to hold the
        /// request open.
        /// </summary>
        protected TClient GetOrCreateClient<TClient>(string poolKey, string uri, Func<string, TClient> creator)
            where TClient : class
        {
            string key = $"{typeof(TClient)}|{poolKey}";
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

        /// <summary>
        /// Where this client reports what it could not do. Null - the default - reports nowhere.
        /// </summary>
        /// <remarks>
        /// Per client, so two of them in one application can report to different places. Set it
        /// before the first call: the service clients underneath are made on first use and take
        /// the logger they are given then.
        /// </remarks>
        public ILog Logger
        {
            get { return _settings.Logger; }
            set { _settings.Logger = value; }
        }

        /// <summary>
        /// How much longer than a pull's own timeout the client waits for the reply, covering the
        /// round trip and a device that is a little late.
        /// </summary>
        public static TimeSpan PullMessagesHttpTimeoutHeadroom { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The HTTP timeout a pull of the given length needs.
        /// </summary>
        /// <remarks>
        /// A pull is a long poll: the device holds the request for up to
        /// <paramref name="pullTimeoutInSeconds"/> and answers sooner only if it has something to
        /// report. The HTTP timeout has to outlast that - with both at the default of 60 seconds,
        /// an idle camera answered exactly as the client gave up.
        /// </remarks>
        public static TimeSpan GetPullMessagesHttpTimeout(int pullTimeoutInSeconds, TimeSpan configuredTimeout)
        {
            TimeSpan needed = TimeSpan.FromSeconds(pullTimeoutInSeconds) + PullMessagesHttpTimeoutHeadroom;
            return needed > configuredTimeout ? needed : configuredTimeout;
        }

        /// <summary>The client's settings with a different HTTP timeout.</summary>
        private OnvifClientSettings WithHttpTimeout(TimeSpan timeout)
        {
            return new OnvifClientSettings(_settings)
            {
                Timeout = timeout,
            };
        }

        #region Device Management

        public Task<GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            return GetDeviceInformationAsync(CancellationToken.None);
        }

        public async Task<GetDeviceInformationResponse> GetDeviceInformationAsync(CancellationToken cancellationToken)
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest(), cancellationToken).ConfigureAwait(false);
            return deviceInfo;
        }

        /// <summary>
        /// Lists the services the device supports.
        /// </summary>
        /// <remarks>
        /// The Onvif core specification puts GetServices in the PRE_AUTH category, so the client
        /// sends no WS-UsernameToken for it. Some cameras, Vivotek among them, demand
        /// authentication anyway; removing the action from
        /// <see cref="SharpOnvifCommon.Security.OnvifAuthenticationOptions.PreAuthActions"/>
        /// makes the client authenticate it like any other call.
        /// </remarks>
        public virtual Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            return GetServicesAsync(includeCapability, CancellationToken.None);
        }

        /// <summary>
        /// Lists the services the device supports.
        /// </summary>
        /// <remarks>
        /// Overriding this one covers calls made with and without a token. Overriding the overload
        /// without one covers only those - which is all it covered before this one existed.
        /// </remarks>
        public virtual async Task<GetServicesResponse> GetServicesAsync(bool includeCapability, CancellationToken cancellationToken)
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var services = await deviceClient.GetServicesAsync(new GetServicesRequest(includeCapability), cancellationToken).ConfigureAwait(false);
            return services;
        }

        /// <summary>
        /// Reads the device's clock. Also a PRE_AUTH action, and useful for discovering the
        /// offset that <see cref="SetCameraUtcNowOffset"/> needs when a camera's clock is wrong.
        /// </summary>
        public Task<SystemDateTime> GetSystemDateAndTimeAsync()
        {
            return GetSystemDateAndTimeAsync(CancellationToken.None);
        }

        public async Task<SystemDateTime> GetSystemDateAndTimeAsync(CancellationToken cancellationToken)
        {
            var deviceClient = GetOrCreateClient(_onvifUri, u => new DeviceClient(u, _settings));
            var cameraTime = await deviceClient.GetSystemDateAndTimeAsync(cancellationToken).ConfigureAwait(false);
            return cameraTime.SystemDateAndTime;
        }

        public Task<System.DateTime> GetSystemDateAndTimeUtcAsync()
        {
            return GetSystemDateAndTimeUtcAsync(CancellationToken.None);
        }

        public async Task<System.DateTime> GetSystemDateAndTimeUtcAsync(CancellationToken cancellationToken)
        {
            var cameraTime = await GetSystemDateAndTimeAsync(cancellationToken).ConfigureAwait(false);
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

        public Task<GetProfilesResponse> GetProfilesAsync()
        {
            return GetProfilesAsync(CancellationToken.None);
        }

        public async Task<GetProfilesResponse> GetProfilesAsync(CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var profiles = await mediaClient.GetProfilesAsync(new GetProfilesRequest(), cancellationToken).ConfigureAwait(false);
            return profiles;
        }

        public Task<MediaUri> GetStreamUriAsync(string profileToken, TransportProtocol protocol = TransportProtocol.RTSP)
        {
            return GetStreamUriAsync(profileToken, protocol, CancellationToken.None);
        }

        public async Task<MediaUri> GetStreamUriAsync(string profileToken, TransportProtocol protocol, CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var streamUri = await mediaClient.GetStreamUriAsync(new StreamSetup() { Transport = new Transport() { Protocol = protocol } }, profileToken, cancellationToken).ConfigureAwait(false);
            return streamUri.MediaUri;
        }

        public Task<MediaUri> GetSnapshotUriAsync(string profileToken)
        {
            return GetSnapshotUriAsync(profileToken, CancellationToken.None);
        }

        public async Task<MediaUri> GetSnapshotUriAsync(string profileToken, CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new MediaClient(u, _settings));
            var streamUri = await mediaClient.GetSnapshotUriAsync(profileToken, cancellationToken).ConfigureAwait(false);
            return streamUri.MediaUri;
        }

        #endregion // Media

        #region Media2

        public Task<Media2.GetProfilesResponse> GetProfiles2Async()
        {
            return GetProfiles2Async(CancellationToken.None);
        }

        public async Task<Media2.GetProfilesResponse> GetProfiles2Async(CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA2, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var profiles = await mediaClient.GetProfilesAsync(new Media2.GetProfilesRequest(), cancellationToken).ConfigureAwait(false);
            return profiles;
        }

        public Task<Media2.GetStreamUriResponse> GetStreamUri2Async(string profileToken, string protocol)
        {
            return GetStreamUri2Async(profileToken, protocol, CancellationToken.None);
        }

        public async Task<Media2.GetStreamUriResponse> GetStreamUri2Async(string profileToken, string protocol, CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA2, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var streamUri = await mediaClient.GetStreamUriAsync(new Media2.GetStreamUriRequest(protocol, profileToken), cancellationToken).ConfigureAwait(false);
            return streamUri;
        }

        public Task<Media2.GetSnapshotUriResponse> GetSnapshotUri2Async(string profileToken)
        {
            return GetSnapshotUri2Async(profileToken, CancellationToken.None);
        }

        public async Task<Media2.GetSnapshotUriResponse> GetSnapshotUri2Async(string profileToken, CancellationToken cancellationToken)
        {
            string mediaUri = await ResolveServiceUriAsync(OnvifServices.MEDIA2, cancellationToken).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient(mediaUri, u => new Media2.Media2Client(u, _settings));
            var streamUri = await mediaClient.GetSnapshotUriAsync(new Media2.GetSnapshotUriRequest(profileToken), cancellationToken).ConfigureAwait(false);
            return streamUri;
        }

        #endregion // Media2

        #region Pull Point subscription

        public Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInSeconds = 60)
        {
            return PullPointSubscribeAsync(initialTerminationTimeInSeconds, CancellationToken.None);
        }

        public async Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInSeconds, CancellationToken cancellationToken)
        {
            string eventUri = await ResolveServiceUriAsync(OnvifServices.EVENTS, cancellationToken).ConfigureAwait(false);
            var eventPortTypeClient = GetOrCreateClient(eventUri, u => new EventPortTypeClient(u, _settings));
            var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                new CreatePullPointSubscriptionRequest()
                {
                    InitialTerminationTime = OnvifHelpers.GetTimeoutInSeconds(initialTerminationTimeInSeconds)
                }, cancellationToken).ConfigureAwait(false);
            return subscribeResponse;
        }

        /// <remarks>
        /// A pull is a long poll: the device holds the request open for up to
        /// <paramref name="timeoutInSeconds"/> waiting for something to report, and answers
        /// immediately if something arrives sooner. The HTTP timeout therefore has to outlast it,
        /// or an idle camera answers exactly on time and the client has already given up - which
        /// is what the default settings did, both being 60 seconds.
        /// </remarks>
        public Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            return PullPointPullMessagesAsync(subscriptionReferenceAddress, timeoutInSeconds, maxMessages, CancellationToken.None);
        }

        /// <remarks>
        /// The token is what ends a pull the device is still holding open, short of disposing
        /// the client.
        /// </remarks>
        public async Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds, int maxMessages, CancellationToken cancellationToken)
        {
            TimeSpan httpTimeout = GetPullMessagesHttpTimeout(timeoutInSeconds, _settings.Timeout);

            var pullPointClient = GetOrCreateClient(
                $"{subscriptionReferenceAddress}|{(int)httpTimeout.TotalSeconds}",
                subscriptionReferenceAddress,
                u => new PullPointSubscriptionClient(u, WithHttpTimeout(httpTimeout)));
            var messages = await pullPointClient.PullMessagesAsync(
                new PullMessagesRequest(
                    OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                    maxMessages,
                    Array.Empty<System.Xml.XmlElement>()), cancellationToken).ConfigureAwait(false);
            return messages;
        }

        public Task<UnsubscribeResponse> PullPointUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            return PullPointUnsubscribeAsync(subscriptionReferenceAddress, CancellationToken.None);
        }

        public async Task<UnsubscribeResponse> PullPointUnsubscribeAsync(string subscriptionReferenceAddress, CancellationToken cancellationToken)
        {
            var pullPointClient = GetOrCreateClient(subscriptionReferenceAddress, u => new PullPointSubscriptionClient(u, _settings));
            var unsubscribeResponse = await pullPointClient.UnsubscribeAsync(new UnsubscribeRequest(), cancellationToken).ConfigureAwait(false);
            return unsubscribeResponse;
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public Task<SubscribeResponse> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInSeconds = 60)
        {
            return BasicSubscribeAsync(onvifEventListenerUri, timeoutInSeconds, CancellationToken.None);
        }

        public async Task<SubscribeResponse> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInSeconds, CancellationToken cancellationToken)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await ResolveServiceUriAsync(OnvifServices.EVENTS, cancellationToken).ConfigureAwait(false);
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
            }, cancellationToken).ConfigureAwait(false);
            return subscriptionResult;
        }

        public Task<RenewResponse> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60)
        {
            return BasicSubscriptionRenewAsync(subscriptionReferenceAddress, timeoutInSeconds, CancellationToken.None);
        }

        public async Task<RenewResponse> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInSeconds, CancellationToken cancellationToken)
        {
            var subscriptionManagerClient = GetOrCreateClient(subscriptionReferenceAddress, u => new SubscriptionManagerClient(u, _settings));
            var renewResult = await subscriptionManagerClient.RenewAsync(new RenewRequest()
            {
                TerminationTime = OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
            }, cancellationToken).ConfigureAwait(false);
            return renewResult;
        }

        public Task<UnsubscribeResponse> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            return BasicSubscriptionUnsubscribeAsync(subscriptionReferenceAddress, CancellationToken.None);
        }

        public async Task<UnsubscribeResponse> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress, CancellationToken cancellationToken)
        {
            var subscriptionManagerClient = GetOrCreateClient(subscriptionReferenceAddress, u => new SubscriptionManagerClient(u, _settings));
            var unsubscribeResult = await subscriptionManagerClient.UnsubscribeAsync(new UnsubscribeRequest(), cancellationToken).ConfigureAwait(false);
            return unsubscribeResult;
        }

        #endregion // Basic subscription

        #region PTZ

        public Task<PTZStatus> GetStatusAsync(string profileToken)
        {
            return GetStatusAsync(profileToken, CancellationToken.None);
        }

        public async Task<PTZStatus> GetStatusAsync(string profileToken, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var status = await ptzClient.GetStatusAsync(profileToken, cancellationToken).ConfigureAwait(false);
            return status.PTZStatus;
        }

        public Task AbsoluteMoveAsync(string profileToken, float zoom, float zoomSpeed)
        {
            return AbsoluteMoveAsync(profileToken, zoom, zoomSpeed, CancellationToken.None);
        }

        public Task AbsoluteMoveAsync(string profileToken, float zoom, float zoomSpeed, CancellationToken cancellationToken)
        {
            return AbsoluteMoveAsync(
                profileToken,
                null,
                new Vector1D() { x = zoom },
                null,
                new Vector1D() { x = zoomSpeed },
                cancellationToken
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return AbsoluteMoveAsync(profileToken, pan, tilt, panSpeed, tiltSpeed, CancellationToken.None);
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed, CancellationToken cancellationToken)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                null,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null,
                cancellationToken
            );
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return AbsoluteMoveAsync(profileToken, pan, tilt, zoom, panSpeed, tiltSpeed, zoomSpeed, CancellationToken.None);
        }

        public Task AbsoluteMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed, CancellationToken cancellationToken)
        {
            return AbsoluteMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                new Vector1D() { x = zoom },
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed },
                cancellationToken
            );
        }

        private async Task AbsoluteMoveAsync(string profileToken, Vector2D vectorPanTilt, Vector1D vectorZoom, Vector2D speedPanTilt, Vector1D speedZoom, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
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
                },
                cancellationToken).ConfigureAwait(false);
        }

        public Task RelativeMoveAsync(string profileToken, float zoom, float zoomSpeed)
        {
            return RelativeMoveAsync(profileToken, zoom, zoomSpeed, CancellationToken.None);
        }

        public Task RelativeMoveAsync(string profileToken, float zoom, float zoomSpeed, CancellationToken cancellationToken)
        {
            return RelativeMoveAsync(
                profileToken,
                null,
                new Vector1D() { x = zoom },
                null,
                new Vector1D() { x = zoomSpeed },
                cancellationToken
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed)
        {
            return RelativeMoveAsync(profileToken, pan, tilt, panSpeed, tiltSpeed, CancellationToken.None);
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float panSpeed, float tiltSpeed, CancellationToken cancellationToken)
        {
            return RelativeMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                null,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null,
                cancellationToken
            );
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return RelativeMoveAsync(profileToken, pan, tilt, zoom, panSpeed, tiltSpeed, zoomSpeed, CancellationToken.None);
        }

        public Task RelativeMoveAsync(string profileToken, float pan, float tilt, float zoom, float panSpeed, float tiltSpeed, float zoomSpeed, CancellationToken cancellationToken)
        {
            return RelativeMoveAsync(
                profileToken,
                new Vector2D() { x = pan, y = tilt },
                new Vector1D() { x = zoom },
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed },
                cancellationToken
            );
        }

        private async Task RelativeMoveAsync(string profileToken, Vector2D vectorPanTilt, Vector1D vectorZoom, Vector2D speedPanTilt, Vector1D speedZoom, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
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
                },
                cancellationToken).ConfigureAwait(false);
        }

        public Task ContinuousMoveAsync(string profileToken, float zoomSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(profileToken, zoomSpeed, timeout, CancellationToken.None);
        }

        public Task ContinuousMoveAsync(string profileToken, float zoomSpeed, string timeout, CancellationToken cancellationToken)
        {
            return ContinuousMoveAsync(
                profileToken,
                null,
                new Vector1D() { x = zoomSpeed },
                timeout,
                cancellationToken);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(profileToken, panSpeed, tiltSpeed, timeout, CancellationToken.None);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, string timeout, CancellationToken cancellationToken)
        {
            return ContinuousMoveAsync(
                profileToken,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                null,
                timeout,
                cancellationToken);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string timeout = null)
        {
            return ContinuousMoveAsync(profileToken, panSpeed, tiltSpeed, zoomSpeed, timeout, CancellationToken.None);
        }

        public Task ContinuousMoveAsync(string profileToken, float panSpeed, float tiltSpeed, float zoomSpeed, string timeout, CancellationToken cancellationToken)
        {
            return ContinuousMoveAsync(
                profileToken,
                new Vector2D() { x = panSpeed, y = tiltSpeed },
                new Vector1D() { x = zoomSpeed },
                timeout,
                cancellationToken);
        }

        private async Task ContinuousMoveAsync(string profileToken, Vector2D speedPanTilt, Vector1D speedZoom, string timeout, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.ContinuousMoveAsync(new ContinuousMoveRequest(
                profileToken,
                new PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                },
                timeout), cancellationToken).ConfigureAwait(false);
        }

        public Task<GetPresetsResponse> GetPresetsAsync(string profileToken)
        {
            return GetPresetsAsync(profileToken, CancellationToken.None);
        }

        public async Task<GetPresetsResponse> GetPresetsAsync(string profileToken, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var presets = await ptzClient.GetPresetsAsync(new GetPresetsRequest(profileToken), cancellationToken).ConfigureAwait(false);
            return presets;
        }

        public Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            return GoToPresetAsync(profileToken, presetToken, panSpeed, tiltSpeed, zoomSpeed, CancellationToken.None);
        }

        public async Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.GotoPresetAsync(
                profileToken,
                presetToken,
                new PTZSpeed()
                {
                    PanTilt = new Vector2D() { x = panSpeed, y = tiltSpeed },
                    Zoom = new Vector1D() { x = zoomSpeed }
                },
                cancellationToken).ConfigureAwait(false);
        }

        public Task<string> SetPresetAsync(string profileToken, string presetName) => SetPresetAsync(profileToken, presetName, null, CancellationToken.None);

        public Task<string> SetPresetAsync(string profileToken, string presetName, CancellationToken cancellationToken) => SetPresetAsync(profileToken, presetName, null, cancellationToken);

        public Task<string> SetPresetAsync(string profileToken, string presetName, string presetToken) => SetPresetAsync(profileToken, presetName, presetToken, CancellationToken.None);

        public async Task<string> SetPresetAsync(string profileToken, string presetName, string presetToken, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var result = await ptzClient.SetPresetAsync(new SetPresetRequest(profileToken, presetName, presetToken), cancellationToken).ConfigureAwait(false);
            return result.PresetToken;
        }

        public Task RemovePresetAsync(string profileToken, string presetToken)
        {
            return RemovePresetAsync(profileToken, presetToken, CancellationToken.None);
        }

        public async Task RemovePresetAsync(string profileToken, string presetToken, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.RemovePresetAsync(profileToken, presetToken, cancellationToken).ConfigureAwait(false);
        }

        public Task StopAsync(string profileToken, bool panTilt = true, bool zoom = true)
        {
            return StopAsync(profileToken, panTilt, zoom, CancellationToken.None);
        }

        public async Task StopAsync(string profileToken, bool panTilt, bool zoom, CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            await ptzClient.StopAsync(profileToken, panTilt, zoom, cancellationToken).ConfigureAwait(false);
        }

        public Task<GetConfigurationsResponse> GetConfigurationsAsync()
        {
            return GetConfigurationsAsync(CancellationToken.None);
        }

        public async Task<GetConfigurationsResponse> GetConfigurationsAsync(CancellationToken cancellationToken)
        {
            string ptzURL = await ResolveServiceUriAsync(OnvifServices.PTZ, cancellationToken).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient(ptzURL, u => new PTZClient(u, _settings));
            var configurations = await ptzClient.GetConfigurationsAsync(new GetConfigurationsRequest(), cancellationToken).ConfigureAwait(false);
            return configurations;
        }

        #endregion PTZ

        #region Utility

        protected virtual Task<string> GetServiceUriAsync(string ns)
        {
            return GetServiceUriAsync(ns, CancellationToken.None);
        }

        /// <remarks>
        /// Overriding this one covers calls made with and without a token. Overriding the overload
        /// without one covers only those - which is all it covered before this one existed.
        /// </remarks>
        protected virtual async Task<string> GetServiceUriAsync(string ns, CancellationToken cancellationToken)
        {
            if (_supportedServices == null)
            {
                GetServicesResponse services = await (cancellationToken.CanBeCanceled
                    ? GetServicesAsync(false, cancellationToken)
                    : GetServicesAsync()).ConfigureAwait(false);
                Dictionary<string, string> supportedServices = new Dictionary<string, string>();
                foreach (var service in services.Service)
                {
                    if (service == null || string.IsNullOrEmpty(service.Namespace)) continue;

                    // Assigned, not added: devices do list a namespace more than once - the same
                    // service at two versions, say - and Add would throw and take every call that
                    // needs a service address down with it. The first address wins, which is the
                    // one the device put first.
                    string key = service.Namespace.ToLowerInvariant();
                    if (!supportedServices.ContainsKey(key))
                        supportedServices[key] = service.XAddr;
                }

                _supportedServices = supportedServices;
            }

            string uri;
            if (_supportedServices.TryGetValue(ns, out uri))
                return uri;
            else
                throw new NotSupportedException($"The device does not support {ns} service!");
        }

        /// <summary>
        /// Looks up a service address the way a call without a token always has, so that a
        /// subclass overriding only the token-less overloads keeps being called for those.
        /// </summary>
        private Task<string> ResolveServiceUriAsync(string ns, CancellationToken cancellationToken)
        {
            return cancellationToken.CanBeCanceled
                ? GetServiceUriAsync(ns, cancellationToken)
                : GetServiceUriAsync(ns);
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
