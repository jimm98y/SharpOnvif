using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// The body of an Onvif request, read once.
    /// <para>
    /// Authentication needs it - for the action a PRE_AUTH request names in its header, for a
    /// WS-UsernameToken, for an auth-int digest - and so does the endpoint. Each used to read and
    /// copy it again, and parse it again; the bytes are kept here for the rest of the request.
    /// </para>
    /// </summary>
    internal static class OnvifRequestBody
    {
        private static readonly object Key = new object();

        /// <summary>Recorded for a body over the limit, so a second reader refuses it without reading.</summary>
        private static readonly object TooLarge = new object();

        /// <summary>
        /// The request body. Null when the read was cancelled before the body was complete.
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// The body is larger than <see cref="OnvifEndpoint.MaxRequestBytes"/>.
        /// </exception>
        public static async Task<byte[]> ReadAsync(HttpContext context)
        {
            if (context.Items.TryGetValue(Key, out object kept))
            {
                if (kept == TooLarge) throw Refused();
                return (byte[])kept;
            }

            long maximum = OnvifEndpoint.MaxRequestBytes;

            long? declared = context.Request.ContentLength;
            if (declared.HasValue && declared.Value > maximum)
            {
                context.Items[Key] = TooLarge;
                throw Refused();
            }

            PipeReader reader = context.Request.BodyReader;
            while (true)
            {
                ReadResult read = await reader.ReadAsync(context.RequestAborted).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = read.Buffer;

                try
                {
                    if (read.IsCanceled) return null;

                    // A chunked request declares no length, so the limit is enforced as it arrives.
                    if (buffer.Length > maximum)
                    {
                        context.Items[Key] = TooLarge;
                        throw Refused();
                    }

                    if (read.IsCompleted)
                    {
                        byte[] body = buffer.ToArray();
                        context.Items[Key] = body;
                        return body;
                    }
                }
                finally
                {
                    // Consumed nothing, examined everything: the next read waits for more, and the
                    // body is still there for anything else in the pipeline that reads it.
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        private static InvalidDataException Refused() =>
            new InvalidDataException("The request is larger than this endpoint accepts.");
    }
}
