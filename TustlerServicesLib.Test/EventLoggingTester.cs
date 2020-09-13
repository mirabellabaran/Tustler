using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TustlerServicesLib.Test
{
    public class ByteComparer : IComparer
    {
        int IComparer.Compare(object block1, object block2)
        {
            var first = block1 as byte[];
            var second = block2 as byte[];

            if (first.Length == second.Length)
            {
                var areEqual = first.Zip(second).All(pair => pair.First == pair.Second);
                return areEqual ? 0 : -1;
            }

            return -1;
        }
    }

    [TestClass]
    public class EventLoggingTester
    {
        private readonly string[] test_data = new string[]
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."
        };

        [TestMethod]
        public void TestRoundTripBlocksToBytesToBlocks()
        {
            var blocks = test_data.Select(str => Encoding.UTF8.GetBytes(str)).ToArray();

            byte[] data = TustlerServicesLib.EventLoggingUtilities.BlockArrayToByteArray(blocks);
            Assert.IsTrue(data.Length == blocks.Sum(block => block.Length) + (blocks.Length * 4));

            var returnedBlocks = TustlerServicesLib.EventLoggingUtilities.ByteArrayToBlockArray(data);
            CollectionAssert.AreEqual(blocks, returnedBlocks, new ByteComparer());

            var strings = returnedBlocks.Select(data => Encoding.UTF8.GetString(data)).ToArray();
            CollectionAssert.AreEqual(test_data, strings);
        }
    }
}
