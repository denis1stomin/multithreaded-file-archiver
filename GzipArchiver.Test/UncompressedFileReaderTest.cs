using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TC = GzipArchiver.Test.TestCommon;

namespace GzipArchiver.Test
{
    [TestClass]
    public class UncompressedFileReaderTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BadConstructorArguments()
        {
            new UncompressedFileReader("  ", 123);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructorArguments2()
        {
            new UncompressedFileReader("valid path string", -100);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void ReadFromNowhere()
        {
            new UncompressedFileReader("some not existing file", 100);
        }

        [TestMethod]
        public void ReadFromEmptyFile()
        {
            TC.CreateNewTestArchive();

            using (var reader = new UncompressedFileReader(TC.TestArchivePath, 100))
            {
                var portion = reader.ReadNextPortion();
                Assert.IsNull(portion);
            }
        }

        [DataTestMethod]
        [DataRow(5000,5100)]
        [DataRow(5100,5100)]
        public void ReadSinglePortion(int fileSize, int portionSize)
        {
            var bytes = TC.GetRandomBytes(fileSize);
            TC.CreateNewTestArchive(bytes);

            using (var reader = new UncompressedFileReader(TC.TestArchivePath, portionSize))
            {
                var portionBytes = (reader.ReadNextPortion() as MemoryStream).ToArray();
                CollectionAssert.AreEqual(bytes, portionBytes);
                Assert.AreEqual(fileSize, portionBytes.Length);

                Assert.AreEqual(null, reader.ReadNextPortion());
                Assert.AreEqual(null, reader.ReadNextPortion());
                Assert.AreEqual(null, reader.ReadNextPortion());
            }
        }

        [DataTestMethod]
        [DataRow(100, 29)]
        [DataRow(100123,33333)]
        [DataRow(500000, 166121)]
        public void ReadFewPortions(int fileSize, int portionSize)
        {
            var bytes = TC.GetRandomBytes(fileSize);
            TC.CreateNewTestArchive(bytes);

            using (var reader = new UncompressedFileReader(TC.TestArchivePath, portionSize))
            {
                var portionBytes = (reader.ReadNextPortion() as MemoryStream).ToArray();
                CollectionAssert.AreEqual(
                    bytes.Take(portionSize).ToArray(), portionBytes);
                Assert.AreEqual(portionSize, portionBytes.Length);

                portionBytes = (reader.ReadNextPortion() as MemoryStream).ToArray();
                CollectionAssert.AreEqual(
                    bytes.Skip(portionSize).Take(portionSize).ToArray(), portionBytes);
                Assert.AreEqual(portionSize, portionBytes.Length);

                portionBytes = (reader.ReadNextPortion() as MemoryStream).ToArray();
                CollectionAssert.AreEqual(
                    bytes.Skip(portionSize * 2).Take(portionSize).ToArray(), portionBytes);
                Assert.AreEqual(portionSize, portionBytes.Length);

                portionBytes = (reader.ReadNextPortion() as MemoryStream).ToArray();
                CollectionAssert.AreEqual(
                    bytes.Skip(portionSize * 3).ToArray(), portionBytes);
                Assert.AreEqual(fileSize - portionSize * 3, portionBytes.Length);

                Assert.AreEqual(null, reader.ReadNextPortion());
                Assert.AreEqual(null, reader.ReadNextPortion());
                Assert.AreEqual(null, reader.ReadNextPortion());
            }
        }
    }
}
