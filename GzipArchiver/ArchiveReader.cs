using System;
using System.IO;

namespace GzipArchiver
{
    public class ArchiveReader : ISourceReader
    {
        public ArchiveReader(string archivePath)
            : this (new PartitionedArchiveComposer(archivePath, IArchiveComposer.OpenMode.Read))
        {
            _doDispose = true;
        }

        public ArchiveReader(IArchiveComposer composer)
        {
            _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        }

        public Stream ReadNextPortion()
        {
            return _composer.ReadNextPortion();
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
