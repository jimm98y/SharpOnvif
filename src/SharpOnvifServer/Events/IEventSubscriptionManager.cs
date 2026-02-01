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
