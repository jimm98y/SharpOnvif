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
        private static readonly ConcurrentDictionary<Type, ServiceDispatcher> Cache =
            new ConcurrentDictionary<Type, ServiceDispatcher>();

        /// <summary>The dispatcher for a service implementation, or null when it has none.</summary>
        public static ServiceDispatcher For(Type implementationType)
        {
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));
            return Cache.GetOrAdd(implementationType, Resolve);
        }

        /// <summary>
        /// Finds the dispatcher a generated service base carries, by what it is rather than by
        /// what it is called.
        /// </summary>
        /// <remarks>
        /// The generator writes that property and this reads it back, which used to be a string
        /// agreed in two places and checked in neither: renaming it compiled on both sides and
        /// stopped every request being routed. A static property of this type is what is being
        /// looked for, and a name is free to change.
        /// </remarks>
        private static ServiceDispatcher Resolve(Type implementationType)
        {
            for (Type type = implementationType; type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (PropertyInfo property in type.GetProperties(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (property.GetIndexParameters().Length == 0
                        && typeof(ServiceDispatcher).IsAssignableFrom(property.PropertyType))
                    {
                        return (ServiceDispatcher)property.GetValue(null);
                    }
                }
            }

            return null;
        }
    }
}
