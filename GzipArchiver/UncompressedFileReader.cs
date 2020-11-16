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
                portionSize : throw new ArgumentException(nameof(portionSize));
        }

        public MemoryStream ReadNextPortion()
        {
            throw new NotImplementedException();
        }
    }
}
