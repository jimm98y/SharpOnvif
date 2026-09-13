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

namespace __RUNTIME__
{
    /// <summary>
    /// Where the generated code says what it could not do.
    /// </summary>
    /// <remarks>
    /// The levels are asked before they are written, so that composing a message nobody will read
    /// costs nothing - a device that answers badly can do so on every request.
    /// <para>
    /// Only the contract is generated. Somewhere to send it to is not: a caller implements this,
    /// or is given one of the loggers the library it came with happens to carry. The runtime
    /// writes nowhere of its own accord, and a null logger is the normal case rather than an
    /// error.
    /// </para>
    /// </remarks>
    public interface ILog
    {
        void LogError(string error);
        void LogWarning(string warning);
        void LogInfo(string info);
        void LogDebug(string debug);
        void LogTrace(string trace);

        bool IsErrorEnabled { get; set; }
        bool IsWarningEnabled { get; set; }
        bool IsInfoEnabled { get; set; }
        bool IsDebugEnabled { get; set; }
        bool IsTraceEnabled { get; set; }
    }
}
