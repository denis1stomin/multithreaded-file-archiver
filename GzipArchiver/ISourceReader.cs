using System.IO;

namespace GzipArchiver
{
    public interface ISourceReader
    {
        Stream ReadNextPortion();
    }
}
