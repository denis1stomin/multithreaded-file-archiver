using System;
using System.IO;

namespace GzipArchiver
{
    public class ArchiveReader : ISourceReader, IDisposable
    {
        public ArchiveReader(string archivePath)
            : this (new PartitionedArchiveComposer(archivePath, IArchiveComposer.OpenMode.Read))
        {
        }

        public ArchiveReader(IArchiveComposer composer)
        {
            _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        }

        public Stream ReadNextPortion()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // TODO
        }

        IArchiveComposer _composer;
    }
}
