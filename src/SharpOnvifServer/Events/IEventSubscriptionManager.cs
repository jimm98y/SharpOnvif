namespace SharpOnvifServer.Events
{
    /// <summary>
    /// Event Subscription Manager manages the lifetime of the event subscriptions on the server. It is responsible
    ///  for creating, maintaining and removing the event subscriptions as well as managing their lifetime.
    /// </summary>
    /// <typeparam name="T">New Event Subscription <see cref="IEventSubscription"/>.</typeparam>
    public interface IEventSubscriptionManager<T> where T : class, IEventSubscription
    {
        /// <summary>
        /// Add a new event subscription.
        /// </summary>
        /// <param name="subscription">New event subscription.</param>
        /// <returns>Event subscription ID used to identify the subscription in subsequent requests.</returns>
        int AddSubscription(T subscription);

        /// <summary>
        /// Returns an existing Event subscription from its subscription ID.
        /// </summary>
        /// <param name="subscriptionID">Subscription ID used to identify the subscription.</param>
        /// <returns>Event subscription.</returns>
        T GetSubscription(int subscriptionID);

        /// <summary>
        /// Removes the Event subscription.
        /// </summary>
        /// <param name="subscriptionID">Subscription ID used to identify the subscription.</param>
        void RemoveSubscription(int subscriptionID);
    }
}
