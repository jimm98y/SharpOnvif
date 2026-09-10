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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharpOnvifClient.Security
{
    /// <summary>
    /// HttpDigestProxy intercepts calls into all methods of the proxied object.
    /// In WCF, this is necessary to handle the <see cref="System.ServiceModel.Security.MessageSecurityException"/>
    ///  and re-try the request. <see cref="System.ServiceModel.Dispatcher.IClientMessageInspector"/> is not
    ///  going to be invoked after such exception is thrown.
    /// To retrieve the WwwAuthenticate header, the proxy parses it from the <see cref="System.ServiceModel.Security.MessageSecurityException"/>
    ///  message. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HttpDigestProxy<T> : DispatchProxy, IDisposable where T : class
    {
        private bool disposedValue;

        public T Target { get; private set; }
        public IHttpMessageState State { get; private set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            object result;

            var returnType = targetMethod.ReturnType;
            if (returnType == typeof(Task))
            {
                return Task.Run(async () =>
                {
                    Task resultTask;
                    try
                    {
                        resultTask = targetMethod.Invoke(Target, args) as Task;
                        await resultTask.ConfigureAwait(false);
                    }
                    catch (System.ServiceModel.Security.MessageSecurityException ex)
                    {
                        IEnumerable<string> wwwAuthenticateHeaders = ParseMessageSecurityException(ex.Message);
                        if (!wwwAuthenticateHeaders.Any())
                            throw; // the request failed for another reason, report the original error

                        State.SetHeaders(wwwAuthenticateHeaders);

                        resultTask = targetMethod.Invoke(Target, args) as Task;
                        await resultTask.ConfigureAwait(false);
                    }
                });
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Task resultTask = targetMethod.Invoke(Target, args) as Task;
                var resultTypeArgs = resultTask.GetType().GetGenericArguments()[0];

                var wrapperTask = Task.Run(async () =>
                {
                    try
                    {
                        await resultTask.ConfigureAwait(false);
                    }
                    catch (System.ServiceModel.Security.MessageSecurityException ex)
                    {
                        IEnumerable<string> wwwAuthenticateHeaders = ParseMessageSecurityException(ex.Message);
                        if (!wwwAuthenticateHeaders.Any())
                            throw; // the request failed for another reason, report the original error

                        State.SetHeaders(wwwAuthenticateHeaders);

                        resultTask = targetMethod.Invoke(Target, args) as Task;
                        await resultTask.ConfigureAwait(false);
                    }

                    var resultProperty = typeof(Task<>).MakeGenericType(resultTypeArgs).GetProperty("Result");
                    result = resultProperty.GetValue(resultTask);
                    return result;
                });

                var method = typeof(HttpDigestProxy<T>).GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.Public);
                return method.MakeGenericMethod(resultTypeArgs).Invoke(null, new object[] { wrapperTask });
            }
            else
            {
                try
                {
                    result = targetMethod.Invoke(Target, args);
                }
                catch (System.ServiceModel.Security.MessageSecurityException ex)
                {
                    IEnumerable<string> wwwAuthenticateHeaders = ParseMessageSecurityException(ex.Message);
                    if (!wwwAuthenticateHeaders.Any())
                        throw; // the request failed for another reason, report the original error

                    State.SetHeaders(wwwAuthenticateHeaders);

                    result = targetMethod.Invoke(Target, args);
                }
                return result;
            }
        }

        /// <summary>
        /// RFC 7235 auth-param: a token, followed by either a quoted-string or another token.
        /// The token is deliberately narrower than the RFC 7230 one, which also allows characters such as
        ///  the apostrophe and the dot. Those are the characters the exception message ends with, and an
        ///  unquoted value would swallow them together with the rest of the sentence. Every value an Onvif
        ///  device sends unquoted (algorithm, qop, stale, userhash, charset, nc) fits in this set.
        /// </summary>
        private const string AUTH_PARAM = @"[\w-]+\s*=\s*(?:""[^""]*""|[\w-]+)";

        /// <summary>
        /// A "Digest" challenge and the comma separated list of its auth-params. Several challenges can
        ///  follow each other, which is why the list stops at anything that is not an auth-param.
        /// </summary>
        private static readonly Regex _digestChallengeRegex = new Regex(
            $@"(?<!\w)Digest\s+(?<param>{AUTH_PARAM})(?:\s*,\s*(?<param>{AUTH_PARAM}))*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <returns>
        /// The WwwAuthenticate headers, or an empty array when the message carries no Digest challenge -
        ///  MessageSecurityException is also what WCF throws for a 403, where there is nothing to retry.
        /// </returns>
        private IEnumerable<string> ParseMessageSecurityException(string message)
        {
            // Workaround: The only way to get the response WwwAuthenticate headers from WCF seems to be
            //  by parsing them from the Exception message text. This is not ideal and we have to be careful
            //  to not use any hardcoded strings as the exception message might be localized.
            // The message quotes the header differently in every language - with apostrophes, with double
            //  quotes (which the challenge itself is full of), with typographic quotes, and in some
            //  languages it is not quoted at all. The quoting is therefore ignored and the challenges are
            //  matched by their own grammar instead, which is what separates them from the text around them.

            /*
            The HTTP request is unauthorized with client authentication scheme 'Anonymous'.
            The authentication header received from the server was
            'Digest realm="IP Camera", qop="auth, auth-int", nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE", Digest realm="IP Camera", qop="auth, auth-int", algorithm=SHA-256, nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE", Digest realm="IP Camera", qop="auth, auth-int", algorithm=SHA-512-256, nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE"'.

            Hikvision, Chinese Windows - the header is enclosed in typographic quotes:
            HTTP 请求未经客户端身份验证方案“Anonymous”授权。从服务器收到的身份验证标头为“Digest qop="auth", realm="IP Camera(FN636)", nonce="663338373a34393137373736373ace51538fd6d7317abb7651757a8a65af", stale="FALSE"”。

            Czech Windows - the header is not enclosed in anything:
            Požadavek protokolu HTTP je neoprávněný se schématem autorizace klienta Anonymous. Záhlaví ověření přijaté ze serveru je Digest realm="IP Camera", qop="auth, auth-int", nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE".
            */
            List<string> wwwAuthenticateHeaders = new List<string>();

            foreach (Match match in _digestChallengeRegex.Matches(message))
            {
                var authParams = match.Groups["param"].Captures.Cast<Capture>().Select(x => x.Value);
                wwwAuthenticateHeaders.Add($"Digest {string.Join(", ", authParams)}");
            }

            if (wwwAuthenticateHeaders.Count == 0)
            {
                Debug.WriteLine($"No WWW-Authenticate Digest challenge was found in: {message}");
            }

            return wwwAuthenticateHeaders;
        }

        public static async Task<TResult> Cast<TResult>(Task<object> task)
        {
            return (TResult)await task.ConfigureAwait(false);
        }

        public static T CreateProxy(T target, IHttpMessageState state)
        {
            var proxy = Create<T, HttpDigestProxy<T>>() as HttpDigestProxy<T>;
            proxy.Target = target;
            proxy.State = state;
            return proxy as T;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var disposableTarget = Target as IDisposable;
                    if (disposableTarget != null)
                    {
                        disposableTarget.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
