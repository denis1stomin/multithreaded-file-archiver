using System;

namespace GzipArchiver
{
    public class Logger : ILogger
    {
        public Logger(int verbosity = 1)
        {
            _verbosity = verbosity;
        }

        public void Log(string msg)
        {
            if (_verbosity > 0)
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                Console.WriteLine($"{timestamp} : {msg}");
            }
        }

        private int _verbosity = 1;
    }
}
