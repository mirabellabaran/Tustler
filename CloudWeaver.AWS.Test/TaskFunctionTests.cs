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
            var taskName = "UploadMediaFile";   // used as both the name of the task function and the task identifier
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.UploadMediaFile;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var mediaFilePath = Path.Combine(WorkingDirectory, "SallyRide2.wav");
            var mediaFileReference = new FileMediaReference(mediaFilePath, "audio/mpeg", "wav");

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestFileMediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetFileMediaReference(mediaFileReference))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestBucket)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetBucket(new TustlerModels.Bucket() { Name="tator" }))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
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
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.StartTranscription;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var vocabularyName = "Bob";
            var languageCode = "en-US";
            var s3MediaReference = new S3MediaReference("tator", "item1", "audio/mpeg", "wav");

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionVocabularyName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName(vocabularyName))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionLanguageCode)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionLanguageCode(languageCode))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestS3MediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetS3MediaReference(s3MediaReference))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
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
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.MonitorTranscription;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var transcriptionJobName = "myJob1";    // first of three mocked jobs

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionJobName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobName(transcriptionJobName))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
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
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.DownloadTranscriptFile;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    //AWSFlagItem.TranscribeSaveJSONTranscript,
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var transcriptURI = "https://s3.ap-southeast-2.amazonaws.com/test/item1";   // assumed to exist

            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptURI)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptURI(transcriptURI))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
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
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.ExtractTranscript;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, transcriptJSONTestFilename);
            var contents = File.ReadAllBytes(transcriptJSONTestFilePath);
            var jsonData = new ReadOnlyMemory<byte>(contents);

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "SetArgument: AWSShareIntraModule(SetTranscriptionDefaultTranscript: You know, Sally Ride is such",
                    "TaskComplete: Extracted transcript data"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranscript()
        {
            var taskName = "SaveTranscript";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.SaveTranscript;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var transcript = "This is a test transcript";

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved transcript data to"
                }));
        }

        [TestMethod]
        public async Task TestTranslateText()
        {
            var taskName = "TranslateText";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.TranslateText;
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

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTerminologyNames)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(terminologyNames))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationLanguageCodeSource)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource(languageCodeSource))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 6);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskInfo: Running French translation...",
                    "TaskInfo: Segment 0 completed",
                    "TaskInfo: Segment 1 completed",
                    "TaskInfo: Segment 2 completed",
                    "SetArgument: AWSShareIntraModule(SetTranslationSegments: TustlerServicesLib.SentenceChunker)",
                    "TaskComplete: Translation to French is complete"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranslation()
        {
            var taskName = "SaveTranslation";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.SaveTranslation;
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

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationSegments)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationSegments(chunker))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskInfo: Working directory is:",
                    "TaskComplete: Saved translation to"
                }));
        }

        [TestMethod]
        public async Task TestMultiLanguageTranslateText()
        {
            var taskName = "MultiLanguageTranslateText";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.MultiLanguageTranslateText;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranslateSaveTranslation
                })
            });
            var agent = InitializeTest(taskName, WorkingDirectory, saveFlags);

            var languageCodeSource = "en";
            var languageTargets = new RetainingStack<TustlerModels.LanguageCode>(
                 new TustlerModels.LanguageCode[] {
                                new TustlerModels.LanguageCode() { Name = "French", Code = "fr" },
                                new TustlerModels.LanguageCode() { Name = "Danish", Code = "da" },
                                new TustlerModels.LanguageCode() { Name = "German", Code = "de" }
                 });
            var terminologyNames = new List<string>()
            {
                "Bob"
            };
            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";

            // Agent callbacks are required for switching between tasks
            // Note that any previous calls to CallTaskAsync(taskName, ) must have run to completion for this to work correctly
            // so here we will just keep track of task function changes
            var taskList = new List<string>();
            agent.CallTask += (object sender, TaskItem task) =>
            {
                taskList.Add(task.TaskName);
            };

            static IEnumerable<TaskResponse> PurgeFunction(TaskFunctionQueryMode queryMode, InfiniteList<MaybeResponse> resolvable_arguments)
            {
                // five more TaskComplete responses (six total)
                return Enumerable.Range(0, 5).Select(i => TaskResponse.NewTaskComplete(i.ToString(), DateTime.Now));
            }

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationLanguageCodeSource)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource(languageCodeSource))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTerminologyNames)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(terminologyNames))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "BeginLoopSequence (3 items): TranslateText, SaveTranslation",
                    "TaskComplete: Translating text into multiple languages..."
                }));

            // call the purge function to exhaust the nested data and task loops
            result = await CallTaskAsync(taskName, PurgeFunction, agent);
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                "TaskComplete: 0",
                "TaskComplete: 1",
                "TaskComplete: 2",
                "TaskComplete: 3",
                "TaskComplete: 4",
            }));

            // expecting a sequence of calls to MultiLanguageTranslateText as arguments are resolved
            // then a sequence of TranslateText, SaveTranslation for each of the three languages
            CollectionAssert.AreEqual(taskList, new string[] {
                "MultiLanguageTranslateText",
                "MultiLanguageTranslateText",
                "MultiLanguageTranslateText",
                "MultiLanguageTranslateText",
                "MultiLanguageTranslateText",
                "TranslateText",
                "SaveTranslation",
                "TranslateText",
                "SaveTranslation",
                "TranslateText",
                "SaveTranslation",
            });
        }

        [TestMethod]
        public async Task TestCreateSubTitles()
        {
            const string TestDataFolderName = "TestData";
            const string transcriptJSONTestFilename = "SallyRide d2a8856b.json";
            const string subTitleFilename = "test_subtitles.txt";

            var taskName = "CreateSubTitles";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.CreateSubTitles;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, TestDataFolderName, transcriptJSONTestFilename);
            var contents = File.ReadAllBytes(transcriptJSONTestFilePath);
            var jsonData = new ReadOnlyMemory<byte>(contents);

            var subTitleFileInfo = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, subTitleFilename));

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestSubtitleFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new AWSShareIntraModule(AWSArgument.NewSetSubtitleFilePath(subTitleFileInfo))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Created subtitle data"
                }));

            var lines = File.ReadAllLines(subTitleFileInfo.FullName);
            Assert.IsTrue(lines.Length == 7);
            Assert.IsTrue(CheckAllStartWith(lines, new string[] {
                    "[1.51] You know , Sally Ride",
                    "[7.25] She's obviously going",
                    "[22.88] But Sally is part",
                    "[29.94] Really showed that",
                    "[36.94] And then ,",
                    "[47.72] Just , in example , exemplary legacy",
                    "[50.06] Ah , that last way beyond her."
                }));
        }

        [TestMethod]
        public async Task TestConvertJsonLogToLogFormat()
        {
            const string TestDataFolderName = "TestData";

            const string jsonFileFilename = "test.json";
            const string logFileFilename = "test-out.bin";

            var taskName = "ConvertJsonLogToLogFormat";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.ConvertJsonLogToLogFormat;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            void Agent_ConvertToBinary(object sender, System.Text.Json.JsonDocument document)
            {
                var taskEvents = Serialization.DeserializeEventsFromJSON(document);
                var blocks = Serialization.SerializeEventsAsBytes(taskEvents, 0);
                var data = EventLoggingUtilities.BlockArrayToByteArray(blocks);
                agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetLogFormatEvents(data))));
            }

            agent.ConvertToBinary += Agent_ConvertToBinary;

            var jsonFilePath = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, jsonFileFilename));
            var logFilePath = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, logFileFilename));

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestOpenJsonFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetOpenJsonFilePath(jsonFilePath))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveLogFormatFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetSaveLogFormatFilePath(logFilePath))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskConvertToBinary {document}"
                }));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved event data in binary log format"
                }));
        }

        [TestMethod]
        public async Task TestConvertLogFormatToJsonLog()
        {
            const string TestDataFolderName = "TestData";

            const string logFileFilename = "test.bin";
            const string jsonFileFilename = "test-out.json";

            var taskName = "ConvertLogFormatToJsonLog";
            Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction = Tasks.ConvertLogFormatToJsonLog;
            var agent = InitializeTest(taskName, WorkingDirectory, null);

            void Agent_ConvertToJson(object sender, byte[] data)
            {
                var blocks = EventLoggingUtilities.ByteArrayToBlockArray(data);
                var taskEvents = Serialization.DeserializeEventsFromBytes(blocks);
                var serializedData = Serialization.SerializeEventsAsJSON(taskEvents);
                agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetJsonEvents(serializedData))));
            }

            agent.ConvertToJson += Agent_ConvertToJson;

            var jsonFilePath = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, jsonFileFilename));
            var logFilePath = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, logFileFilename));

            var result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestOpenLogFormatFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetOpenLogFormatFilePath(logFilePath))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveJsonFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(new StandardShareIntraModule(StandardArgument.NewSetSaveJsonFilePath(jsonFilePath))));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestNotifications)" });

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskConvertToJson: (5146 bytes)"
                }));

            result = await CallTaskAsync(taskName, taskFunction, agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved event data as JSON"
                }));
        }

        private static Agent InitializeTest(string taskId, string workingDirectory, SaveFlags saveFlags)
        {
            var notificationsList = new NotificationsList();
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var agent = new Agent(knownArguments, retainResponses: true);
            agent.SetTaskIdentifier(taskId);
            agent.SetWorkingDirectory(new System.IO.DirectoryInfo(workingDirectory));
            if (saveFlags is object)
                agent.SetSaveFlags(saveFlags);

            return agent;
        }

        private static bool CheckAllStartWith(string[] resultItems, string[] testItems)
        {
            return resultItems.Zip(testItems, (resultItem, testItem) => resultItem.StartsWith(testItem)).All(test => test);
        }

        private static async Task<string[]> CallTaskAsync(string taskName, Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskFunction, Agent agent)
        {
            // generate an arguments stack (by default an infinite enumerable of Nothing arguments)
            var args = new InfiniteList<MaybeResponse>(MaybeResponse.Nothing);

            // add resolved arguments from the current internal agent state (SetArgument events on the event stack)
            agent.PrepareFunctionArguments(args);

            // call the function
            var responseStream = taskFunction(TaskFunctionQueryMode.Invoke, args);

            var currentTask = new TaskItem("CloudWeaver.AWS.Tasks", taskName, string.Empty);

            // process the response stream and modify internal agent state
            await agent.RunTask(currentTask, responseStream);

            // stringify the responses to the last function call
            return agent.LastCallResponseList().Select(response => response.ToString()).ToArray();
        }
    }
}
