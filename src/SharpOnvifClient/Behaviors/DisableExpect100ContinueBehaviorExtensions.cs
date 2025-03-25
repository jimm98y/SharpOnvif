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
            if (disableExpect100Continue == null)
                return;

            if (!channel.Endpoint.EndpointBehaviors.Contains(disableExpect100Continue))
            {
                channel.Endpoint.EndpointBehaviors.Add(disableExpect100Continue);
            }
        }
    }
}
