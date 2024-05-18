using OnvifEvents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpOnvifClient.Client
{
    public static class SimpleOnvifClientExtensions
    {
        public static Task<OnvifMedia.MediaUri> GetStreamUriAsync(this SimpleOnvifClient client, OnvifMedia.Profile profile)
        {
            return client.GetStreamUriAsync(profile.token);
        }

        public static Task<PullMessagesResponse> PullPointPullMessagesAsync(this SimpleOnvifClient client, CreatePullPointSubscriptionResponse subscribeResponse, int timeoutInSeconds = 60, int maxMessages = 100)
        {
            return client.PullPointPullMessagesAsync(subscribeResponse.SubscriptionReference.Address.Value, timeoutInSeconds, maxMessages);
        }

        public static Task<RenewResponse1> BasicSubscriptionRenewAsync(this SimpleOnvifClient client, SubscribeResponse1 subscribeResponse, int timeoutInMinutes = 5)
        {
            return client.BasicSubscriptionRenewAsync(subscribeResponse.SubscribeResponse.SubscriptionReference.Address.Value, timeoutInMinutes);
        }
    }
}
