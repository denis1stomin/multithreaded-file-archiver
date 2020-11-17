using System.IO;

namespace GzipArchiver
{
    public interface IResultWriter
    {
        void WritePortion(Stream portion);
    }
}
