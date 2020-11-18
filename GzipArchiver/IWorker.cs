using System.IO;

namespace GzipArchiver
{
    public interface IWorker
    {
        Stream HandlePortion(Stream portion);
    }
}
