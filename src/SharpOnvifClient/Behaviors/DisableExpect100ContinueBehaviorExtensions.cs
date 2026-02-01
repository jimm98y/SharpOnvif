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
