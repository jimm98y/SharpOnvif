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

using SharpOnvifClient.Security;
using System;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Threading.Tasks;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// WCF does not let the client see the response of an unauthorized request, so the WwwAuthenticate
    ///  challenge has to be recovered from the message of the
    ///  <see cref="MessageSecurityException"/> it throws instead. The message is localized and every
    ///  language encloses the header differently, which is what these tests cover.
    /// </summary>
    /// <remarks>
    /// The formats are the "HttpAuthorizationFailed" resource of System.ServiceModel, verbatim.
    ///  {0} is the client authentication scheme, {1} the WwwAuthenticate header(s) of the response.
    /// </remarks>
    public static class HttpDigestMessages
    {
        public const string EnUS = "The HTTP request is unauthorized with client authentication scheme '{0}'. The authentication header received from the server was '{1}'.";

        /// <summary>The header is enclosed in typographic quotes.</summary>
        public const string ZhHans = "HTTP 请求未经客户端身份验证方案“{0}”授权。从服务器收到的身份验证标头为“{1}”。";

        /// <summary>The header is enclosed in double quotes, which the challenge itself is full of.</summary>
        public const string De = "Die HTTP-Anforderung ist beim Clientauthentifizierungsschema \"{0}\" nicht autorisiert. Vom Server wurde der Authentifizierungsheader \"{1}\" empfangen.";

        /// <summary>The header is not enclosed in anything at all.</summary>
        public const string Cs = "Požadavek protokolu HTTP je neoprávněný se schématem autorizace klienta {0}. Záhlaví ověření přijaté ze serveru je {1}.";
    }

    [ServiceContract]
    public interface IFaultingChannel
    {
        [OperationContract]
        Task CallAsync();

        [OperationContract]
        Task<string> CallWithResultAsync();
    }

    /// <summary>
    /// Stands in for a WCF channel that is answered with a challenge. The failure is reported on the
    ///  returned task, the way WCF reports it - a synchronous throw would be wrapped in a
    ///  <see cref="System.Reflection.TargetInvocationException"/> and never reach the proxy.
    /// </summary>
    public class FaultingChannel : IFaultingChannel
    {
        private readonly string _exceptionMessage;

        /// <summary>
        /// Number of times the channel was called, so a test can tell a retry from a rethrow.
        /// </summary>
        public int Calls { get; private set; }

        public FaultingChannel(string exceptionMessage)
        {
            this._exceptionMessage = exceptionMessage;
        }

        public Task CallAsync()
        {
            Calls++;
            return Task.FromException(new MessageSecurityException(_exceptionMessage));
        }

        public Task<string> CallWithResultAsync()
        {
            Calls++;
            return Task.FromException<string>(new MessageSecurityException(_exceptionMessage));
        }
    }

    public static class HttpDigestChallengeSource
    {
        /// <summary>
        /// Runs a call through <see cref="HttpDigestProxy{T}"/> and returns the challenges it recovered
        ///  from the exception, which is what the client then authenticates with.
        /// </summary>
        public static string[] GetChallenges(string exceptionMessage)
        {
            var state = new HttpDigestState();
            GetChallenges(exceptionMessage, state);
            return state.GetHeaders();
        }

        /// <summary>
        /// The same, against a state that is already in use, so that a re-challenge can be tested.
        /// </summary>
        public static void GetChallenges(string exceptionMessage, IHttpMessageState state)
        {
            var channel = new FaultingChannel(exceptionMessage);
            var proxy = HttpDigestProxy<IFaultingChannel>.CreateProxy(channel, state);

            try
            {
                // the retry fails as well, so the exception is expected either way
                proxy.CallAsync().GetAwaiter().GetResult();
            }
            catch (MessageSecurityException)
            { }
        }

        /// <summary>
        /// Builds the exception message a device answering with these challenges would produce.
        /// </summary>
        public static string CreateExceptionMessage(string format, params string[] challenges)
        {
            // WCF concatenates the WwwAuthenticate headers of the response into a single value
            return string.Format(format, "Anonymous", string.Join(", ", challenges));
        }
    }
}
