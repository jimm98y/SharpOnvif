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

using System;
using SharpOnvifCommon;

namespace SharpOnvifClient
{
    /// <summary>What discovery was doing when it failed.</summary>
    public enum OnvifDiscoveryOperation
    {
        /// <summary>Opening a socket on an interface, or joining the discovery group on it.</summary>
        Listen,

        /// <summary>Sending a Probe.</summary>
        Probe,

        /// <summary>Reading what arrived.</summary>
        Receive,

        /// <summary>Running the application's own handler for something that arrived.</summary>
        Handler,
    }

    /// <summary>
    /// Something discovery could not do, on one interface.
    /// </summary>
    /// <remarks>
    /// Discovery is done on every interface at once and carries on when one of them fails, because
    /// a machine with an interface that cannot carry multicast is ordinary. That makes the failures
    /// easy to lose: an IPv6 Probe that cannot leave the machine looks exactly like a network with
    /// no IPv6 devices on it. Subscribing to these says which it is.
    /// </remarks>
    public class OnvifDiscoveryFailureEventArgs : EventArgs
    {
        public OnvifDiscoveryFailureEventArgs(OnvifDiscoveryOperation operation, string networkInterface, Exception error)
        {
            Operation = operation;
            NetworkInterface = networkInterface;
            Error = error;
        }

        /// <summary>What was being attempted.</summary>
        public OnvifDiscoveryOperation Operation { get; }

        /// <summary>The interface address it was attempted on, where there was one.</summary>
        public string NetworkInterface { get; }

        /// <summary>What went wrong.</summary>
        public Exception Error { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(NetworkInterface)
                ? $"Onvif discovery could not {Operation}: {Error?.Message}"
                : $"Onvif discovery could not {Operation} on {NetworkInterface}: {Error?.Message}";
        }
    }

    internal static class OnvifDiscoveryFailure
    {
        /// <summary>
        /// Reports a failure without letting the reporting become one: a handler that throws must
        /// not take down the discovery it was told about.
        /// </summary>
        public static void Raise(
            EventHandler<OnvifDiscoveryFailureEventArgs> handler,
            object sender,
            IOnvifLogger logger,
            OnvifDiscoveryOperation operation,
            string networkInterface,
            Exception error)
        {
            var args = new OnvifDiscoveryFailureEventArgs(operation, networkInterface, error);

            // Reported both ways on purpose: the log is where an operator looks afterwards, the
            // event is how an application can say something about it at the time. The text
            // already names the error, so it is not appended a second time.
            logger.Warning(args.ToString());

            if (handler == null) return;

            try
            {
                handler(sender, args);
            }
            catch (Exception ex)
            {
                logger.Warning("An Onvif discovery failure handler threw.", ex);
            }
        }
    }
}
