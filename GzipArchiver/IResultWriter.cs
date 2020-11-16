using System.IO;

namespace GzipArchiver
{
    interface IResultWriter
    {
        void WritePortion(int index, MemoryStream portion);
    }
}
