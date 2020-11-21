using CloudWeaver.Foundation.Types;
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
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.UploadMediaFile;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var mediaFilePath = Path.Combine(WorkingDirectory, "SallyRide2.wav");
            var mediaFileReference = new FileMediaReference(mediaFilePath, "audio/mpeg", "wav");

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestFileMediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestFileMediaReference),
                new StandardShareIntraModule(StandardArgument.NewSetFileMediaReference(mediaFileReference))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestBucket)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestBucket),
                new AWSShareIntraModule(AWSArgument.NewSetBucket(new TustlerModels.Bucket() { Name = "tator" }))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                    "Notification: Message=Upload succeeded; Detail=Task: Upload item 'SallyRide2.wav' to S3 completed @",
                    "SetArgument: AWSShareIntraModule(SetS3MediaReference: CloudWeaver.AWS.S3MediaReference)",
                    "TaskComplete: Uploaded media file"
                }));
        }

        [TestMethod]
        public async Task TestStartTranscription()
        {
            var taskName = "StartTranscription";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.StartTranscription;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var vocabularyName = "Bob";
            var languageCodeDomain = new LanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US");
            var s3MediaReference = new S3MediaReference("tator", "item1", "audio/mpeg", "wav");

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionVocabularyName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName(vocabularyName))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionLanguageCode)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode),
                new AWSShareIntraModule(AWSArgument.NewSetLanguage(languageCodeDomain))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestS3MediaReference)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestS3MediaReference),
                new AWSShareIntraModule(AWSArgument.NewSetS3MediaReference(s3MediaReference))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                    "SetArgument: AWSShareIntraModule(SetTranscriptionJobName:",
                    "ShowValue: AWSShowIntraModule(DisplayTranscriptionJobsModel: TustlerModels.TranscriptionJobsViewModel)",
                    "TaskComplete: Transcription started"
                }));
        }

        [TestMethod]
        public async Task TestMonitorTranscription()
        {
            var taskName = "MonitorTranscription";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.MonitorTranscription;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var transcriptionJobName = "myJob1";    // first of three mocked jobs

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionJobName)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionJobName),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobName(transcriptionJobName))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                    "SetArgument: AWSShareIntraModule(SetTranscriptURI: https://s3.ap-southeast-2.amazonaws.com/tator/",
                    "ShowValue: AWSShowIntraModule(DisplayTranscriptionJob: TustlerModels.TranscriptionJob)",
                    "TaskComplete: Transcription Job Completed"
                }));
        }

        [TestMethod]
        public async Task TestDownloadTranscript()
        {
            var taskName = "DownloadTranscript";
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    //AWSFlagItem.TranscribeSaveJSONTranscript,
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var transcriptURI = "https://s3.ap-southeast-2.amazonaws.com/test/item1";   // assumed to exist

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, saveFlags);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptURI)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptURI),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptURI(transcriptURI))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 5);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                    "Notification: Message=Download succeeded; Detail=Task: Download",
                    "SetArgument: AWSShareIntraModule(SetTranscriptJSON: 234 bytes)",
                    "TaskComplete: Downloaded transcript file"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranscript()
        {
            const string transcriptJSONTestFilename = "SallyRide d2a8856b.json";

            var taskName = "SaveTranscript";

            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranscribeSaveJSONTranscript
                })
            });

            var agent = await InitializeTestAsync(taskName, WorkingDirectory, saveFlags);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, transcriptJSONTestFilename);
            var jsonData = File.ReadAllBytes(transcriptJSONTestFilePath);

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)",
                "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)",
                "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)",
                "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)"
            }));

            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved JSON transcript to"
                }));
        }

        [TestMethod]
        public async Task TestExtractTranscribedDefault()
        {
            const string transcriptJSONTestFilename = "SallyRide d2a8856b.json";

            var taskName = "ExtractTranscribedDefault";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.ExtractTranscribedDefault;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, transcriptJSONTestFilename);
            var jsonData = File.ReadAllBytes(transcriptJSONTestFilePath);

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "SetArgument: AWSShareIntraModule(SetTranscriptionDefaultTranscript: You know, Sally Ride is such",
                    "TaskComplete: Extracted transcript data"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranscribedDefault()
        {
            var taskName = "SaveTranscribedDefault";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.SaveTranscribedDefault;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranscribeSaveDefaultTranscript
                })
            });
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, saveFlags);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var transcript = "This is a test transcript";

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)",
                "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)",
                "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)",
                "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)"
            }));

            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskComplete: Saved transcribed text to"
                }));
        }

        [TestMethod]
        public async Task TestTranslateText()
        {
            var taskName = "TranslateText";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.TranslateText;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var languageCodeDomain = new LanguageCodeDomain(LanguageDomain.Translation, "English", "en");
            var languages = new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" }
                }.Select(languageCode => new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(languageCode)));
            var languageTargets = new AWSIterationStack(Guid.NewGuid(), languages);
            var terminologyNames = new List<string>()
            {
                "Bob"
            };
            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 3);
            CollectionAssert.AreEqual(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)",
                "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)",
                "RequestArgument: AWSRequestIntraModule(RequestTranslationTerminologyNames)"
            });

            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(terminologyNames))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationLanguageCodeSource)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource),
                new AWSShareIntraModule(AWSArgument.NewSetLanguage(languageCodeDomain))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 8);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                    "TaskInfo: Running French translation...",
                    "TaskInfo: Segment 0 completed",
                    "TaskInfo: Segment 1 completed",
                    "TaskInfo: Segment 2 completed",
                    "SetArgument: AWSShareIntraModule(SetTranslationSegments: TustlerServicesLib.SentenceChunker)",
                    "TaskComplete: Translation from English to French is complete"
                }));
        }

        [TestMethod]
        public async Task TestSaveTranslation()
        {
            var taskName = "SaveTranslation";
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.SaveTranslation;
            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranslateSaveTranslation
                })
            });
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, saveFlags);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var languages = new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" }
                }.Select(languageCode => new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(languageCode)));
            var languageTargets = new AWSIterationStack(Guid.NewGuid(), languages);

            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";
            var translation = "Ceci est une transcription de test.";
            var chunker = new SentenceChunker(transcript, 40);    // chunkSize must be larger than the sentence size
            foreach (var kvp in chunker.Chunks)
            {
                var index = kvp.Key;
                chunker.Update(index, translation);
            }

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            CollectionAssert.AreEqual(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)",
                "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)",
                "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)",
                "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)"
            });

            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationSegments)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationSegments),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationSegments(chunker))
            ));

            result = await CallTaskAsync(agent);
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

            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new AWSFlagSet(new AWSFlagItem[] {
                    AWSFlagItem.TranslateSaveTranslation
                })
            });
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, saveFlags);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var languageCodeDomain = new LanguageCodeDomain(LanguageDomain.Translation, "English", "en");
            var languages = new TustlerModels.LanguageCode[] {
                    new TustlerModels.LanguageCode() { Name = "French", Code = "fr" },
                    new TustlerModels.LanguageCode() { Name = "Danish", Code = "da" },
                    new TustlerModels.LanguageCode() { Name = "German", Code = "de" }
                }.Select(languageCode => new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(languageCode)));
            var languageTargets = new AWSIterationStack(Guid.NewGuid(), languages);

            var terminologyNames = new List<string>()
            {
                "Bob"
            };
            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 4);
            CollectionAssert.AreEqual(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestSaveFlags)",
                "RequestArgument: StandardRequestIntraModule(RequestWorkingDirectory)",
                "RequestArgument: StandardRequestIntraModule(RequestTaskIdentifier)",
                "RequestArgument: AWSRequestIntraModule(RequestTranslationLanguageCodeSource)"
            });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource),
                new AWSShareIntraModule(AWSArgument.NewSetLanguage(languageCodeDomain))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTargetLanguages)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(languageTargets))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranslationTerminologyNames)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames),
                new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(terminologyNames))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptionDefaultTranscript)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript(transcript))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 28);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                "RequestArgument: AWSRequestIntraModule(RequestAWSInterface)",
                "BeginLoopSequence (3 items): TranslateText, SaveTranslation",
                "TaskComplete: Translating text into multiple languages...",
                "TaskInfo: Running French translation...",
                "TaskInfo: Segment 0 completed",
                "TaskInfo: Segment 1 completed",
                "TaskInfo: Segment 2 completed",
                "SetArgument: AWSShareIntraModule(SetTranslationSegments: TustlerServicesLib.SentenceChunker)",
                "TaskComplete: Translation from English to French is complete",
                "TaskInfo: Working directory is:",
                "TaskComplete: Saved translation to Translation-MultiLanguageTranslateText-fr.txt",
                "TaskInfo: Running Danish translation...",
                "TaskInfo: Segment 0 completed",
                "TaskInfo: Segment 1 completed",
                "TaskInfo: Segment 2 completed",
                "SetArgument: AWSShareIntraModule(SetTranslationSegments: TustlerServicesLib.SentenceChunker)",
                "TaskComplete: Translation from English to Danish is complete",
                "TaskInfo: Working directory is:",
                "TaskComplete: Saved translation to Translation-MultiLanguageTranslateText-da.txt",
                "TaskInfo: Running German translation...",
                "TaskInfo: Segment 0 completed",
                "TaskInfo: Segment 1 completed",
                "TaskInfo: Segment 2 completed",
                "SetArgument: AWSShareIntraModule(SetTranslationSegments: TustlerServicesLib.SentenceChunker)",
                "TaskComplete: Translation from English to German is complete",
                "TaskInfo: Working directory is:",
                "TaskComplete: Saved translation to Translation-MultiLanguageTranslateText-de.txt"
                }));
        }

        [TestMethod]
        public async Task TestCreateSubTitles()
        {
            const string TestDataFolderName = "TestData";
            const string transcriptJSONTestFilename = "SallyRide d2a8856b.json";
            const string subTitleFilename = "test_subtitles.txt";

            var taskName = "CreateSubTitles";
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            var transcriptJSONTestFilePath = Path.Combine(WorkingDirectory, TestDataFolderName, transcriptJSONTestFilename);
            var jsonData = File.ReadAllBytes(transcriptJSONTestFilePath);

            var subTitleFileInfo = new FileInfo(Path.Combine(WorkingDirectory, TestDataFolderName, subTitleFilename));

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestTranscriptJSON)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptJSON),
                new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(jsonData))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: AWSRequestIntraModule(RequestSubtitleFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new AWSRequestIntraModule(AWSRequest.RequestSubtitleFilePath),
                new AWSShareIntraModule(AWSArgument.NewSetSubtitleFilePath(subTitleFileInfo))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
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
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.ConvertJsonLogToLogFormat;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            void Agent_ConvertToBinary(object sender, System.Text.Json.JsonDocument document)
            {
                var taskEvents = Serialization.DeserializeEventsFromJSON(document);
                var blocks = Serialization.SerializeEventsAsBytes(taskEvents, 0);
                var data = EventLoggingUtilities.BlockArrayToByteArray(blocks);
                agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestLogFormatEvents),
                    new StandardShareIntraModule(StandardArgument.NewSetLogFormatEvents(data))
                ));
            }

            agent.ConvertToBinary += Agent_ConvertToBinary;

            var jsonFilePath = new FilePickerPath(
                Path.Combine(WorkingDirectory, TestDataFolderName, jsonFileFilename),
                "json", FilePickerMode.Open);
            var logFilePath = new FilePickerPath(
                Path.Combine(WorkingDirectory, TestDataFolderName, logFileFilename),
                "bin", FilePickerMode.Save);

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestOpenJsonFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestOpenJsonFilePath),
                new StandardShareIntraModule(StandardArgument.NewSetFilePath(jsonFilePath))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveLogFormatFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestSaveLogFormatFilePath),
                new StandardShareIntraModule(StandardArgument.NewSetFilePath(logFilePath))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 3);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "RequestArgument: StandardRequestIntraModule(RequestNotifications)",
                    "TaskConvertToBinary {document}",
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
            // Func<TaskFunctionQueryMode, InfiniteList<MaybeResponse>, IEnumerable<TaskResponse>> taskName = Tasks.ConvertLogFormatToJsonLog;
            var agent = await InitializeTestAsync(taskName, WorkingDirectory, null);
            agent.PushTask(new TaskFunctionSpecifier("CloudWeaver.AWS", "CloudWeaver.AWS.Tasks", taskName, false, true));

            void Agent_ConvertToJson(object sender, byte[] data)
            {
                var blocks = EventLoggingUtilities.ByteArrayToBlockArray(data);
                var taskEvents = Serialization.DeserializeEventsFromBytes(blocks);
                var serializedData = Serialization.SerializeEventsAsJSON(taskEvents);
                agent.AddArgument(TaskResponse.NewSetArgument(
                    new StandardRequestIntraModule(StandardRequest.RequestJsonEvents),
                    new StandardShareIntraModule(StandardArgument.NewSetJsonEvents(serializedData))
                ));
            }

            agent.ConvertToJson += Agent_ConvertToJson;

            var logFilePath = new FilePickerPath(
                Path.Combine(WorkingDirectory, TestDataFolderName, logFileFilename),
                "bin", FilePickerMode.Open);

            var jsonFilePath = new FilePickerPath(
                Path.Combine(WorkingDirectory, TestDataFolderName, jsonFileFilename),
                "json", FilePickerMode.Save);

            var result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestOpenLogFormatFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestOpenLogFormatFilePath),
                new StandardShareIntraModule(StandardArgument.NewSetFilePath(logFilePath))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 1);
            CollectionAssert.AreEqual(result, new string[] { "RequestArgument: StandardRequestIntraModule(RequestSaveJsonFilePath)" });
            agent.AddArgument(TaskResponse.NewSetArgument(
                new StandardRequestIntraModule(StandardRequest.RequestSaveJsonFilePath),
                new StandardShareIntraModule(StandardArgument.NewSetFilePath(jsonFilePath))
            ));

            result = await CallTaskAsync(agent);
            Assert.IsTrue(result.Length == 2);
            Assert.IsTrue(CheckAllStartWith(result, new string[] {
                    "TaskConvertToJson: (7219 bytes)",
                    "TaskComplete: Saved event data as JSON"
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
