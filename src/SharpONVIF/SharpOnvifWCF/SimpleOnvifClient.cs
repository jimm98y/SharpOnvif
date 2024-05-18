using OnvifEvents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace SharpOnvifWCF
{
    public class SimpleOnvifClient
    {
        public const string ONVIF_MEDIA = "http://www.onvif.org/ver10/media/wsdl";
        public const string ONVIF_EVENTS = "http://www.onvif.org/ver10/events/wsdl";

        private readonly string _onvifUri;
        private readonly IEndpointBehavior _auth;
        private Dictionary<string, string> _supportedServices;

        public SimpleOnvifClient(string onvifUri, string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(onvifUri)) 
                throw new ArgumentNullException(nameof(onvifUri));

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("User name or password must not be empty!");

            this._onvifUri = onvifUri;
            this._auth = OnvifHelper.CreateAuthenticationBehavior(userName, password);
        }

        #region Device Management

        public async Task<OnvifDeviceMgmt.GetDeviceInformationResponse> GetDeviceInformationAsync()
        {
            using (var deviceClient = new OnvifDeviceMgmt.DeviceClient(
                OnvifHelper.CreateBinding(),
                OnvifHelper.CreateEndpointAddress(_onvifUri)))
            {
                OnvifHelper.SetAuthentication(deviceClient.Endpoint, _auth);
                var deviceInfo = await deviceClient.GetDeviceInformationAsync(new OnvifDeviceMgmt.GetDeviceInformationRequest()).ConfigureAwait(false);
                return deviceInfo;
            }
        }

        public async Task<OnvifDeviceMgmt.GetServicesResponse> GetServicesAsync(bool includeCapability = false)
        {
            using (var deviceClient = new OnvifDeviceMgmt.DeviceClient(
                OnvifHelper.CreateBinding(),
                OnvifHelper.CreateEndpointAddress(_onvifUri)))
            {
                OnvifHelper.SetAuthentication(deviceClient.Endpoint, _auth);
                var services = await deviceClient.GetServicesAsync(includeCapability).ConfigureAwait(false);
                return services;
            }
        }

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
                throw new NotSupportedException($"The device does not support {ns} sevice!");
        }
        
        public async Task<OnvifDeviceMgmt.SystemDateTime> GetSystemDateAndTimeAsync()
        {
            using (var deviceClient = new OnvifDeviceMgmt.DeviceClient(
                OnvifHelper.CreateBinding(),
                OnvifHelper.CreateEndpointAddress(_onvifUri)))
            {
                OnvifHelper.SetAuthentication(deviceClient.Endpoint, _auth);
                var cameraTime = await deviceClient.GetSystemDateAndTimeAsync().ConfigureAwait(false); 
                return cameraTime;
            }
        }
        
        public async Task<DateTime> GetSystemDateAndTimeUtcAsync()
        {
            var cameraTime = await GetSystemDateAndTimeAsync().ConfigureAwait(false);
            var cameraDateTime = new DateTime(
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

        public async Task<OnvifMedia.GetProfilesResponse> GetProfilesAsync()
        {
            string mediaUrl = await GetServiceUriAsync(ONVIF_MEDIA).ConfigureAwait(false);
            using (var mediaClient = new OnvifMedia.MediaClient(
                OnvifHelper.CreateBinding(),
                OnvifHelper.CreateEndpointAddress(mediaUrl)))
            {
                OnvifHelper.SetAuthentication(mediaClient.Endpoint, _auth);
                var profiles = await mediaClient.GetProfilesAsync().ConfigureAwait(false);
                return profiles;
            }
        }

        public Task<OnvifMedia.MediaUri> GetStreamUriAsync(OnvifMedia.Profile profile)
        {
            return GetStreamUriAsync(profile.token);
        }

        public async Task<OnvifMedia.MediaUri> GetStreamUriAsync(string profileToken)
        {
            string mediaUrl = await GetServiceUriAsync(ONVIF_MEDIA).ConfigureAwait(false);
            using (var mediaClient = new OnvifMedia.MediaClient(
                OnvifHelper.CreateBinding(),
                OnvifHelper.CreateEndpointAddress(mediaUrl)))
            {
                OnvifHelper.SetAuthentication(mediaClient.Endpoint, _auth);
                var streamUri = await mediaClient.GetStreamUriAsync(new OnvifMedia.StreamSetup(), profileToken).ConfigureAwait(false);
                return streamUri;
            }
        }

        #endregion // Media

        #region Events

        #region Pull Point subscription

        public async Task<OnvifEvents.CreatePullPointSubscriptionResponse> PullPointSubscribeAsync(int initialTerminationTimeInMinutes = 5)
        {
            string eventUri = await GetServiceUriAsync(ONVIF_EVENTS);
            using (OnvifEvents.EventPortTypeClient eventPortTypeClient = new OnvifEvents.EventPortTypeClient(
                            OnvifHelper.CreateBinding(),
                            OnvifHelper.CreateEndpointAddress(eventUri)))
            {
                OnvifHelper.SetAuthentication(eventPortTypeClient.Endpoint, _auth);

                var subscribeResponse = await eventPortTypeClient.CreatePullPointSubscriptionAsync(
                    new OnvifEvents.CreatePullPointSubscriptionRequest()
                    {
                        InitialTerminationTime = GetTimeoutInMinutes(initialTerminationTimeInMinutes)
                    }).ConfigureAwait(false);

                Debug.WriteLine($"Pull Point subscription termination time: {subscribeResponse.TerminationTime}");

                return subscribeResponse;
            }
        }

        public Task<OnvifEvents.PullMessagesResponse> PullPointPullMessagesAsync(OnvifEvents.CreatePullPointSubscriptionResponse subscribeResponse, int timeoutInMinutes = 5, int maxMessages = 100)
        {
            return PullPointPullMessagesAsync(subscribeResponse.SubscriptionReference.Address.Value, timeoutInMinutes, maxMessages);
        }

        public async Task<OnvifEvents.PullMessagesResponse> PullPointPullMessagesAsync(string subscriptionReferenceAddress, int timeoutInMinutes = 5, int maxMessages = 100)
        {
            using (OnvifEvents.PullPointSubscriptionClient pullPointClient =
                new OnvifEvents.PullPointSubscriptionClient(
                    OnvifHelper.CreateBinding(),
                    OnvifHelper.CreateEndpointAddress(subscriptionReferenceAddress)))
            {
                OnvifHelper.SetAuthentication(pullPointClient.Endpoint, _auth);

                var messages = await pullPointClient.PullMessagesAsync(
                    new OnvifEvents.PullMessagesRequest(
                        GetTimeoutInMinutes(timeoutInMinutes), 
                        maxMessages, 
                        Array.Empty<System.Xml.XmlElement>())).ConfigureAwait(false);

                return messages;
            }
        }

        #endregion // Pull Point subscription

        #region Basic subscription

        public async Task<OnvifEvents.SubscribeResponse1> BasicSubscribeAsync(string onvifEventListenerUri, int timeoutInMinutes = 5)
        {
            // Basic events need an exception in Windows Firewall + VS must run as Admin
            string eventUri = await GetServiceUriAsync(ONVIF_EVENTS);
            using (OnvifEvents.NotificationProducerClient notificationProducerClient = new OnvifEvents.NotificationProducerClient(
                            OnvifHelper.CreateBinding(),
                            OnvifHelper.CreateEndpointAddress(eventUri)))
            {
                OnvifHelper.SetAuthentication(notificationProducerClient.Endpoint, _auth);

                var subscriptionResult = await notificationProducerClient.SubscribeAsync(new OnvifEvents.Subscribe()
                {
                    InitialTerminationTime = GetTimeoutInMinutes(timeoutInMinutes),
                    ConsumerReference = new OnvifEvents.EndpointReferenceType() 
                    { 
                        Address = new OnvifEvents.AttributedURIType() 
                        { 
                            Value = onvifEventListenerUri
                        } 
                    }
                }).ConfigureAwait(false);

                return subscriptionResult;
            }
        }

        #endregion // Basic subscription

        #region Common events

        private static bool IsMotionEvent(string eventXml)
        {
            if(string.IsNullOrEmpty(eventXml))
                return false;

            return eventXml.Contains("RuleEngine/CellMotionDetector/Motion") && eventXml.Contains("SimpleItem Name=\"IsMotion\"");
        }

        public static bool? IsMotionDetected(string eventXml)
        {
            if (!IsMotionEvent(eventXml)) return null;
            return eventXml.Contains("SimpleItem Name=\"IsMotion\" Value=\"true\"");
        }

        public static bool? IsMotionDetected(NotificationMessageHolderType message)
        {
            return IsMotionDetected(string.Concat(message.Topic.Any.First().Value, message.Message.InnerXml));
        }

        private static bool IsTamperEvent(string eventXml)
        {
            if(string.IsNullOrEmpty(eventXml))
                return false;

            return eventXml.Contains("RuleEngine/TamperDetector/Tamper") && eventXml.Contains("SimpleItem Name=\"IsTamper\"");
        }

        public static bool? IsTamperDetected(string eventXml)
        {
            if (!IsTamperEvent(eventXml)) return null;
            return eventXml.Contains("SimpleItem Name=\"IsTamper\" Value=\"true\"");
        }

        public static bool? IsTamperDetected(NotificationMessageHolderType message)
        {
            return IsTamperDetected(string.Concat(message.Topic.Any.First().Value, message.Message.InnerXml));
        }

        #endregion // Common events

        #endregion // Events

        #region Discovery

        public static async Task<IList<string>> DiscoverAsync(string ipAddress, Action<string> progress = null)
        {
            var devices = await OnvifHelper.DiscoverAsync(ipAddress, progress);
            return devices;
        }

        #endregion // Discovery

        #region Utility

        private static string GetTimeoutInSeconds(int timeoutInSeconds)
        {
            return $"PT{timeoutInSeconds}S";
        }

        private static string GetTimeoutInMinutes(int timeoutInMinutes)
        {
            return $"PT{timeoutInMinutes}M";
        }

        #endregion // Utility
    }

    /// <summary>
    /// Simple Onvif event listener.
    /// </summary>
    /// <remarks>Basic events need an exception in Windows Firewall + VS must run as Admin.</remarks>
    public class SimpleOnvifEventListener : IDisposable
    {
        private bool _disposedValue;
        private readonly Action<int, string> _onEvent;
        private readonly int _port;
        private readonly Task _listenerTask;

        public HttpListener Listener { get; } = new HttpListener();

        public SimpleOnvifEventListener(Action<int, string> onEvent, int port = 9999)
        {
            this._onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
            this._port = port;

            string httpUri = GetHttpUri("+", port);
            Listener.Prefixes.Add(httpUri);
            Listener.Start();
            _listenerTask = Task.Run(async () =>
            {
                while (!_disposedValue)
                {
                    HttpListenerContext ctx = await Listener.GetContextAsync();
                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        try
                        {
                            string data = GetRequestPostData(ctx.Request);
                            int cameraID = 0;

                            // ctx.Request.RawUrl should be somthing like "/0/"
                            if (int.TryParse(ctx.Request.RawUrl.TrimStart('/').TrimEnd('/'), out cameraID))
                            {
                                ProcessNotification(cameraID, data);
                            }
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            resp.StatusCode = (int)HttpStatusCode.OK;
                            resp.StatusDescription = "Status OK";
                        }
                    }
                }
            });
        }

        public string GetOnvifEventListenerUri(string host, int cameraID = 0)
        {
            return $"{GetHttpUri(host, _port)}{cameraID}/";
        }

        private static string GetHttpUri(string host, int port)
        {
            return $"http://{host}:{port}/";
        }

        private void ProcessNotification(int cameraID, string data)
        {
            _onEvent.Invoke(cameraID, data);
        }

        private static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (Stream body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                    return reader.ReadToEnd();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Listener.Close();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
