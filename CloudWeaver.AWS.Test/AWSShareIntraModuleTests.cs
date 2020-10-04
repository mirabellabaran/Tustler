using CloudWeaver.Types;
using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerModels;
using TustlerServicesLib;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class StandardShareIntraModuleTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\TestData";

        [TestMethod]
        public void TestDescription()
        {
            IShareIntraModule notificationsListModule = new StandardShareIntraModule(StandardArgument.NewSetNotificationsList(new NotificationsList()));
            Assert.IsTrue(notificationsListModule.Description().StartsWith("NotificationsList: 0 notifications"));

            var identifier = new FSharpOption<string>("my identifier");
            IShareIntraModule taskIdentifierModule = new StandardShareIntraModule(StandardArgument.NewSetTaskIdentifier(identifier));
            Assert.IsTrue(taskIdentifierModule.Description().StartsWith("TaskIdentifier: my identifier"));

            var task = new FSharpOption<TaskItem>(new TaskItem("module name", "task name", "description"));
            IShareIntraModule taskItemModule = new StandardShareIntraModule(StandardArgument.NewSetTaskItem(task));
            Assert.IsTrue(taskItemModule.Description().StartsWith("TaskItem: module name task name description"));

            var dir = new FSharpOption<DirectoryInfo>(new DirectoryInfo("some path"));
            IShareIntraModule workingDirectoryModule = new StandardShareIntraModule(StandardArgument.NewSetWorkingDirectory(dir));
            Assert.IsTrue(workingDirectoryModule.Description().StartsWith($"WorkingDirectory: {dir.Value.FullName}"));

            var saveFlags = new SaveFlags(new ISaveFlagSet[]
            {
                new StandardFlagSet(new StandardFlagItem[]
                {
                    StandardFlagItem.SaveTaskName
                }),
                new AWSFlagSet(new AWSFlagItem[]
                {
                    AWSFlagItem.TranscribeSaveJSONTranscript,
                    AWSFlagItem.TranscribeSaveDefaultTranscript,
                    AWSFlagItem.TranslateSaveTranslation
                })
            });
            var saveFlagsOption = new FSharpOption<SaveFlags>(saveFlags);
            IShareIntraModule saveFlagsModule = new StandardShareIntraModule(StandardArgument.NewSetSaveFlags(saveFlagsOption));
            Assert.IsTrue(saveFlagsModule.Description().StartsWith("SaveFlags: StandardFlag.SaveTaskName, AWSFlag.TranscribeSaveDefaultTranscript, AWSFlag.TranscribeSaveJSONTranscript, AWSFlag.TranslateSaveTranslation"));

            var jsonFilePath = Path.Combine(WorkingDirectory, "test.json");
            var jsonData = File.ReadAllBytes(jsonFilePath);
            IShareIntraModule jsonEventsModule = new StandardShareIntraModule(StandardArgument.NewSetJsonEvents(jsonData));
            Assert.IsTrue(jsonEventsModule.Description().StartsWith("JsonEvents: 8961 bytes"));

            var binaryFilePath = Path.Combine(WorkingDirectory, "test.bin");
            var binaryData = File.ReadAllBytes(binaryFilePath);
            IShareIntraModule logFormatEventsModule = new StandardShareIntraModule(StandardArgument.NewSetLogFormatEvents(binaryData));
            Assert.IsTrue(logFormatEventsModule.Description().StartsWith("LogFormatEvents: 6390 bytes"));

            IShareIntraModule openJsonFilePathModule = new StandardShareIntraModule(StandardArgument.NewSetOpenJsonFilePath(new FileInfo("my open JSON path")));
            Assert.IsTrue(openJsonFilePathModule.Description().StartsWith($"OpenJsonFilePath: {new FileInfo("my open JSON path").FullName}"));

            IShareIntraModule saveJsonFilePathModule = new StandardShareIntraModule(StandardArgument.NewSetSaveJsonFilePath(new FileInfo("my save JSON path")));
            Assert.IsTrue(saveJsonFilePathModule.Description().StartsWith($"SaveJsonFilePath: {new FileInfo("my save JSON path").FullName}"));

            IShareIntraModule openLogFormatFilePathModule = new StandardShareIntraModule(StandardArgument.NewSetOpenLogFormatFilePath(new FileInfo("my open log format path")));
            Assert.IsTrue(openLogFormatFilePathModule.Description().StartsWith($"OpenLogFormatFilePath: {new FileInfo("my open log format path").FullName}"));

            IShareIntraModule saveLogFormatFilePathModule = new StandardShareIntraModule(StandardArgument.NewSetSaveLogFormatFilePath(new FileInfo("my save log format path")));
            Assert.IsTrue(saveLogFormatFilePathModule.Description().StartsWith($"SaveLogFormatFilePath: {new FileInfo("my save log format path").FullName}"));
        }
    }

    [TestClass]
    public class AWSShareIntraModuleTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\TestData";

        [TestMethod]
        public async Task TestDescription()
        {
            var awsInterface = new AmazonWebServiceInterface( new TustlerInterfaces.RuntimeOptions() { IsMocked = true } );

            IShareIntraModule awsInterfaceModule = new AWSShareIntraModule(AWSArgument.NewSetAWSInterface(awsInterface));
            Assert.AreEqual("AWSInterface: mocking mode enabled", awsInterfaceModule.Description());

            IShareIntraModule bucketModule = new AWSShareIntraModule(AWSArgument.NewSetBucket(new Bucket() { Name = "test", CreationDate = new DateTime(637370970747723577) }));
            Assert.IsTrue(bucketModule.Description().StartsWith("Bucket: test"));

            var bucketsModel = new BucketViewModel();
            await bucketsModel.Refresh(awsInterface, true, new NotificationsList());
            IShareIntraModule bucketsModelModule = new AWSShareIntraModule(AWSArgument.NewSetBucketsModel(bucketsModel));
            Assert.IsTrue(bucketsModelModule.Description().StartsWith("BucketsModel: tator, test"));

            IShareIntraModule s3MediaReferenceModule = new AWSShareIntraModule(AWSArgument.NewSetS3MediaReference(
                new S3MediaReference("test", "key", "audio/x-wav", "wav")));
            Assert.IsTrue(s3MediaReferenceModule.Description().StartsWith("S3MediaReference: key key from test (audio/x-wav)"));

            IShareIntraModule transcriptionJobNameModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobName("test_job"));
            Assert.IsTrue(transcriptionJobNameModule.Description().StartsWith("TranscriptionJobName: test_job"));

            var path = Path.Combine(WorkingDirectory, "SallyRide d2a8856b.json");
            var data = new ReadOnlyMemory<byte>(File.ReadAllBytes(path));
            IShareIntraModule transcriptionJsonModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(data));
            Assert.IsTrue(transcriptionJsonModule.Description().StartsWith("TranscriptJSON: 59850 bytes"));

            IShareIntraModule transcriptionDefaultTranscriptModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript("test transcript"));
            Assert.IsTrue(transcriptionDefaultTranscriptModule.Description().StartsWith("TranscriptionDefaultTranscript: test transcript"));

            IShareIntraModule transcriptURIModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptURI("test URI"));
            Assert.IsTrue(transcriptURIModule.Description().StartsWith("TranscriptURI: test URI"));

            var transcriptionJobsModel = new TranscriptionJobsViewModel();
            await transcriptionJobsModel.ListTasks(awsInterface, new NotificationsList());  // -> three jobs from Mock interface (note that the order varies)
            IShareIntraModule transcriptionJobsModelModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobsModel(transcriptionJobsModel));
            Assert.IsTrue(transcriptionJobsModelModule.Description().StartsWith("TranscriptionJobsModel: myJob1, myJob2, myJob3"));

            IShareIntraModule fileMediaReferenceModule = new AWSShareIntraModule(AWSArgument.NewSetFileMediaReference(
                new FileMediaReference("my path", "audio/x-wav", "wav")));
            Assert.IsTrue(fileMediaReferenceModule.Description().StartsWith("FileMediaReference: my path of type audio/x-wav"));

            IShareIntraModule transcriptionLanguageCodeModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionLanguageCode("en-US"));
            Assert.IsTrue(transcriptionLanguageCodeModule.Description().StartsWith("TranscriptionLanguageCode: en-US"));

            IShareIntraModule transcriptionVocabularyNameModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName("[None]"));
            Assert.IsTrue(transcriptionVocabularyNameModule.Description().StartsWith("TranscriptionVocabularyName: [None]"));

            IShareIntraModule translationLanguageCodeSourceModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource("en"));
            Assert.IsTrue(translationLanguageCodeSourceModule.Description().StartsWith("TranslationLanguageCodeSource: en"));

            var items = new IShareIterationArgument[]
            {
                new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(new LanguageCode() { Name="Arabic", Code="ar" })),
                new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(new LanguageCode() { Name="Azerbaijani", Code="az" }))
            };
            var consumable = new AWSIterationStack(Guid.NewGuid(), items);
            IShareIntraModule translationTargetLanguagesModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(consumable));
            Assert.IsTrue(translationTargetLanguagesModule.Description().StartsWith("TranslationTargetLanguages: Arabic, Azerbaijani"));

            IShareIntraModule translationTerminologyNamesModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(
                new List<string>() { "bob", "jane" }));
            Assert.IsTrue(translationTerminologyNamesModule.Description().StartsWith("TranslationTerminologyNames: bob, jane"));

            var transcript = "This is a test transcript. This is a test transcript. This is a test transcript.";
            var translation = "Ceci est une transcription de test.";
            var chunker = new SentenceChunker(transcript, 40);    // chunkSize must be larger than the sentence size
            foreach (var kvp in chunker.Chunks)
            {
                var index = kvp.Key;
                chunker.Update(index, translation);
            }
            IShareIntraModule translationSegmentsModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationSegments(chunker));
            Assert.IsTrue(translationSegmentsModule.Description().StartsWith("TranslationSegments: completed (3 segments)"));

            IShareIntraModule subtitleFilePathModule = new AWSShareIntraModule(AWSArgument.NewSetSubtitleFilePath(new FileInfo("my path")));
            Assert.IsTrue(subtitleFilePathModule.Description().StartsWith($"SubtitleFilePath: {new FileInfo("my path").FullName}"));
        }
    }
}
