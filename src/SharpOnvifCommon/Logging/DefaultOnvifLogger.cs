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
    /// A logger that writes to the console. Logging is off until it is switched on, so a library
    /// that is working says nothing.
    /// </summary>
    public sealed class DefaultOnvifLogger : ILog
    {
        /// <summary>Whether anything is written at all.</summary>
        public bool IsLoggingEnabled { get; set; } = false;

        public bool IsErrorEnabled { get; set; } = true;
        public bool IsWarningEnabled { get; set; } = true;
        public bool IsInfoEnabled { get; set; } = true;
        public bool IsDebugEnabled { get; set; } = true;
        public bool IsTraceEnabled { get; set; } = true;

        public void LogError(string error)
        {
            if (IsLoggingEnabled && IsErrorEnabled) Console.WriteLine(error);
        }

        public void LogWarning(string warning)
        {
            if (IsLoggingEnabled && IsWarningEnabled) Console.WriteLine(warning);
        }

        public void LogInfo(string info)
        {
            if (IsLoggingEnabled && IsInfoEnabled) Console.WriteLine(info);
        }

        public void LogDebug(string debug)
        {
            if (IsLoggingEnabled && IsDebugEnabled) Console.WriteLine(debug);
        }

        public void LogTrace(string trace)
        {
            if (IsLoggingEnabled && IsTraceEnabled) Console.WriteLine(trace);
        }
    }
}
