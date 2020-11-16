using CloudWeaver.Foundation.Types;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TustlerFFMPEG;

namespace CloudWeaver.MediaServices.Test
{
    public class RuntimeOptions : IRuntimeOptions
    {
        public RuntimeOptions()
        {
            IsMocked = false;
        }

        public bool IsMocked
        {
            get;
            set;
        }
    }

    [TestClass]
    public class TaskFunctionTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache";

        [TestMethod]
        public async Task GetCodecInfoAsync()
        {
            var taskName = "GetCodecInfo";   // used as both the name of the task function and the task identifier

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.MediaServices", "CloudWeaver.MediaServices.Tasks", taskName, false, true));

            var codecName = "aac";

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AVRequestIntraModule(RequestCodecName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AVShareIntraModule(AVArgument.NewSetCodecName(codecName))));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AVRequestIntraModule(RequestAVInterface)",
                    "SetArgument: AVShareIntraModule(SetCodecInfo: TustlerFFMPEG.Types.CodecInfo.CodecPair)",
                    "TaskComplete: Task complete"
                }));

            var codecPair = agent.LastCallResponseList()
                .Where(response => response.IsSetArgument)
                .Select(response => {
                    var iface = (response as TaskResponse.SetArgument).Item;
                    var mod = iface as AVShareIntraModule;
                    return (mod.Argument as AVArgument.SetCodecInfo).Item;
                })
                .First();

            Assert.IsTrue(codecPair.Decoder.Name == "aac");
        }

        [TestMethod]
        public async Task GetCodecInfoNoSuchCodecAsync()
        {
            var taskName = "GetCodecInfo";   // used as both the name of the task function and the task identifier

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.MediaServices", "CloudWeaver.MediaServices.Tasks", taskName, false, true));

            var codecName = "unknown";

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AVRequestIntraModule(RequestCodecName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AVShareIntraModule(AVArgument.NewSetCodecName(codecName))));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AVRequestIntraModule(RequestAVInterface)",
                    "Notification: Context=GetCodecInfo; Message=Codec not found",
                    "TaskComplete: Task complete"
                }));
        }

        [TestMethod]
        public async Task GetMediaInfoAsync()
        {
            var taskName = "GetMediaInfo";

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.MediaServices", "CloudWeaver.MediaServices.Tasks", taskName, false, true));

            var fileMediaReference = new FilePickerPath(@"C:\Users\Zev\Videos\The Shawshank Redemption (1994)\The.Shawshank.Redemption.1994.CD1.AC3.iNTERNAL.DVDRip.XviD-xCZ.avi", "", FilePickerMode.Open);

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AVRequestIntraModule(RequestOpenMediaFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetFilePath(fileMediaReference))));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AVRequestIntraModule(RequestAVInterface)",
                    "SetArgument: AVShareIntraModule(SetMediaInfo: TustlerFFMPEG.Types.MediaInfo.MediaInfo)",
                    "TaskComplete: Task complete"
                }));

            var mediaInfo = agent.LastCallResponseList()
                .Where(response => response.IsSetArgument)
                .Select(response => {
                    var iface = (response as TaskResponse.SetArgument).Item;
                    var mod = iface as AVShareIntraModule;
                    return (mod.Argument as AVArgument.SetMediaInfo).Item;
                })
                .First();

            Assert.IsTrue(mediaInfo.Duration == 4095.8);
        }

        [TestMethod]
        public async Task GetMediaInfoNoSuchPathAsync()
        {
            var taskName = "GetMediaInfo";

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.MediaServices", "CloudWeaver.MediaServices.Tasks", taskName, false, true));

            var fileMediaReference = new FileMediaReference(@"C:\temp\temp.avi", "", "");

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestFileMediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetFileMediaReference(fileMediaReference))));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AVRequestIntraModule(RequestAVInterface)",
                    "Notification: Context=GetMediaInfo; Message=Path argument must exist",
                    "TaskComplete: Task complete"
                }));
        }

        private static bool CheckAllStartWith(string[] resultItems, string[] testItems)
        {
            return resultItems.Zip(testItems, (resultItem, testItem) => resultItem.StartsWith(testItem)).All(test => test);
        }

        private static async Task<Agent> InitializeTestAsync(string taskName, string workingDirectory, SaveFlags saveFlags)
        {
            var avInterface = new FFMPEGServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new AVKnownArguments(avInterface));

            var taskLogger = new TaskLogger();

            var taskFunctionResolver = await TaskFunctionResolver.Create();
            var agent = new Agent(knownArguments, taskFunctionResolver, taskLogger, retainResponses: true);

            agent.SetTaskIdentifier(taskName);
            agent.SetWorkingDirectory(new DirectoryInfo(workingDirectory));
            if (saveFlags is object)
                agent.SetSaveFlags(saveFlags);

            return agent;
        }

        private static async Task<string[]> CallTaskAsync(Agent agent)
        {
            // The visibility of internally resolved requests is controlled here
            // Note that the task function is re-called immediately after request resolution
            agent.ClearResponses();

            // process the response stream and modify internal agent state
            if (agent.TaskAvailable)
                await agent.RunNext();
            else
                await agent.RunCurrent();

            // stringify the responses to the last function call
            return agent.LastCallResponseList().Select(response => response.ToString()).ToArray();
        }
    }
}
