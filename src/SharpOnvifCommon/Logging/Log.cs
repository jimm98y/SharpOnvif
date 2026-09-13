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

namespace SharpOnvifCommon
{
    /// <summary>
    /// The logger the client and the shared code report through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These reports used to be <c>Debug.WriteLine</c>, which is compiled out of a release build:
    /// a Probe that could not leave the machine said nothing anyone would ever see, so IPv6
    /// discovery looked like a network with no devices on it rather than a client that was not
    /// asking.
    /// </para>
    /// <para>
    /// The default logger writes to the console and is switched off, so a library that is working
    /// stays quiet. Point it somewhere else to bridge into a host's own logging - there is no
    /// dependency on any logging package, which is what keeps these assemblies free of any.
    /// </para>
    /// <code>
    /// Log.Logger = new MyLogger(hostLogger);          // your own IOnvifLogger
    /// Log.Logger = NullOnvifLogger.Instance;          // or nothing at all
    /// </code>
    /// <para>
    /// The server does not use this: it is given an <c>ILogger</c> by the host and logs to that.
    /// </para>
    /// </remarks>
    public static class Log
    {
        private static IOnvifLogger _logger = new DefaultOnvifLogger();

        /// <summary>Where reports go. Never null; setting null restores the default.</summary>
        public static IOnvifLogger Logger
        {
            get { return _logger; }
            set { _logger = value ?? new DefaultOnvifLogger(); }
        }

        public static void Error(string message, Exception error = null)
        {
            IOnvifLogger logger = _logger;
            if (logger.IsErrorEnabled) logger.LogError(Compose(message, error));
        }

        public static void Warning(string message, Exception error = null)
        {
            IOnvifLogger logger = _logger;
            if (logger.IsWarningEnabled) logger.LogWarning(Compose(message, error));
        }

        public static void Info(string message, Exception error = null)
        {
            IOnvifLogger logger = _logger;
            if (logger.IsInfoEnabled) logger.LogInfo(Compose(message, error));
        }

        public static void Debug(string message, Exception error = null)
        {
            IOnvifLogger logger = _logger;
            if (logger.IsDebugEnabled) logger.LogDebug(Compose(message, error));
        }

        public static void Trace(string message, Exception error = null)
        {
            IOnvifLogger logger = _logger;
            if (logger.IsTraceEnabled) logger.LogTrace(Compose(message, error));
        }

        private static string Compose(string message, Exception error)
        {
            return error == null ? message : message + " - " + error.Message;
        }
    }
}
