using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// Finds the dispatcher that belongs to a service implementation.
    /// <para>
    /// Each generated service base carries a static <c>Dispatcher</c> property, and the
    /// implementation the host registers derives from it. Walking the base chain finds it without
    /// the host having to name the dispatcher itself.
    /// </para>
    /// </summary>
    public static class OnvifDispatcherRegistry
    {
        /// <summary>
        /// What the generated service base calls the dispatcher it carries. Named here because
        /// this is the only place that knows it - the generator writes the property, this reads
        /// it back, and nothing else mentions it.
        /// </summary>
        internal const string DispatcherProperty = "Dispatcher";

        private static readonly ConcurrentDictionary<Type, ServiceDispatcher> Cache =
            new ConcurrentDictionary<Type, ServiceDispatcher>();

        /// <summary>The dispatcher for a service implementation, or null when it has none.</summary>
        public static ServiceDispatcher For(Type implementationType)
        {
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));
            return Cache.GetOrAdd(implementationType, Resolve);
        }

        private static ServiceDispatcher Resolve(Type implementationType)
        {
            for (Type type = implementationType; type != null && type != typeof(object); type = type.BaseType)
            {
                PropertyInfo property = type.GetProperty(
                    DispatcherProperty,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                if (property != null && typeof(ServiceDispatcher).IsAssignableFrom(property.PropertyType))
                    return (ServiceDispatcher)property.GetValue(null);
            }

            return null;
        }
    }
}
