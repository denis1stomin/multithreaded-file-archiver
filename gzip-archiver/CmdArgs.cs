using System;

namespace GzipArchiver
{
    class CmdArgsException : ArgumentException
    {
        public CmdArgsException(string msg)
            : base(msg)
        {

        }
    } 

    class CmdArgs
    {
        public enum ActionType
        {
            Compress,
            Decompress
        }

        public ActionType Action { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
    }
}
