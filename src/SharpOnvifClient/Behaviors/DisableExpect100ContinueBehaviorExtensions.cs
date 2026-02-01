using System;
using System.ServiceModel;

namespace SharpOnvifClient.Behaviors
{
    public static class DisableExpect100ContinueBehaviorExtensions
    {
        public static void SetDisableExpect100Continue<TChannel>(
            this ClientBase<TChannel> channel,
            System.ServiceModel.Description.IEndpointBehavior disableExpect100Continue = null) where TChannel : class
        {
            SetDisableExpect100Continue(channel as TChannel, disableExpect100Continue); 
        }

        public static void SetDisableExpect100Continue<TChannel>(
            TChannel wcfChannel,
            System.ServiceModel.Description.IEndpointBehavior disableExpect100Continue = null) where TChannel : class
        {
            if (disableExpect100Continue == null)
                return;

            var channel = wcfChannel as ClientBase<TChannel>;
            if (channel == null)
                throw new ArgumentException($"{wcfChannel} is not WCF {nameof(ClientBase<TChannel>)}");

            if (!channel.Endpoint.EndpointBehaviors.Contains(disableExpect100Continue))
            {
                channel.Endpoint.EndpointBehaviors.Add(disableExpect100Continue);
            }
        }
    }
}
