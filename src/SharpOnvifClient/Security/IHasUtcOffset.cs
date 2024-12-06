using System;

namespace SharpOnvifClient.Security
{
    public interface IHasUtcOffset
    {
        TimeSpan UtcNowOffset { get; set; }
    }
}
