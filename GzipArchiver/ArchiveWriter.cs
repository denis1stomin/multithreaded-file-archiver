using System;
using System.IO;

namespace GzipArchiver
{
    public class ArchiveWriter : IResultWriter, IDisposable
    {
        public ArchiveWriter(string archivePath)
        {}

        public void WritePortion(Stream portion)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
