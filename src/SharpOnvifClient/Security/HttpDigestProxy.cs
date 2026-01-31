using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharpOnvifClient.Security
{
    public class HttpDigestProxy<T> : DispatchProxy where T : class
    {
        public T Target { get; private set; }
        public HttpDigestState State { get; private set; }

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
                    catch(System.ServiceModel.Security.MessageSecurityException ex)
                    {
                        State.SetResponse(ParseMessageSecurityException(ex.Message));

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
                        State.SetResponse(ParseMessageSecurityException(ex.Message));

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
                    State.SetResponse(ParseMessageSecurityException(ex.Message));

                    result = targetMethod.Invoke(Target, args);
                }
                return result;
            }            
        }

        private IEnumerable<string> ParseMessageSecurityException(string message)
        {
            // Workaround: The only way to get the response WwwAuthenticate headers from WCF seems to be
            //  by parsing them from the Exception message text. This is not ideal and we have to be careful
            //  to not use any hardcoded strings as the exception message might be localized.

            /*            
            The HTTP request is unauthorized with client authentication scheme 'Anonymous'. 
            The authentication header received from the server was 
            'Digest realm="IP Camera", qop="auth, auth-int", nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE", Digest realm="IP Camera", qop="auth, auth-int", algorithm=SHA-256, nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE", Digest realm="IP Camera", qop="auth, auth-int", algorithm=SHA-512-256, nonce="0000019c15330aa11b968762b51d15c40ba2f12eb2cb1ec02427a1d82440325adbff100bc3f35d74a401e5d533f271cd5e81101a", opaque="00000000", userhash=TRUE, stale="FALSE"'.
            */
            string[] parts = message.Split('\'');
            string wwwAuthenticateHeadersConcatenated = parts.Single(x => x.StartsWith("Digest", StringComparison.InvariantCultureIgnoreCase));
            string[] wwwAuthenticateHeaderParts = wwwAuthenticateHeadersConcatenated.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            List<string> wwwAuthenticateHeaders = new List<string>();
            StringBuilder stringBuilder = null;
            for (int i = 0; i < wwwAuthenticateHeaderParts.Length; i++)
            {
                if(wwwAuthenticateHeaderParts[i].TrimStart().StartsWith("Digest", StringComparison.InvariantCultureIgnoreCase))
                {
                    if(stringBuilder != null)
                    {
                        wwwAuthenticateHeaders.Add(stringBuilder.ToString());
                    }

                    stringBuilder = new StringBuilder();
                    stringBuilder.Append(wwwAuthenticateHeaderParts[i].TrimStart());
                }
                else if(stringBuilder != null)
                {
                    stringBuilder.Append(",");
                    stringBuilder.Append(wwwAuthenticateHeaderParts[i]);
                }
                else
                {
                    Debug.WriteLine($"Error parsing MessageException text");
                }
            }
            if(stringBuilder != null)
            {
                wwwAuthenticateHeaders.Add(stringBuilder.ToString());
            }

            return wwwAuthenticateHeaders;
        }

        public static async Task<TResult> Cast<TResult>(Task<object> task)
        {
            return (TResult)await task.ConfigureAwait(false);
        }

        public static T CreateProxy(T target, HttpDigestState state)
        {
            var proxy = Create<T, HttpDigestProxy<T>>() as HttpDigestProxy<T>;
            proxy.Target = target;
            proxy.State = state;
            return proxy as T;
        }
    }
}
