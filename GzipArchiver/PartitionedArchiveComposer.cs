using System;
using System.IO;
using System.Text;

namespace GzipArchiver
{
    public class PartitionedArchiveComposer : IArchiveComposer, IDisposable
    {
        public string FilePath { get; }
        public IArchiveComposer.OpenMode Mode { get; }

        public PartitionedArchiveComposer(string path, IArchiveComposer.OpenMode mode)
        {
            FilePath = !string.IsNullOrWhiteSpace(path) ? path : throw new ArgumentException(nameof(path));
            Mode = mode;

            if (Mode == IArchiveComposer.OpenMode.Read)
                OpenRead();
            else
                OpenWrite();
        }

        public void WritePortion(MemoryStream portion)
        {
            if (Mode != IArchiveComposer.OpenMode.Write)
                throw new InvalidOperationException($"instance is initialized for {Mode} mode");

            _portionIndex ++;

            var portionFilePath = $"{FilePath}_{_portionIndex}";

            using (var portionStream =
                new FileStream(portionFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                portion.CopyTo(portionStream);
            }

            var portionInfo = $"{_portionIndex},{portionFilePath}";
            _mainWriter.WriteLine(portionInfo);
        }

        public MemoryStream ReadNextPortion()
        {
            if (Mode != IArchiveComposer.OpenMode.Read)
                throw new InvalidOperationException($"instance is initialized for {Mode} mode");

            _portionIndex ++;

            do
            {
                var portionInfo = _mainReader.ReadLine();
                // end of file
                if (portionInfo == null)
                    return null;
                
                var portionArr = portionInfo.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (portionArr.Length < 2)
                    // or FileFormatException or some another custom exception
                    throw new FormatException("corrupted archive");

                var parsed = int.TryParse(portionArr[0], out var parsedIndex);
                if (!parsed)
                    throw new FormatException("corrupted archive");

                // TODO : does it make sense to check order here?
                //if (parsedIndex != _portionIndex)
                
                MemoryStream portion = null; //GetNextPortionFromCache(parsedIndex);
                if (portion == null)
                {
                    var portionFilePath = portionArr[1];
                    portion = GetNextPortionFromFile(portionFilePath);
                }

                return portion;
            }
            while (true);
        }

        public void Close()
        {
            _mainReader?.Close();
            _mainWriter?.Close();
        }

        public void Dispose()
        {
            _mainReader?.Dispose();
            _mainWriter?.Dispose();
        }

        private MemoryStream GetNextPortionFromFile(string filePath)
        {
            // TODO : try-catch FileNotFound => CorruptedArchive etc

            using (var portionStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var portion = new MemoryStream();
                portionStream.CopyTo(portion);
                portion.Position = 0;

                return portion;
            }
        }

        private void OpenRead()
        {
            // TODO : we could try-catch here and other functions to introduce own error abstraction layer

            var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _mainReader = new StreamReader(stream, _fileEncoding);
        }

        private void OpenWrite()
        {
            var stream = new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            _mainWriter = new StreamWriter(stream, _fileEncoding);
        }

        private StreamWriter _mainWriter;
        private StreamReader _mainReader;
        private long _portionIndex = -1;
        
        private Encoding _fileEncoding = Encoding.UTF8;
    }
}
