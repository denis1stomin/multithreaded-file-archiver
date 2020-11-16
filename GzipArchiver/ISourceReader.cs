using System.IO;

namespace GzipArchiver
{
    interface ISourceReader
    {
        MemoryStream ReadNextPortion();
    }
}
