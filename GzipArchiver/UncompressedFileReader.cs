using System;
using System.IO;

namespace GzipArchiver
{
    public class UncompressedFileReader : ISourceReader
    {
        public string FilePath { get; }
        public int PortionSizeBytes { get; }

        public UncompressedFileReader(string filePath, int portionSize)
        {
            FilePath = !string.IsNullOrWhiteSpace(filePath) ?
                filePath : throw new ArgumentException(nameof(filePath));
            
            PortionSizeBytes = (portionSize > 0) ?
                portionSize : throw new ArgumentOutOfRangeException(nameof(portionSize));

            _sourceStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream ReadNextPortion()
        {
            var portion = ReadExactNumberOfBytes();
            
            if (portion != null)
                return new MemoryStream(portion);

            return null;
        }

        public void Dispose()
        {
            _sourceStream?.Dispose();
            _sourceStream = null;
        }

        private byte[] ReadExactNumberOfBytes()
        {
            // TODO : add try-catch for the case something bad with OS file system etc

            int readCnt = 0;
            var portion = new byte[PortionSizeBytes];

            while (readCnt < PortionSizeBytes)
            {
                var justRead = _sourceStream.Read(portion, readCnt, PortionSizeBytes - readCnt);
                if (justRead == 0)
                    break;

                readCnt += justRead;
            }

            // end of stream
            if (readCnt == 0)
                return null;

            // crop unused size
            if (readCnt < PortionSizeBytes)
                Array.Resize(ref portion, readCnt);

            return portion;
        }

        private FileStream _sourceStream;
    }
}
