using SharpOnvifServer.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OnvifService.Onvif
{
    public class EventSourceImpl : IEventSource
    {
        private readonly Timer _timer;

        public event EventHandler<NotificationEventArgs> OnEvent;

        public EventSourceImpl()
        {
            _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        }

        private void OnTick(object state)
        {
            OnEvent?.Invoke(this, new NotificationEventArgs(
                new NotificationMessage()
                {
                    TopicNamespacePrefix = "tns1",
                    TopicNamespace = "http://www.onvif.org/ver10/topics",
                    Topic = "RuleEngine/CellMotionDetector/Motion",
                    Created = DateTime.UtcNow,
                    Source = new Dictionary<string, string>()
                    {
                        { "VideoSourceConfigurationToken", "VideoSourceToken" },
                        { "VideoAnalyticsConfigurationToken", "VideoAnalyticsToken" },
                        { "Rule", "MyMotionDetectorRule" }
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "IsMotion", "true" }
                    }
                }
            ));
        }
    }
}
