using System.IO;

namespace GzipArchiver
{
    public interface ISourceReader
    {
        string FilePath { get; }
        int PortionSizeBytes { get; }

        Stream ReadNextPortion();
    }
}
