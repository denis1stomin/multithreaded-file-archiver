using System.IO;
using System.IO.Compression;

namespace GzipArchiver
{
    public class CompressionWorker : IWorker
    {
        public Stream HandlePortion(Stream portion)
        {
            var resultStream = new MemoryStream();
            using (var gzipStream = new GZipStream(resultStream, CompressionMode.Compress, true))
                portion.CopyTo(gzipStream);
            
            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }
    }
}
