using System;
using System.IO;

namespace GzipArchiver
{
    public class AsIsResultWriter : IResultWriter
    {
        public AsIsResultWriter(string outputPath)
            : this(InitStream(outputPath))
        {
        }

        // Just an example of DI principle but in practice
        //   it does not make a lot sense here with Stream class
        public AsIsResultWriter(Stream outputStream)
        {
            _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        }
        
        public void WritePortion(Stream portion)
        {
            portion.CopyTo(_outputStream);
        }

        public void Dispose()
        {
            _outputStream?.Dispose();
        }

        private static Stream InitStream(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException(nameof(outputPath));

            return new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        private Stream _outputStream;
    }
}
