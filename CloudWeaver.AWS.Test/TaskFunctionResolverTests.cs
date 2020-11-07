using CloudWeaver.Foundation.Types;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class TaskFunctionResolverTests
    {
        [TestMethod]
        public async Task TestAddFunctionAsync()
        {
            static IEnumerable<TaskResponse> PurgeFunction(TaskFunctionQueryMode queryMode, InfiniteList<MaybeResponse> resolvable_arguments)
            {
                // five more TaskComplete responses (six total)
                return Enumerable.Range(0, 5).Select(i => TaskResponse.NewTaskComplete(i.ToString(), DateTime.Now));
            }

            static MethodInfo Helper(Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> func)
            {
                return func.Method;
            }

            var agent = await InitializeTestAsync("", "", null);

            // add the purge function to the resolver
            var mi = Helper(PurgeFunction);            
            agent.AddFunction(mi);

            var functionModule = mi.DeclaringType;
            var assembly = functionModule.Assembly;
            var taskFunctionSpecifier = new TaskFunctionSpecifier(assembly.FullName, functionModule.FullName, mi.Name, false);

            agent.PushTask(taskFunctionSpecifier);
            await agent.RunNext();

            var result = agent.LastCallResponseList().Select(response => response.ToString()).ToArray();
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                "TaskComplete: 0",
                "TaskComplete: 1",
                "TaskComplete: 2",
                "TaskComplete: 3",
                "TaskComplete: 4"
            }));
        }

        private static bool CheckAllStartWith(string[] resultItems, string[] testItems)
        {
            return resultItems.Zip(testItems, (resultItem, testItem) => resultItem.StartsWith(testItem)).All(test => test);
        }

        private static async Task<Agent> InitializeTestAsync(string taskName, string workingDirectory, SaveFlags saveFlags)
        {
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var taskLogger = new TaskLogger();

            var taskFunctionResolver = await TaskFunctionResolver.Create();
            var agent = new Agent(knownArguments, taskFunctionResolver, taskLogger, retainResponses: true);

            //agent.SetTaskIdentifier(taskName);
            //agent.SetWorkingDirectory(new DirectoryInfo(workingDirectory));
            //if (saveFlags is object)
            //    agent.SetSaveFlags(saveFlags);

            return agent;
        }
    }
}
