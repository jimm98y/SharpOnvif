using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class CreatePullPointBase : CreatePullPoint
    {
        public virtual CreatePullPointResponse1 CreatePullPoint(CreatePullPointRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
