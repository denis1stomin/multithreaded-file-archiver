using System;
using System.IO;

namespace GzipArchiver
{
    public partial class CompressionPipeline
    {
        public class PortionTicket : IDisposable
        {
            public long Index { get; }
            public Stream Data { get; }

            public PortionTicket(long index, Stream data)
            {
                Index = (index >= 0) ?
                    index : throw new ArgumentOutOfRangeException(nameof(index));
                
                Data = data ?? throw new ArgumentNullException(nameof(data));
            }

            public void Dispose()
            {
                Data?.Dispose();
            }
        }
    }
}
