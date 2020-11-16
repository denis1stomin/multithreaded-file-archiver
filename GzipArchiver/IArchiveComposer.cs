using System.IO;

namespace GzipArchiver
{
    public interface IArchiveComposer
    {
        public enum OpenMode
        {
            Read,
            Write
        }

        string FilePath { get; }
        OpenMode Mode { get; }

        void WritePortion(MemoryStream portion);
        MemoryStream ReadNextPortion();
        void Close();
    }
}
