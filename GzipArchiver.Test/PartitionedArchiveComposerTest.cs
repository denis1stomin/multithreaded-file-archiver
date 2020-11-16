using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TC = GzipArchiver.Test.TestCommon;

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
            TC.CreateNewTestArchive();

            var composer = new PartitionedArchiveComposer(TC.TestArchivePath, IArchiveComposer.OpenMode.Read);
            composer.WritePortion(new MemoryStream());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SingleModePerInstance2()
        {
            TC.DeleteTestArchive();

            var composer = new PartitionedArchiveComposer(TC.TestArchivePath, IArchiveComposer.OpenMode.Write);
            composer.ReadNextPortion();
        }

        [TestMethod]
        public void WriteFewPortions()
        {
            TC.DeleteTestArchive();

            using (var composer = new PartitionedArchiveComposer(TC.TestArchivePath, IArchiveComposer.OpenMode.Write))
            {
                var data = Encoding.UTF8.GetBytes(TC.TestPortion1);
                composer.WritePortion(new MemoryStream(data));

                data = TC.GetRandomBytes(TC.AveragePortionSize);
                composer.WritePortion(new MemoryStream(data));

                data = Encoding.UTF8.GetBytes(TC.TestPortion3);
                composer.WritePortion(new MemoryStream(data));
            }

            using (var reader = new StreamReader($"{TC.TestArchivePath}_0"))
            {
                var content = reader.ReadToEnd();
                Assert.AreEqual(TC.TestPortion1, content);
            }

            var portion2Size = new FileInfo($"{TC.TestArchivePath}_1").Length;
            Assert.AreEqual(portion2Size, TC.AveragePortionSize);

            using (var reader = new StreamReader($"{TC.TestArchivePath}_2"))
            {
                var content = reader.ReadToEnd();
                Assert.AreEqual(TC.TestPortion3, content);
            }
        }

        [TestMethod]
        public void ReadFewPortions()
        {
            // Using data after previous test, don't want to create more test data

            using (var composer = new PartitionedArchiveComposer(TC.TestArchivePath, IArchiveComposer.OpenMode.Read))
            {
                var portion1 = composer.ReadNextPortion();
                Assert.AreEqual(TC.TestPortion1, Encoding.UTF8.GetString(portion1.ToArray()));

                var portion2 = composer.ReadNextPortion();
                Assert.AreEqual(TC.AveragePortionSize, portion2.ToArray().Length);

                var portion3 = composer.ReadNextPortion();
                Assert.AreEqual(TC.TestPortion3, Encoding.UTF8.GetString(portion3.ToArray()));

                var portion4 = composer.ReadNextPortion();
                Assert.AreEqual(null, portion4);

                var portion5 = composer.ReadNextPortion();
                Assert.AreEqual(null, portion5);
            }
        }

        // [TestMethod]
        // public void ReadFewDisorderedPortions()
        // {
        //     RandomizeLinesOrder(TestArchivePath);            
        //     ReadFewPortions();
        // }
    }
}
