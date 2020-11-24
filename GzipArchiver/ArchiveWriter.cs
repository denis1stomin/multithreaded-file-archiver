using System;
using System.IO;

namespace GzipArchiver
{
    public class ArchiveWriter : IResultWriter
    {
        public ArchiveWriter(string archivePath)
            : this (new PartitionedArchiveComposer(archivePath, IArchiveComposer.OpenMode.Write))
        {
            _doDispose = true;
        }

        public ArchiveWriter(IArchiveComposer composer)
        {
            _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        }

        public void WritePortion(Stream portion)
        {
            _composer.WritePortion(portion as MemoryStream);
        }

        public void Dispose()
        {
            if (_doDispose)
                _composer.Dispose();
        }

        private IArchiveComposer _composer;
        private bool _doDispose = false;
    }
}
