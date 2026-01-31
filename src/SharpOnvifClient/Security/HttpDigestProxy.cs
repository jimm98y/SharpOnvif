using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SharpOnvifClient.Security
{
    public class HttpDigestProxy<T> : DispatchProxy where T : class
    {
        public T Target { get; private set; }

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
                    result = targetMethod.Invoke(Target, args);
                }
                return result;
            }            
        }

        public static async Task<TResult> Cast<TResult>(Task<object> task)
        {
            return (TResult)await task.ConfigureAwait(false);
        }

        public static T CreateProxy(T target)
        {
            var proxy = Create<T, HttpDigestProxy<T>>() as HttpDigestProxy<T>;
            proxy.Target = target;
            return proxy as T;
        }
    }
}
