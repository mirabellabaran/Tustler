using Microsoft.VisualStudio.TestTools.UnitTesting;
using TustlerServicesLib;

namespace TustlerServicesLibTest
{
    [TestClass]
    public class SentenceChunkerTester
    {
        [TestMethod]
        public void TestReturns1Chunk()
        {
            var filePath = @"C:\Users\Zev\Downloads\testtext.txt";
            var chunker = new TustlerServicesLib.SentenceChunker(filePath);

            Assert.IsTrue(chunker.NumChunks == 1);
        }
    }
}
