using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class PullPointBase : PullPoint
    {
        public virtual DestroyPullPointResponse1 DestroyPullPoint(DestroyPullPointRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetMessagesResponse1 GetMessages(GetMessagesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void Notify(Notify1 request)
        {
            throw new NotImplementedException();
        }
    }
}
