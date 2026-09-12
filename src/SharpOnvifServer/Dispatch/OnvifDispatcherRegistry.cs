using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// Finds the dispatcher that belongs to a service implementation.
    /// <para>
    /// Each generated service base carries a static <c>OnvifDispatcher</c> property, and the
    /// implementation the host registers derives from it. Walking the base chain finds it without
    /// the host having to name the dispatcher itself.
    /// </para>
    /// </summary>
    public static class OnvifDispatcherRegistry
    {
        private static readonly ConcurrentDictionary<Type, OnvifServiceDispatcher> Cache =
            new ConcurrentDictionary<Type, OnvifServiceDispatcher>();

        /// <summary>The dispatcher for a service implementation, or null when it has none.</summary>
        public static OnvifServiceDispatcher For(Type implementationType)
        {
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));
            return Cache.GetOrAdd(implementationType, Resolve);
        }

        private static OnvifServiceDispatcher Resolve(Type implementationType)
        {
            for (Type type = implementationType; type != null && type != typeof(object); type = type.BaseType)
            {
                PropertyInfo property = type.GetProperty(
                    "OnvifDispatcher",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                if (property != null && typeof(OnvifServiceDispatcher).IsAssignableFrom(property.PropertyType))
                    return (OnvifServiceDispatcher)property.GetValue(null);
            }

            return null;
        }
    }
}
