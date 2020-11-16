using System;
using System.IO;

namespace GzipArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var param = ParseArgs(args);

                Console.WriteLine("Input parameters:");
                Console.WriteLine($"  action type = {param.Action}");
                Console.WriteLine($"  source path = {param.SourcePath}");
                Console.WriteLine($"  dest path   = {param.DestinationPath}");

                using (var archiver = new GzipArchiver(param.SourcePath, param.DestinationPath))
                {
                    if (param.Action == CmdArgs.ActionType.Compress)
                    {
                        Console.WriteLine("Starting to compress data...");
                        archiver.Compress();
                    }
                    else
                    {
                        Console.WriteLine("Starting to decompress data...");
                        archiver.Decompress();
                    }
                }

                Console.WriteLine("Finished");

            }
            catch (CmdArgsException ex)
            {
                Console.WriteLine(ex.Message);
                ShowHelpAndExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Operation failed:");
                Console.WriteLine(ex.Message);

                Environment.Exit(1);
            }
        }

        static CmdArgs ParseArgs(string[] args)
        {
            if (args.Length < 3)
                throw new CmdArgsException("Not enough input parameters.");

            var parsedArgs = new CmdArgs
            {
                SourcePath = args[1],
                DestinationPath = args[2]
            };

            if (!File.Exists(parsedArgs.SourcePath))
                throw new CmdArgsException($"Cannot find source file path '{parsedArgs.SourcePath}'.");

            if (File.Exists(parsedArgs.DestinationPath))
                throw new CmdArgsException($"Looks like the destination file '{parsedArgs.DestinationPath}' already exists. Only commercial version of the product can rewrite existing files :)");

            var strAction = args[0];
            var parsed = Enum.TryParse(strAction, true, out CmdArgs.ActionType action);
            if (parsed)
                parsedArgs.Action = action;
            else
                throw new CmdArgsException($"Not supported action type '{strAction}'.");

            return parsedArgs;
        }

        static void ShowHelpAndExit()
        {
            Console.WriteLine("Simple multithreaded file archiver.");
            Console.WriteLine("  GzipTest <compress/decompress> <source path> <destination path pattern>");
            Console.WriteLine("  Example:");
            Console.WriteLine("  GzipTest compress my-big-file.txt file-archive");

            Environment.Exit(1);
        }
    }
}
