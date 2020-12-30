using System.IO;
using System.IO.Compression;

namespace GzipArchiver
{
    public class DecompressionWorker : IWorker
    {
        public Stream HandlePortion(Stream portion)
        {
            var resultStream = new MemoryStream();
            using (var gzipStream = new GZipStream(portion, CompressionMode.Decompress, true))
                gzipStream.CopyTo(resultStream);

            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }
    }
}
