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

using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OnvifService.Onvif
{
    /// <summary>
    /// Onvif Event Subscription Manager.
    /// </summary>
    public class SubscriptionManagerImpl : SubscriptionManager, PullPointSubscription, IEventSubscription
    {
        private readonly ILogger<SubscriptionManagerImpl> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private IEventSource _eventSource;
        private readonly string _notificationEndpoint; // endpoint where to send Basic subscription notifications
        private readonly TimeSpan _expirationDelta;

        private readonly ConcurrentQueue<NotificationMessage> _messages = new ConcurrentQueue<NotificationMessage>();

        /// <summary>
        /// Subscription expiration time in UTC.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        public SubscriptionManagerImpl(
            DateTime expirationTime, 
            TimeSpan expirationDelta,
            string notificationEndpoint,
            ILogger<SubscriptionManagerImpl> logger,
            IHttpClientFactory httpClientFactory,
            IEventSource eventSource)
        {
            ExpirationTime = expirationTime;
            _expirationDelta = expirationDelta;
            _notificationEndpoint = notificationEndpoint; // for Basic subscriptions only
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _eventSource = eventSource;
            eventSource.OnEvent += EventSource_OnEvent;
        }

        private void EventSource_OnEvent(object sender, NotificationEventArgs e)
        {
            if (string.IsNullOrEmpty(_notificationEndpoint))
            {
                // PullPoint
                _messages.Enqueue(e.Message);
            }
            else
            {
                // Basic
                SendBasicEventNotification(e.Message);
            }
        }

        public PullMessagesResponse PullMessages(PullMessagesRequest request)
        {
            if (!string.IsNullOrEmpty(_notificationEndpoint))
                throw new InvalidOperationException($"{nameof(SubscriptionManagerImpl)}: {nameof(PullMessages)} is not supported on Basic event subscription!");

            DateTime now = DateTime.UtcNow;
            DateTime expiration = now.Add(_expirationDelta); // TODO: review if adding the initial Delta is correct
            ExtendExpiration(expiration);

            // spinlock wait until timeout
            TimeSpan timeout = OnvifHelpers.FromTimeout(request.Timeout);
            DateTime waitUntil = now.Add(timeout);
            while(_messages.Count == 0 && DateTime.UtcNow < waitUntil)
            {
                Task.Delay(500).Wait();
            }

            NotificationMessage message = null;
            List<NotificationMessage> content = new List<NotificationMessage>();
            while(_messages.TryDequeue(out message) && content.Count < request.MessageLimit)
            {
                content.Add(message);
            }

            return new PullMessagesResponse()
            {
                CurrentTime = now,
                TerminationTime = expiration,
                NotificationMessage = content.Select(msg => new NotificationMessageHolderType()
                {
                    Any = OnvifEvents.CreateNotificationMessage(msg) 
                }).ToArray()
            };
        }

        public RenewResponse1 Renew(RenewRequest request)
        {
            DateTime now = DateTime.UtcNow;
            DateTime expiration = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, request.Renew.TerminationTime, now.AddMinutes(1));
            ExtendExpiration(expiration);

            return new RenewResponse1()
            {
                RenewResponse = new RenewResponse()
                {
                    CurrentTime = DateTime.UtcNow,
                    TerminationTime = expiration
                }
            };
        }

        public UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            return new UnsubscribeResponse1()
            {
                UnsubscribeResponse = new UnsubscribeResponse()
            };
        }

        private void ExtendExpiration(DateTime expiration)
        {
            ExpirationTime = expiration;
        }

        public void Detach()
        {
            _eventSource.OnEvent -= EventSource_OnEvent;
        }

        private void SendBasicEventNotification(NotificationMessage message)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(NotificationMessageHolderType));
            using (StringWriter writer = new StringWriter())
            {
                var msg = new NotificationMessageHolderType()
                {
                    Any = OnvifEvents.CreateNotificationMessage(message)
                };

                serializer.Serialize(writer, msg);

                string content = writer.ToString();

                var httpClient = _httpClientFactory.CreateClient();
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _notificationEndpoint)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/soap+xml")
                };

                try
                {
                    var httpResponseMessage = httpClient.Send(httpRequestMessage);

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        _logger.LogDebug($"{nameof(SubscriptionManagerImpl)}: Sent Basic event to {_notificationEndpoint}\r\n{content}\r\n");
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError($"{nameof(SubscriptionManagerImpl)}: Failed to send Basic event to {_notificationEndpoint} because of an exception: {ex.Message}.");
                }
            }
        }

        public SeekResponse Seek(SeekRequest request)
        {
            _logger.LogError($"{nameof(Seek)} is currently not supported");
            return new SeekResponse();
        }

        public void SetSynchronizationPoint()
        {
            _logger.LogError($"{nameof(SetSynchronizationPoint)} is currently not supported");
        }
    }
}
