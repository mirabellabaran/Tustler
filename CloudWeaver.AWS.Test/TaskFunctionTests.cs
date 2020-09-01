using Amazon.Polly;
using CloudWeaver;
using CloudWeaver.AWS;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task TestUploadMediaFile()
        {
            var taskName = "UploadMediaFile";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.UploadMediaFile;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var mediaFilePath = Path.Combine(WorkingDirectory, "SallyRide2.wav");
            var mediaFileReference = new FileMediaReference(mediaFilePath, "audio/mpeg", "wav");

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestFileMediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetFileMediaReference(mediaFileReference))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestBucket)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetBucket(new TustlerModels.Bucket() { Name="tator" }))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "Notification: Message=Upload succeeded; Detail=Task: Upload item 'SallyRide2.wav' to S3 completed @",
                    "SetArgument: AWSShareIntraModule(SetS3MediaReference: CloudWeaver.AWS.S3MediaReference)",
                    "TaskComplete: Uploaded media file"
                }));
        }

        [TestMethod]
        public async Task TestStartTranscription()
        {
            var taskName = "StartTranscription";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.StartTranscription;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var vocabularyName = "Bob";
            var languageCode = "en-US";
            var s3MediaReference = new S3MediaReference("tator", "item1", "audio/mpeg", "wav");

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionVocabularyName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName(vocabularyName))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionLanguageCode)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionLanguageCode(languageCode))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestS3MediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetS3MediaReference(s3MediaReference))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "SetArgument: AWSShareIntraModule(SetTranscriptionJobName:",
                    "ShowValue: AWSShowIntraModule(DisplayTranscriptionJobsModel: TustlerModels.TranscriptionJobsViewModel)",
                    "TaskComplete: Transcription started"
                }));
        }

        [TestMethod]
        public async Task TestMonitorTranscription()
        {
            var taskName = "MonitorTranscription";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.MonitorTranscription;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var transcriptionJobName = "myJob1";    // first of three mocked jobs

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionJobName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobName(transcriptionJobName))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "SetArgument: AWSShareIntraModule(SetTranscriptURI: https://s3.ap-southeast-2.amazonaws.com/tator/",
                    "ShowValue: AWSShowIntraModule(DisplayTranscriptionJob: TustlerModels.TranscriptionJob)",
                    "TaskComplete: Transcription Job Completed"
                }));
        }

        [TestMethod]
        public async Task TestDownloadTranscriptFile()
        {
            var taskName = "DownloadTranscriptFile";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.DownloadTranscriptFile;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    //AWSFlagItem.TranscribeSaveJSONTranscript,
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
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "Notification: Message=Download succeeded; Detail=Task: Download",
                    "SetArgument: AWSShareIntraModule(SetTranscriptJSON: System.ReadOnlyMemory<Byte>[",
                    "TaskComplete: Downloaded transcript file"
                }));
        }

        [TestMethod]
        public async Task TestExtractTranscript()
        {
            const string transcriptJSONTestFilename = "SallyRide d2a8856b.json";

            var taskName = "ExtractTranscript";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.ExtractTranscript;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, transcriptJSONTestFilename);
            var contents = File.ReadAllBytes(transcriptJSONTestFilePath);
            var jsonData = new ReadOnlyMemory<byte>(contents);

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "SetArgument: AWSShareIntraModule(SetDefaultTranscript: You know, Sally Ride is such an amazing figure",
                    "TaskComplete: Extracted transcript data"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranscript()
        {
            var taskName = "SaveTranscript";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.SaveTranscript;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var transcript = "This is a test transcript";

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskName)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetDefaultTranscript(transcript))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved transcript data to"
                }));
        }

        [TestMethod]
        public async Task TestTranslateText()
        {
            var taskName = "TranslateText";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.TranslateText;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var languageCodeSource = "en";
            var languageTargets = new RetainingStack<TustlerModels.LanguageCode>(
                new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" }
                });
            var terminologyNames = new List<string>()
            {
                "Bob"
            };
            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskName)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTerminologyNames)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(terminologyNames))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationLanguageCodeSource)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource(languageCodeSource))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetDefaultTranscript(transcript))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskInfo: backoff (ms): 0,",
                    "TaskInfo: backoff (ms): 0,",
                    "TaskInfo: backoff (ms): 0,",
                    "TaskComplete: Translation to French is complete"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranslation()
        {
            var taskName = "SaveTranslation";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.SaveTranslation;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranslateSaveTranslation
                })
            });
            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var languageTargets = new RetainingStack<TustlerModels.LanguageCode>(
                new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" }
                });

            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";
            var translation = "Ceci est une transcription de test.";
            var chunker = new SentenceChunker(transcript, 40);    // chunkSize must be larger than the sentence size
            foreach (var kvp in chunker.Chunks)
            {
                var index = kvp.Key;
                chunker.Update(index, translation);
            }

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskName)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationSegments)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationSegments(chunker))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved translation to"
                }));
        }

        [TestMethod]
        public async Task TestMultiLanguageTranslateText()
        {
            var taskName = "MultiLanguageTranslateText";
            Func<InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.MultiLanguageTranslateText;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var languageTargets = new RetainingStack<TustlerModels.LanguageCode>(
                new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" },
                    new TustlerModels.LanguageCode() { Name = "Danish", Code = "da" },
                    new TustlerModels.LanguageCode() { Name = "German", Code = "de" }
                });

            var result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });
        }

        private static Agent InitializeTest(string taskName, string workingDirectory, SaveFlags saveFlags)
        {
            var notificationsList = new NotificationsList();
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var agent = new Agent(knownArguments, retainResponses: true);
            agent.SetTaskName(taskName);
            agent.SetWorkingDirectory(new System.IO.DirectoryInfo(workingDirectory));
            if (saveFlags is object)
                agent.SetSaveFlags(saveFlags);

            return agent;
        }

        private static bool CheckAllStartWith(string[] resultItems, string[] testItems)
        {
            return resultItems.Zip(testItems, (resultItem, testItem) => resultItem.StartsWith(testItem)).All(test => test);
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

            // stringify the responses to the last function call
            return agent.LastCallResponseList().Select(response => response.ToString()).ToArray();
        }
    }
}
