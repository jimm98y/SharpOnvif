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

namespace SharpOnvifCommon
{
    /// <summary>
    /// An <see cref="ILog"/> that discards everything, for a host that has its own
    /// logging and does not want the library writing anywhere of its own accord.
    /// </summary>
    public sealed class NullOnvifLogger : ILog
    {
        public static readonly NullOnvifLogger Instance = new NullOnvifLogger();

        private NullOnvifLogger() { }

        public void LogError(string error) { }
        public void LogWarning(string warning) { }
        public void LogInfo(string info) { }
        public void LogDebug(string debug) { }
        public void LogTrace(string trace) { }

        public bool IsErrorEnabled { get { return false; } set { } }
        public bool IsWarningEnabled { get { return false; } set { } }
        public bool IsInfoEnabled { get { return false; } set { } }
        public bool IsDebugEnabled { get { return false; } set { } }
        public bool IsTraceEnabled { get { return false; } set { } }
    }
}
