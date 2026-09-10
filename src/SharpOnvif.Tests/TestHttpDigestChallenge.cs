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

using SharpOnvifCommon.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Recovering the WwwAuthenticate challenges from the localized message of the
    ///  MessageSecurityException WCF throws for an unauthorized request.
    /// </summary>
    [TestClass]
    public sealed class TestHttpDigestChallenge
    {
        private const string MD5Challenge = "Digest realm=\"IP Camera(FN636)\", qop=\"auth, auth-int\", nonce=\"abc123\", opaque=\"00000000\", userhash=TRUE, stale=\"FALSE\"";
        private const string Sha256Challenge = "Digest realm=\"IP Camera(FN636)\", qop=\"auth, auth-int\", algorithm=SHA-256, nonce=\"abc123\", opaque=\"00000000\", userhash=TRUE, stale=FALSE";

        /// <summary>
        /// Every language System.ServiceModel ships resources for. The quoting differs per language and in
        ///  Czech there is no quoting at all, so nothing but the challenge grammar tells where it ends.
        /// </summary>
        [DataRow("en-US", "The HTTP request is unauthorized with client authentication scheme '{0}'. The authentication header received from the server was '{1}'.")]
        [DataRow("zh-Hans", "HTTP 请求未经客户端身份验证方案“{0}”授权。从服务器收到的身份验证标头为“{1}”。")]
        [DataRow("zh-Hant", "HTTP 要求未經用戶端驗證配置 '{0}' 的授權。接收自伺服器的驗證標頭為 '{1}'。")]
        [DataRow("ja", "この HTTP 要求は、クライアントの認証方式 '{0}' では承認されません。サーバーから受信した認証ヘッダーは '{1}' でした。")]
        [DataRow("ru", "Запрос HTTP не разрешен для схемы аутентификации клиента \"{0}\". От сервера получен заголовок аутентификации \"{1}\".")]
        [DataRow("de", "Die HTTP-Anforderung ist beim Clientauthentifizierungsschema \"{0}\" nicht autorisiert. Vom Server wurde der Authentifizierungsheader \"{1}\" empfangen.")]
        [DataRow("fr", "La requête HTTP n'est pas autorisée avec un schéma d'authentification client '{0}'. L'en-tête d'authentification reçu du serveur était '{1}'.")]
        [DataRow("tr", "HTTP isteği, istemci kimlik doğrulama düzeni '{0}' içinde yetkilendirilmemiş. Sunucudan alınan kimlik denetimi üst bilgisi: '{1}'.")]
        [DataRow("cs", "Požadavek protokolu HTTP je neoprávněný se schématem autorizace klienta {0}. Záhlaví ověření přijaté ze serveru je {1}.")]
        [DataRow("pl", "Żądanie protokołu HTTP nie jest autoryzowane na podstawie schematu uwierzytelniania klienta „{0}”. Nagłówek uwierzytelnienia otrzymany z serwera to „{1}”.")]
        [DataRow("pt-BR", "A solicitação HTTP não está autorizada no esquema de autenticação de cliente '{0}'. O cabeçalho de autenticação recebido do servidor foi '{1}'.")]
        [DataRow("it", "La richiesta HTTP non è autorizzata con lo schema di autenticazione client '{0}'. Intestazione di autenticazione ricevuta dal server: '{1}'.")]
        [DataRow("ko", "HTTP 요청이 클라이언트 인증 구성표 '{0}'(으)로 인증되지 않습니다. 서버에서 수신한 인증 헤더가 '{1}'입니다.")]
        [DataRow("es", "La solicitud HTTP no está autorizada con el esquema de autenticación de cliente \"{0}\". El encabezado de autenticación recibido del servidor era \"{1}\".")]
        [TestMethod]
        public void TestLocalizedMessage(string culture, string format)
        {
            string message = HttpDigestChallengeSource.CreateExceptionMessage(format, MD5Challenge, Sha256Challenge);
            var challenges = HttpDigestChallengeSource.GetChallenges(message);

            CollectionAssert.AreEqual(new[] { MD5Challenge, Sha256Challenge }, challenges, culture);
        }

        /// <summary>
        /// The message reported for Hikvision cameras, which enclose the header in typographic quotes.
        /// </summary>
        [TestMethod]
        public void TestHikvision()
        {
            const string challenge = "Digest qop=\"auth\", realm=\"IP Camera(FN636)\", nonce=\"663338373a34393137373736373ace51538fd6d7317abb7651757a8a65af\", stale=\"FALSE\"";
            string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.ZhHans, challenge);

            CollectionAssert.AreEqual(new[] { challenge }, HttpDigestChallengeSource.GetChallenges(message));
        }

        /// <summary>
        /// A device offers one challenge per algorithm it supports, all in the same response.
        /// </summary>
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(12)]
        [TestMethod]
        public void TestMultipleChallenges(int count)
        {
            var challenges = Enumerable.Range(0, count)
                .Select(x => $"Digest realm=\"My IP Camera\", qop=\"auth, auth-int\", algorithm=SHA-256, nonce=\"n{x}\", opaque=\"o\", userhash=TRUE, stale=FALSE")
                .ToArray();

            string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, challenges);

            CollectionAssert.AreEqual(challenges, HttpDigestChallengeSource.GetChallenges(message));
        }

        /// <summary>
        /// Everything the server side of SharpOnvif can offer has to survive the round trip.
        /// </summary>
        [TestMethod]
        public void TestGeneratedChallenges()
        {
            string[] algorithms = { "", "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess" };
            string[] realms = { "My IP Camera", "Camera, Hall", "Camera = 1; qop=auth" };
            var timestamp = DateTimeOffset.UtcNow;
            byte[] salt = HttpDigestAuthentication.CreateNonceSessionSalt();

            foreach (var serialization in new[] { BinarySerializationType.Hex, BinarySerializationType.Base64 })
            {
                foreach (string algorithm in algorithms)
                {
                    foreach (string realm in realms)
                    {
                        foreach (bool stale in new[] { false, true })
                        {
                            var challenges = new List<string>
                            {
                                HttpDigestAuthentication.CreateWwwAuthenticateRFC2069(
                                    "MD5", serialization, timestamp, algorithm, null, salt, realm, "00000000", stale)
                            };

                            foreach (string qop in new[] { "auth", "auth-int", "auth, auth-int" })
                            {
                                challenges.Add(HttpDigestAuthentication.CreateWwwAuthenticateRFC2617(
                                    "MD5", serialization, timestamp, algorithm, null, salt, realm, "00000000", qop, stale));

                                foreach (bool userhash in new[] { false, true })
                                {
                                    challenges.Add(HttpDigestAuthentication.CreateWwwAuthenticateRFC7616(
                                        "MD5", serialization, timestamp, algorithm, null, salt, realm, "00000000", qop, "UTF-8", userhash, stale));
                                }
                            }

                            foreach (string challenge in challenges)
                            {
                                // Czech is the format that encloses the header in nothing at all
                                string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, challenge);
                                CollectionAssert.AreEqual(new[] { challenge }, HttpDigestChallengeSource.GetChallenges(message), challenge);
                            }
                        }
                    }
                }
            }
        }

        [DataRow("Digest realm=\"testrealm@host.com\", nonce=\"dcd98b7102dd2f0e8b11d0f600bfb0c093\", opaque=\"5ccc069c403ebaf9f0171e9517f40e41\"", DisplayName = "RFC 2069")]
        [DataRow("Digest realm=\"testrealm@host.com\", qop=\"auth,auth-int\", nonce=\"dcd98b7102dd2f0e8b11d0f600bfb0c093\", opaque=\"5ccc069c403ebaf9f0171e9517f40e41\"", DisplayName = "RFC 2617")]
        [DataRow("Digest realm=\"http-auth@example.org\", qop=\"auth, auth-int\", algorithm=SHA-256, nonce=\"7ypf/xlj9XXwfDPEoM4URrv/xwf94BcCAzFZH4GiTo0v\", opaque=\"FQhe/qaU925kfnzjCev0ciny7QMkPqMAFRtzCUYo5tdS\"", DisplayName = "RFC 7616 SHA-256")]
        [DataRow("Digest realm=\"api@example.org\", qop=\"auth\", algorithm=SHA-512-256, nonce=\"5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK\", opaque=\"HRPCssKJSGjCrkzDg8OhwpzCiGPChXYjwrI2QmXDnsOS\", charset=UTF-8, userhash=true", DisplayName = "RFC 7616 userhash")]
        [DataRow("Digest realm=\"testrealm\", domain=\"/foo /bar/baz\", qop=\"auth\", nonce=\"abc\"", DisplayName = "domain with spaces")]
        [DataRow("Digest realm=\"IP Camera\", qop=auth, nonce=\"abc\", stale=FALSE", DisplayName = "unquoted qop")]
        [TestMethod]
        public void TestRfcExamples(string challenge)
        {
            foreach (string format in new[] { HttpDigestMessages.EnUS, HttpDigestMessages.Cs, HttpDigestMessages.De, HttpDigestMessages.ZhHans })
            {
                string message = HttpDigestChallengeSource.CreateExceptionMessage(format, challenge);
                CollectionAssert.AreEqual(new[] { challenge }, HttpDigestChallengeSource.GetChallenges(message));
            }
        }

        /// <summary>
        /// The text that follows the challenge must not end up in the last auth-param. When it does, the
        ///  client reads back a "stale" it cannot recognize and reports valid credentials as invalid.
        /// </summary>
        [DataRow(HttpDigestMessages.EnUS)]
        [DataRow(HttpDigestMessages.ZhHans)]
        [DataRow(HttpDigestMessages.De)]
        [DataRow(HttpDigestMessages.Cs)]
        [TestMethod]
        public void TestTrailingTextIsNotAbsorbed(string format)
        {
            const string challenge = "Digest realm=\"IP Camera\", qop=\"auth, auth-int\", nonce=\"abc\", stale=TRUE";

            var challenges = HttpDigestChallengeSource.GetChallenges(
                HttpDigestChallengeSource.CreateExceptionMessage(format, challenge));

            CollectionAssert.AreEqual(new[] { challenge }, challenges);
            Assert.AreEqual("TRUE", HttpDigestAuthentication.GetValueFromHeader(challenges[0], "stale", false));
        }

        /// <summary>
        /// The client authentication scheme is named in the same message and can be called "Digest" too.
        /// </summary>
        [DataRow(HttpDigestMessages.EnUS)]
        [DataRow(HttpDigestMessages.Cs)]
        [TestMethod]
        public void TestSchemeNamedDigestIsNotAChallenge(string format)
        {
            string message = string.Format(format, "Digest", MD5Challenge);

            CollectionAssert.AreEqual(new[] { MD5Challenge }, HttpDigestChallengeSource.GetChallenges(message));
        }

        [DataRow("Basic realm=\"IP Camera\", Digest realm=\"IP Camera\", qop=\"auth\", nonce=\"abc\"", DisplayName = "Basic first")]
        [DataRow("Digest realm=\"IP Camera\", qop=\"auth\", nonce=\"abc\", Basic realm=\"IP Camera\"", DisplayName = "Basic last")]
        [TestMethod]
        public void TestOtherSchemesAreIgnored(string header)
        {
            string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, header);

            CollectionAssert.AreEqual(
                new[] { "Digest realm=\"IP Camera\", qop=\"auth\", nonce=\"abc\"" },
                HttpDigestChallengeSource.GetChallenges(message));
        }

        /// <summary>
        /// A 403 is reported as a MessageSecurityException as well, but carries no challenge.
        /// </summary>
        [DataRow("The HTTP request was forbidden with client authentication scheme 'Anonymous'.")]
        [DataRow("Požadavek protokolu HTTP byl zakázán se schématem autorizace klienta Anonymous.")]
        [DataRow("The remote server returned an error: (500) Internal Server Error.")]
        [TestMethod]
        public void TestMessageWithoutChallenge(string message)
        {
            Assert.IsNull(HttpDigestChallengeSource.GetChallenges(message));
        }

        /// <summary>
        /// The header is chosen by the device, so it is hostile input. Anything quadratic or worse here
        ///  would not finish - .NET caps response headers at 64 KB, so 128 KB is already twice the ceiling.
        /// </summary>
        [TestMethod]
        public void TestAdversarialInput()
        {
            const int length = 128000;
            string[] inputs =
            {
                "Digest " + new string('a', length),
                "Digest " + new string('a', length) + "=",
                "Digest a=\"" + new string('x', length),
                "Digest a=" + new string('"', length),
                "Digest " + Repeat("a=b, ", length / 5),
                "Digest " + Repeat("a=b, ", length / 5) + "”。",
                Repeat("Digest ", length / 7),
                Repeat("Digest a=", length / 9),
                "Digest " + Repeat("a = , ", length / 6),
                "Digest a=\"" + Repeat("b, c=\\\"", length / 7),
                "Digest a" + new string(' ', length) + "=b",
                "Digest a=b" + Repeat(" , ", length / 3),
                HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, MD5Challenge) + new string('a', length),
            };

            var stopwatch = Stopwatch.StartNew();
            foreach (string input in inputs)
            {
                HttpDigestChallengeSource.GetChallenges(input);
            }
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 5, $"parsing took {stopwatch.Elapsed.TotalSeconds:0.0} s");
        }

        /// <summary>
        /// Matching the "Digest" scheme is case insensitive, which must not depend on the current culture -
        ///  in Turkish the upper case of "i" is not "I".
        /// </summary>
        [DataRow("tr-TR")]
        [DataRow("en-US")]
        [TestMethod]
        public void TestCultureDoesNotAffectMatching(string culture)
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

                foreach (string scheme in new[] { "Digest", "DIGEST", "digest" })
                {
                    string challenge = scheme + " realm=\"IP Camera\", qop=\"auth\", nonce=\"abc\"";
                    string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, challenge);

                    Assert.AreEqual(1, HttpDigestChallengeSource.GetChallenges(message).Length, scheme);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        private static string Repeat(string value, int count)
        {
            var builder = new StringBuilder(value.Length * count);
            for (int i = 0; i < count; i++)
            {
                builder.Append(value);
            }
            return builder.ToString();
        }
    }
}
