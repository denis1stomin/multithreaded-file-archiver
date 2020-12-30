using System;
using System.IO;

namespace GzipArchiver
{
    public interface ISourceReader : IDisposable
    {
        Stream ReadNextPortion();
    }
}
