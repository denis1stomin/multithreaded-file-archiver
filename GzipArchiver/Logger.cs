using System;

namespace GzipArchiver
{
    public class Logger : ILogger
    {
        public void Log(string msg)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            Console.WriteLine($"{timestamp} : {msg}");
        }
    }
}
