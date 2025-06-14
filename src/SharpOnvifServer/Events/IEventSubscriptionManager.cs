namespace SharpOnvifServer.Events
{
    public interface IEventSubscriptionManager<T> where T : class, IEventSubscription
    {
        int AddSubscription(T subscription);
        T GetSubscription(int subscriptionID);
        void RemoveSubscription(int subscriptionID);
    }
}
