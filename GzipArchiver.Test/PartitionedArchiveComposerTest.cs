using System;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GzipArchiver.Test
{
    [TestClass]
    public class PartitionedArchiveComposerTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BadConstructorArguments()
        {
            new PartitionedArchiveComposer(null, IArchiveComposer.OpenMode.Read);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BadConstructorArguments2()
        {
            new PartitionedArchiveComposer("  ", IArchiveComposer.OpenMode.Read);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SingleModePerInstance()
        {
            CreateTestArchive();

            var composer = new PartitionedArchiveComposer(TestArchivePath, IArchiveComposer.OpenMode.Read);
            composer.WritePortion(new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SingleModePerInstance2()
        {
            DeleteTestArchive();

            var composer = new PartitionedArchiveComposer(TestArchivePath, IArchiveComposer.OpenMode.Write);
            composer.ReadNextPortion();
        }

        [TestMethod]
        public void WriteFewPortions()
        {
            DeleteTestArchive();

            using (var composer = new PartitionedArchiveComposer(TestArchivePath, IArchiveComposer.OpenMode.Write))
            {
                var data = Encoding.UTF8.GetBytes(TestPortion1);
                composer.WritePortion(new MemoryStream(data));

                data = GetRandomBytes(AveragePortionSize);
                composer.WritePortion(new MemoryStream(data));

                data = Encoding.UTF8.GetBytes(TestPortion3);
                composer.WritePortion(new MemoryStream(data));
            }

            using (var reader = new StreamReader($"{TestArchivePath}_0"))
            {
                var content = reader.ReadToEnd();
                Assert.AreEqual(TestPortion1, content);
            }

            var portion2Size = new FileInfo($"{TestArchivePath}_1").Length;
            Assert.AreEqual(portion2Size, AveragePortionSize);

            using (var reader = new StreamReader($"{TestArchivePath}_2"))
            {
                var content = reader.ReadToEnd();
                Assert.AreEqual(TestPortion3, content);
            }
        }

        [TestMethod]
        public void ReadFewPortions()
        {
            // Using data after previous test, don't want to create more test data

            using (var composer = new PartitionedArchiveComposer(TestArchivePath, IArchiveComposer.OpenMode.Read))
            {
                var portion1 = composer.ReadNextPortion();
                Assert.AreEqual(TestPortion1, Encoding.UTF8.GetString(portion1.ToArray()));

                var portion2 = composer.ReadNextPortion();
                Assert.AreEqual(AveragePortionSize, portion2.ToArray().Length);

                var portion3 = composer.ReadNextPortion();
                Assert.AreEqual(TestPortion3, Encoding.UTF8.GetString(portion3.ToArray()));

                var portion4 = composer.ReadNextPortion();
                Assert.AreEqual(null, portion4);

                var portion5 = composer.ReadNextPortion();
                Assert.AreEqual(null, portion5);
            }
        }

        [TestMethod]
        public void ReadFewDisorderedPortions()
        {
            RandomizeLinesOrder(TestArchivePath);            
            ReadFewPortions();
        }

        public static void CreateTestArchive()
        {
            if (!File.Exists(TestArchivePath))
                using (File.Create(TestArchivePath));
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
