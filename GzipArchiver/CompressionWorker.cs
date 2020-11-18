using System.IO;
using System.IO.Compression;

namespace GzipArchiver
{
    public class CompressionWorker : IWorker
    {
        public Stream HandlePortion(Stream portion)
        {
            var resultStream = new MemoryStream();
            using (var gzipStream = new GZipStream(resultStream, CompressionMode.Decompress))
                portion.CopyTo(gzipStream);
            
            resultStream.Position = 0;
            return resultStream;
        }
    }
}
