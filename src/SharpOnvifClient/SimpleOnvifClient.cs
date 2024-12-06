using SharpOnvifClient.DeviceMgmt;
using SharpOnvifClient.Events;
using SharpOnvifClient.Media;
using SharpOnvifClient.PTZ;
using SharpOnvifClient.Security;
using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace SharpOnvifClient
{
    public class SimpleOnvifClient
    {
        private readonly string _onvifUri;
        private readonly DigestBehavior _auth;
        private Dictionary<string, string> _supportedServices;

        public SimpleOnvifClient(string onvifUri, string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(onvifUri))
                throw new ArgumentNullException(nameof(onvifUri));

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("User name or password must not be empty!");

            _onvifUri = onvifUri;
            _auth = new DigestBehavior(userName, password);
        }

        public void SetCameraUtcNowOffset(TimeSpan utcNowOffset)
        {
            _auth.UtcNowOffset = utcNowOffset;
        }

        public static void SetAuthentication(ServiceEndpoint endpoint, IEndpointBehavior authenticationBehavior)
        {
            if (!endpoint.EndpointBehaviors.Contains(authenticationBehavior))
            {
                endpoint.EndpointBehaviors.Add(authenticationBehavior);
            }
        }

        #region PTZ

        public async Task<PTZStatus> GetStatusAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                var status = await ptzClient.GetStatusAsync(
                    profileToken).ConfigureAwait(false);

                return status;
            }
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
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
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
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
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
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                await ptzClient.ContinuousMoveAsync(
                    profileToken,
                    new PTZ.PTZSpeed()
                    {
                        PanTilt = speedPanTilt,
                        Zoom = speedZoom
                    },
                    timeout).ConfigureAwait(false);
            }
        }

        public async Task<GetPresetsResponse> GetPresetsAsync(string profileToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                var presets = await ptzClient.GetPresetsAsync(
                    profileToken).ConfigureAwait(false);
                return presets;
            }
        }

        public async Task GoToPresetAsync(string profileToken, string presetToken, float panSpeed, float tiltSpeed, float zoomSpeed)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                await ptzClient.GotoPresetAsync(
                    profileToken,
                    presetToken,
                    new PTZ.PTZSpeed()
                    {
                        PanTilt = new PTZ.Vector2D() { x = panSpeed, y = tiltSpeed },
                        Zoom = new PTZ.Vector1D() { x = zoomSpeed }
                    }).ConfigureAwait(false);
            }
        }

        public async Task<string> SetPresetAsync(string profileToken, string presetName)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                
                var result = await ptzClient.SetPresetAsync(
                    new SetPresetRequest(profileToken, presetName, null)
                ).ConfigureAwait(false);

                return result.PresetToken;
            }
        }

        public async Task RemovePresetAsync(string profileToken, string presetToken)
        {
            string ptzURL = await GetServiceUriAsync(OnvifServices.PTZ).ConfigureAwait(false);
            using (var ptzClient = new PTZClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(ptzURL)))
            {
                SetAuthentication(ptzClient.Endpoint, _auth);
                await ptzClient.RemovePresetAsync(
                    profileToken,
                    presetToken).ConfigureAwait(false);
            }
        }

        #endregion PTZ

        #region Device Management

        public async Task<GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            using (var deviceClient = new DeviceClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(_onvifUri)))
            {
                SetAuthentication(deviceClient.Endpoint, _auth);
                var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest()).ConfigureAwait(false);
                return deviceInfo;
            }
        }

        public async Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            using (var deviceClient = new DeviceClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(_onvifUri)))
            {
                SetAuthentication(deviceClient.Endpoint, _auth);
                var services = await deviceClient.GetServicesAsync(includeCapability).ConfigureAwait(false);
                return services;
            }
        }

        public async Task<SystemDateTime> GetSystemDateAndTimeAsync()
        {
            using (var deviceClient = new DeviceClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(_onvifUri)))
            {
                SetAuthentication(deviceClient.Endpoint, _auth);
                var cameraTime = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false);
                return cameraTime;
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
            string mediaUrl = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            using (var mediaClient = new MediaClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(mediaUrl)))
            {
                SetAuthentication(mediaClient.Endpoint, _auth);
                var profiles = await mediaClient.GetProfilesAsync().ConfigureAwait(false);
                return profiles;
            }
        }

        public async Task<MediaUri> GetStreamUriAsync(string profileToken)
        {
            string mediaUrl = await GetServiceUriAsync(OnvifServices.MEDIA).ConfigureAwait(false);
            using (var mediaClient = new MediaClient(
                OnvifBindingFactory.CreateBinding(),
                new System.ServiceModel.EndpointAddress(mediaUrl)))
            {
                SetAuthentication(mediaClient.Endpoint, _auth);
                var streamUri = await mediaClient.GetStreamUriAsync(new StreamSetup() { Transport = new Transport() {  Protocol = TransportProtocol.RTSP } }, profileToken).ConfigureAwait(false);
                return streamUri;
            }
        }

        #endregion // Media

        #region Pull Point subscription

        public async Task<CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInMinutes = 5)
        {
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            using (EventPortTypeClient eventPortTypeClient = new EventPortTypeClient(
                            OnvifBindingFactory.CreateBinding(),
                            new System.ServiceModel.EndpointAddress(eventUri)))
            {
                SetAuthentication(eventPortTypeClient.Endpoint, _auth);

                var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                    new CreatePullPointSubscriptionRequest()
                    {
                        InitialTerminationTime = OnvifHelpers.GetTimeoutInMinutes(initialTerminationTimeInMinutes)
                    }).ConfigureAwait(false);

                Debug.WriteLine($"Pull Point subscription termination time: {subscribeResponse.TerminationTime}");

                return subscribeResponse;
            }
        }

        public async Task<PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            using (PullPointSubscriptionClient pullPointClient =
                new PullPointSubscriptionClient(
                    OnvifBindingFactory.CreateBinding(),
                    new System.ServiceModel.EndpointAddress(subscriptionReferenceAddress)))
            {
                SetAuthentication(pullPointClient.Endpoint, _auth);

                var messages = await pullPointClient.PullMessagesAsync(
                    new PullMessagesRequest(
                        OnvifHelpers.GetTimeoutInSeconds(timeoutInSeconds),
                        maxMessages,
                        Array.Empty<System.Xml.XmlElement>())).ConfigureAwait(false);

                return messages;
            }
        }

        public async Task<UnsubscribeResponse1> PullPointUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            using (PullPointSubscriptionClient pullPointClient =
                new PullPointSubscriptionClient(
                    OnvifBindingFactory.CreateBinding(),
                    new System.ServiceModel.EndpointAddress(subscriptionReferenceAddress)))
            {
                SetAuthentication(pullPointClient.Endpoint, _auth);
                var unsubscribeResponse = await pullPointClient.UnsubscribeAsync(new Unsubscribe()).ConfigureAwait(false);
                return unsubscribeResponse;
            }
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public async Task<SubscribeResponse1> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInMinutes = 5)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await GetServiceUriAsync(OnvifServices.EVENTS);
            using (NotificationProducerClient notificationProducerClient = new NotificationProducerClient(
                            OnvifBindingFactory.CreateBinding(),
                            new System.ServiceModel.EndpointAddress(eventUri)))
            {
                SetAuthentication(notificationProducerClient.Endpoint, _auth);

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
        }

        public async Task<RenewResponse1> BasicSubscriptionRenewAsync(string subscriptionReferenceAddress, int timeoutInMinutes = 5)
        {
            using (SubscriptionManagerClient subscriptionManagerClient = new SubscriptionManagerClient(
                            OnvifBindingFactory.CreateBinding(),
                            new System.ServiceModel.EndpointAddress(subscriptionReferenceAddress)))
            {
                SetAuthentication(subscriptionManagerClient.Endpoint, _auth);

                var renewResult = await subscriptionManagerClient.RenewAsync(new Renew()
                {
                    TerminationTime = OnvifHelpers.GetTimeoutInMinutes(timeoutInMinutes),
                }).ConfigureAwait(false);

                return renewResult;
            }
        }

        public async Task<UnsubscribeResponse1> BasicSubscriptionUnsubscribeAsync(string subscriptionReferenceAddress)
        {
            using (SubscriptionManagerClient subscriptionManagerClient = new SubscriptionManagerClient(
                            OnvifBindingFactory.CreateBinding(),
                            new System.ServiceModel.EndpointAddress(subscriptionReferenceAddress)))
            {
                SetAuthentication(subscriptionManagerClient.Endpoint, _auth);
                var unsubscribeResult = await subscriptionManagerClient.UnsubscribeAsync(new Unsubscribe()).ConfigureAwait(false);
                return unsubscribeResult;
            }
        }

        #endregion // Basic subscription

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
    }
}
