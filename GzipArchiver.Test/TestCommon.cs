using System;
using System.IO;
using System.Linq;

namespace GzipArchiver.Test
{
    public static class TestCommon
    {
        public static void CreateNewTestArchive(byte[] data = null)
        {
            using (var file = File.Create(TestArchivePath))
            {
                if (data != null)
                    file.Write(data, 0, data.Length);
            }
        }

        public static void DeleteTestArchive()
        {
            if (File.Exists(TestArchivePath))
                File.Delete(TestArchivePath);

            for (int i = 0; i < 10; ++i)
            {
                var path = $"{TestArchivePath}_{i}";
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public static byte[] GetRandomBytes(int size)
        {
            var result = new byte[size];
            var rand = new Random();

            rand.NextBytes(result);

            return result;
        }

        public static void RandomizeLinesOrder(string path)
        {
            var lines = File.ReadAllLines(path);
            var rnd = new Random();
            lines = lines.OrderBy(line => rnd.Next()).ToArray();
            File.WriteAllLines(path, lines);
        }

        public const string TestArchivePath = "output.gz";
        public const string TestPortion1 = "some text #0";
        public const string TestPortion2 = "some text #1";
        public const string TestPortion3 = "some text #2";
        public const int AveragePortionSize = 2000 * 3000;
    }
}
