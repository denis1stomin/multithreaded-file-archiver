using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GzipArchiver;
using Moq;
using TC = GzipArchiver.Test.TestCommon;

namespace GzipArchiver.Test
{
    [TestClass]
    public class CompressionPipelineTest
    {
        [TestMethod]
        public void GetInputDataWhileItExists()
        {
            var callCount = 0;
            var dataPortion = new MemoryStream(TC.GetRandomBytes(123));

            var readerMock = new Mock<ISourceReader>();
            readerMock.Setup(h => h.ReadNextPortion())
                .Callback(() => {
                    callCount ++;
                    if (callCount > 10)
                        dataPortion = null;
                    else
                        dataPortion = new MemoryStream(TC.GetRandomBytes(123));
                })
                .Returns(() => dataPortion);

            var writerMock = new Mock<IResultWriter>();
            var loggerMock = new Mock<ILogger>();
            
            using (var pipeline = new CompressionPipeline(
                readerMock.Object, new CompressionWorker(), writerMock.Object, loggerMock.Object))
            {
                pipeline.DoWork();
            }

            Assert.AreEqual(11, callCount);
        }

        [TestMethod]
        public void ReadsAndWritesTheSameNumberOfTimes()
        {
            var readsCount = 0;
            var dataPortion = new MemoryStream(TC.GetRandomBytes(123));

            var readerMock = new Mock<ISourceReader>();
            readerMock.Setup(h => h.ReadNextPortion())
                .Callback(() => {
                    readsCount ++;
                    if (readsCount > 15)
                        dataPortion = null;
                    else
                        dataPortion = new MemoryStream(TC.GetRandomBytes(123));
                })
                .Returns(() => dataPortion);

            var writesCount = 0;
            var writerMock = new Mock<IResultWriter>();
            writerMock.Setup(h => h.WritePortion(It.IsAny<Stream>()))
                .Callback(() => writesCount ++);

            var loggerMock = new Mock<ILogger>();
            
            using (var pipeline = new CompressionPipeline(
                readerMock.Object, new CompressionWorker(), writerMock.Object, loggerMock.Object))
            {
                pipeline.DoWork();
            }

            // This minus-one is because the last read null portion is not handled internally.
            Assert.AreEqual(readsCount - 1, writesCount);
        }
    }
}
