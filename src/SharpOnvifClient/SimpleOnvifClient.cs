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
    public class SimpleOnvifClient : IDisposable
    {
        private bool _disposedValue;

        private readonly string _onvifUri;
        private Dictionary<string, string> _supportedServices;

        private object _syncRoot = new object();
        private readonly Dictionary<string, ICommunicationObject> _clients = new Dictionary<string, ICommunicationObject>();
        private readonly System.Net.NetworkCredential _credentials;
        private readonly OnvifAuthentication _authentication;
        private readonly IEndpointBehavior _legacyAuth;
        private readonly IEndpointBehavior _disableExpect100ContinueBehavior;

        public SimpleOnvifClient(string onvifUri, bool disableExpect100Continue = false) : this(onvifUri, null, null, OnvifAuthentication.None, disableExpect100Continue)
        { }

        public SimpleOnvifClient(string onvifUri, string userName, string password, bool disableExpect100Continue = false) : this(onvifUri, userName, password, OnvifAuthentication.WsUsernameToken | OnvifAuthentication.HttpDigest, disableExpect100Continue)
        { }

        public SimpleOnvifClient(string onvifUri, string userName, string password, OnvifAuthentication authentication, bool disableExpect100Continue = false)
        {
            if (string.IsNullOrWhiteSpace(onvifUri))
                throw new ArgumentNullException(nameof(onvifUri));

            if(authentication != OnvifAuthentication.None)
            {
                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                    throw new ArgumentNullException("User name or password must not be empty!");

                _credentials = new System.Net.NetworkCredential(userName, password);

                if (authentication.HasFlag(OnvifAuthentication.WsUsernameToken))
                {
                    _legacyAuth = new WsUsernameTokenBehavior(_credentials);
                }
            }

            if (disableExpect100Continue)
            {
                _disableExpect100ContinueBehavior = new DisableExpect100ContinueBehavior();
            }

            _onvifUri = onvifUri;
            _authentication = authentication;
        }

        public void SetCameraUtcNowOffset(TimeSpan utcNowOffset)
        {
            if (_authentication.HasFlag(OnvifAuthentication.WsUsernameToken))
            {
                ((IHasUtcOffset)_legacyAuth).UtcNowOffset = utcNowOffset;
            }
            else
            {
                throw new NotSupportedException("Time offset is only supported for WsUsernameToken authentication");
            }
        }

        public TClient GetOrCreateClient<TClient, TChannel>(string uri, Func<string, TClient> creator) where TClient : ClientBase<TChannel> where TChannel : class
        {
            string key = $"{typeof(TClient)}|{uri}";
            lock (_syncRoot)
            {
                if (_clients.ContainsKey(key))
                {
                    return (TClient)_clients[key];
                }
                else
                {
                    var client = creator(uri);
                    client.SetOnvifAuthentication(_authentication, _credentials, _legacyAuth);
                    client.SetDisableExpect100Continue(_disableExpect100ContinueBehavior);
                    _clients.Add(key, client);
                    return client;
                }
            }
        }

        #region Device Management

        public async Task<GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            var deviceClient = GetOrCreateClient<DeviceClient, Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest()).ConfigureAwait(false);
            return deviceInfo;
        }

        public async Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            var deviceClient = GetOrCreateClient<DeviceClient, Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var services = await deviceClient.GetServicesAsync(includeCapability).ConfigureAwait(false);
            return services;
        }

        public async Task<SystemDateTime> GetSystemDateAndTimeAsync()
        {
            var deviceClient = GetOrCreateClient<DeviceClient, Device>(_onvifUri, (u) => new DeviceClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var cameraTime = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
            return cameraTime;
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
            var mediaClient = GetOrCreateClient<MediaClient, Media.Media>(mediaUri, (u) => new MediaClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var profiles = await mediaClient.GetProfilesAsync().ConfigureAwait(false);
            return profiles;
        }

        public async Task<MediaUri> GetStreamUriAsync(string profileToken)
        {
            string mediaUri = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            var mediaClient = GetOrCreateClient<MediaClient, Media.Media>(mediaUri, (u) => new MediaClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var streamUri = await mediaClient.GetStreamUriAsync(new StreamSetup() { Transport = new Transport() {  Protocol = TransportProtocol.RTSP } }, profileToken).ConfigureAwait(false);
            return streamUri;
        }

        #endregion // Media

        #region Pull Point subscription

        public async Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInMinutes = 5)
        {
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var eventPortTypeClient = GetOrCreateClient<EventPortTypeClient, EventPortType>(eventUri, (u) => new EventPortTypeClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                new CreatePullPointSubscriptionRequest()
                {
                    InitialTerminationTime = OnvifHelpers.GetTimeoutInMinutes(initialTerminationTimeInMinutes)
                }).ConfigureAwait(false);
            return subscribeResponse;
        }

        public async Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            var pullPointClient = GetOrCreateClient<PullPointSubscriptionClient, PullPointSubscription>(subscriptionReferenceAddress, (u) => new PullPointSubscriptionClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var messages = await pullPointClient.PullMessagesAsync(
                new PullMessagesRequest(
                    OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                    maxMessages,
                    Array.Empty<System.Xml.XmlElement>())).ConfigureAwait(false);
            return messages;
        }

        public async Task<UnsubscribeResponse1> PullPointUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var pullPointClient = GetOrCreateClient<PullPointSubscriptionClient, PullPointSubscription>(subscriptionReferenceAddress, (u) => new PullPointSubscriptionClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var unsubscribeResponse = await pullPointClient.UnsubscribeAsync(new Unsubscribe()).ConfigureAwait(false);
            return unsubscribeResponse;
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public async Task<SubscribeResponse1> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInMinutes = 5)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            var notificationProducerClient = GetOrCreateClient<NotificationProducerClient, NotificationProducer>(eventUri, (u) => new NotificationProducerClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var subscriptionResult = await notificationProducerClient.SubscribeAsync(new Subscribe()
            {
                InitialTerminationTime = OnvifHelpers.GetTimeoutInMinutes(timeoutInMinutes),
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

        public async Task<RenewResponse1> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInMinutes = 5)
        {
            var subscriptionManagerClient = GetOrCreateClient<SubscriptionManagerClient, SubscriptionManager>(subscriptionReferenceAddress, (u) => new SubscriptionManagerClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var renewResult = await subscriptionManagerClient.RenewAsync(new Renew()
            {
                TerminationTime = OnvifHelpers.GetTimeoutInMinutes(timeoutInMinutes),
            }).ConfigureAwait(false);
            return renewResult;
        }

        public async Task<UnsubscribeResponse1> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            var subscriptionManagerClient = GetOrCreateClient<SubscriptionManagerClient, SubscriptionManager>(subscriptionReferenceAddress, (u) => new SubscriptionManagerClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var unsubscribeResult = await subscriptionManagerClient.UnsubscribeAsync(new Unsubscribe()).ConfigureAwait(false);
            return unsubscribeResult;
        }

        #endregion // Basic subscription

        #region PTZ

        public async Task<PTZStatus> GetStatusAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
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
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
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
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
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
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            await ptzClient.ContinuousMoveAsync(
                profileToken,
                new PTZ.PTZSpeed()
                {
                    PanTilt = speedPanTilt,
                    Zoom = speedZoom
                },
                timeout).ConfigureAwait(false);
        }

        public async Task<GetPresetsResponse> GetPresetsAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var presets = await ptzClient.GetPresetsAsync(profileToken).ConfigureAwait(false);
            return presets;
        }

        public async Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
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
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            var result = await ptzClient.SetPresetAsync(new SetPresetRequest(profileToken, presetName, null)).ConfigureAwait(false);
            return result.PresetToken;
        }

        public async Task RemovePresetAsync(string profileToken, string presetToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            var ptzClient = GetOrCreateClient<PTZClient, PTZ.PTZ>(ptzURL, (u) => new PTZClient(OnvifBindingFactory.CreateBinding(), new EndpointAddress(u)));
            await ptzClient.RemovePresetAsync(profileToken, presetToken).ConfigureAwait(false);
        }

        #endregion PTZ

        #region Utility

        private async Task<string> GetServiceUriAsync(string ns)
        {
            if (_supportedServices == null)
            {
                var services = await GetServicesAsync().ConfigureAwait(false);
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
