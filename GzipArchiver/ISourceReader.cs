using System.IO;

namespace GzipArchiver
{
    interface ISourceReader
    {
        string FilePath { get; }
        int PortionSizeBytes { get; }

        MemoryStream ReadNextPortion();
    }
}
