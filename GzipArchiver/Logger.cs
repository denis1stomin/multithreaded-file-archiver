using System;

namespace GzipArchiver
{
    public class Logger : ILogger
    {
        public void Log(string msg)
        {
            Console.WriteLine($"{DateTime.UtcNow} : {msg}");
        }
    }
}
