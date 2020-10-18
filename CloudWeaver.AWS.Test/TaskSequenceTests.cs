using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class TaskSequenceTests
    {
        [TestMethod]
        public void TestInsert()
        {
            var items = new TaskItem[] {
                new TaskItem("mod1", "task1", string.Empty),
                new TaskItem("mod2", "task1", string.Empty),
                new TaskItem("mod2", "task2", string.Empty),
            };

            var taskSequence = new TaskSequence(Guid.NewGuid(), items, ItemOrdering.Sequential);

            var expected = new string[] { "mod1.task1", "mod2.task1", "mod2.task2" };
            var actual = taskSequence.Select(task => task.FullPath).ToArray();
            CollectionAssert.AreEqual(expected, actual);

            // try insert before with no consumed items
            Assert.ThrowsException<InvalidOperationException>(() => taskSequence.InsertBeforeCurrent(new TaskItem("mod1", "inserted", string.Empty)));

            // consume an item and then insert before
            (taskSequence as IConsumableTaskSequence).ConsumeTask();
            var newSequence = taskSequence.InsertBeforeCurrent(new TaskItem("mod1", "inserted", string.Empty));

            expected = new string[] { "mod1.inserted", "mod1.task1", "mod2.task1", "mod2.task2" };
            actual = newSequence.Select(task => task.FullPath).ToArray();
            CollectionAssert.AreEqual(expected, actual);

            // consume another item and then insert before
            (taskSequence as IConsumableTaskSequence).ConsumeTask();
            newSequence = taskSequence.InsertBeforeCurrent(new TaskItem("mod1", "inserted", string.Empty));

            expected = new string[] { "mod1.task1", "mod1.inserted", "mod2.task1", "mod2.task2" };
            actual = newSequence.Select(task => task.FullPath).ToArray();
            CollectionAssert.AreEqual(expected, actual);

            // consume the last item and then insert before
            (taskSequence as IConsumableTaskSequence).ConsumeTask();
            newSequence = taskSequence.InsertBeforeCurrent(new TaskItem("mod1", "inserted", string.Empty));

            expected = new string[] { "mod1.task1", "mod2.task1", "mod1.inserted", "mod2.task2" };
            actual = newSequence.Select(task => task.FullPath).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}