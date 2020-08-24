using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class TaskFunctionTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache";

        [TestMethod]
        public async Task TestDownloadTranscriptFileAsync()
        {
            var taskName = "DownloadTranscriptFile";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.DownloadTranscriptFile;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranscribeSaveJSONTranscript,
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var transcriptURI = "https://s3.ap-southeast-2.amazonaws.com/test/item1";   // assumed to exist

            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskName)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptURI)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptURI(transcriptURI))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 2);
            CollectionAssert.AreEqual(result, new string[] {
                "SetArgument: AWSShareIntraModule(SetTranscriptJSON: System.ReadOnlyMemory<Byte>[256])",
                "TaskComplete: Downloaded transcript file"
            });
        }

        private static Agent InitializeTest(string taskName, string workingDirectory, SaveFlags saveFlags)
        {
            var notificationsList = new NotificationsList();
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var agent = new Agent(knownArguments);
            agent.SetTaskName(taskName);
            agent.SetWorkingDirectory(new System.IO.DirectoryInfo(workingDirectory));
            agent.SetSaveFlags(saveFlags);

            return agent;
        }

        private static async Task<string[]> CallTaskAsync(Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction, Agent agent)
        {
            // generate an arguments stack (by default an infinite enumerable of Nothing arguments)
            var args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing);

            // add resolved arguments from the current internal agent state (SetArgument events on the event stack)
            agent.PrepareFunctionArguments(args);

            // call the function
            var responseStream = taskFunction(args);

            // process the response stream and modify internal agent state
            await agent.RunTask(responseStream);

            return responseStream.Select(response => response.ToString()).ToArray();
        }
    }
}
