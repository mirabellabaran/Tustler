using CloudWeaver.Agent;
using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class TaskFunctionTests
    {
        [TestMethod]
        public void TestDownloadTranscriptFile()
        {
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.DownloadTranscriptFile;

            var notificationsList = new NotificationsList();
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));
            var agent = new Agent.Agent(knownArguments);

            var result = CallTask(taskFunction, agent);
            CollectionAssert.AreEqual(result, new TaskResponse[] { });
            Console.WriteLine(result);
        }

        private static string[] CallTask(Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction, Agent.Agent agent)
        {
            // generate an arguments stack (by default an infinite enumerable of Nothing arguments)
            var args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing);

            agent.PrepareFunctionArguments(args);
            var responseStream = taskFunction(args);
            agent.RunTask(responseStream);

            return responseStream.Select(response => response.ToString()).ToArray();
        }
    }
}
