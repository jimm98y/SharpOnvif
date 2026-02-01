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

using SharpOnvifServer.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OnvifService.Onvif
{
    /// <summary>
    /// Sample Event Source implementation that generates a new motion event every 10 seconds.
    /// </summary>
    public class EventSourceImpl : IEventSource, IDisposable
    {
        private readonly Timer _timer;
        private bool _disposedValue;

        public event EventHandler<NotificationEventArgs> OnEvent;

        public EventSourceImpl()
        {
            _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10));
        }

        private void OnTick(object state)
        {
            OnEvent?.Invoke(this, new NotificationEventArgs(
                // sample Motion detected event
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

        #region IDisposable implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timer.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable implementation
    }
}
