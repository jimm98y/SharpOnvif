using System;
using System.Diagnostics;
using System.Reflection;

namespace SharpOnvifClient.Security
{
    public class BasicObjectProxy<T> : DispatchProxy where T: class
    {
        public T Target { get; private set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            object result = targetMethod.Invoke(Target, args);
            return result;
        }

        public static T CreateProxy(T target)
        {
            var proxy = Create<T, BasicObjectProxy<T>>() as BasicObjectProxy<T>;
            proxy.Target = target;
            return proxy as T;
        }
    }
}
