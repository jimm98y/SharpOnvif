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
    /// Reporting to a logger that may not be there, and may not want the level.
    /// </summary>
    /// <remarks>
    /// A logger is carried by the object doing the work - a client, a listener - so that two of
    /// them in one process can report to different places, or one of them to nowhere. That means
    /// every call site has to cope with not having one, which is what these are for.
    /// </remarks>
    public static class OnvifLoggerExtensions
    {
        public static void Error(this ILog logger, string message, Exception error = null)
        {
            if (logger != null && logger.IsErrorEnabled) logger.LogError(Compose(message, error));
        }

        public static void Warning(this ILog logger, string message, Exception error = null)
        {
            if (logger != null && logger.IsWarningEnabled) logger.LogWarning(Compose(message, error));
        }

        public static void Info(this ILog logger, string message, Exception error = null)
        {
            if (logger != null && logger.IsInfoEnabled) logger.LogInfo(Compose(message, error));
        }

        public static void Debug(this ILog logger, string message, Exception error = null)
        {
            if (logger != null && logger.IsDebugEnabled) logger.LogDebug(Compose(message, error));
        }

        public static void Trace(this ILog logger, string message, Exception error = null)
        {
            if (logger != null && logger.IsTraceEnabled) logger.LogTrace(Compose(message, error));
        }

        private static string Compose(string message, Exception error)
        {
            return error == null ? message : message + " - " + error.Message;
        }
    }
}
