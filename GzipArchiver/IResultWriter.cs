using System;
using System.IO;

namespace GzipArchiver
{
    public interface IResultWriter : IDisposable
    {
        void WritePortion(Stream portion);
    }
}
