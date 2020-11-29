using CloudWeaver.Types;
using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerModels;
using TustlerServicesLib;
using System.Text;
using System.Text.Json;
using CloudWeaver.Foundation.Types;
using System.Linq;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class StandardShareIntraModuleTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\TestData";

        private string StringifyBytes(IShareIntraModule module, JsonSerializerOptions options)
        {
            // AsBytes() returns either a UTF8-encoded string or a UTF8-encoded Json document as a byte array
            return UTF8Encoding.UTF8.GetString(module.AsBytes(options));
        }

        [TestMethod]
        public void TestDescription()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new CloudWeaver.Converters.FilePickerPathConverter());

            IShareIntraModule notificationsListModule = new StandardShareIntraModule(StandardArgument.NewSetNotificationsList(new NotificationsList()));
            Assert.IsTrue(notificationsListModule.Description().StartsWith("NotificationsList: 0 notifications"));
            Assert.IsTrue(StringifyBytes(notificationsListModule, options).StartsWith("{\"Notifications\":[]}"));

            var identifier = new FSharpOption<string>("my identifier");
            IShareIntraModule taskIdentifierModule = new StandardShareIntraModule(StandardArgument.NewSetTaskIdentifier(identifier));
            Assert.IsTrue(taskIdentifierModule.Description().StartsWith("TaskIdentifier: my identifier"));
            Assert.IsTrue(StringifyBytes(taskIdentifierModule, options).StartsWith("my identifier"));

            var task = new FSharpOption<TaskItem>(new TaskItem("module name", "task name", "description"));
            IShareIntraModule taskItemModule = new StandardShareIntraModule(StandardArgument.NewSetTaskItem(task));
            Assert.IsTrue(taskItemModule.Description().StartsWith("TaskItem: module name task name description"));
            Assert.IsTrue(StringifyBytes(taskItemModule, options).StartsWith("{\"ModuleName\":\"module name\",\"TaskName\":\"task name\",\"Description\":\"description\",\"FullPath\":\"module name.task name\"}"));

            var dir = new FSharpOption<DirectoryInfo>(new DirectoryInfo("some path"));
            IShareIntraModule workingDirectoryModule = new StandardShareIntraModule(StandardArgument.NewSetWorkingDirectory(dir));
            Assert.IsTrue(workingDirectoryModule.Description().StartsWith($"WorkingDirectory: {dir.Value.FullName}"));
            Assert.IsTrue(StringifyBytes(workingDirectoryModule, options).StartsWith(dir.Value.FullName));

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
            Assert.IsTrue(StringifyBytes(saveFlagsModule, options).StartsWith("[\"StandardFlag.SaveTaskName\",\"AWSFlag.TranscribeSaveDefaultTranscript\",\"AWSFlag.TranscribeSaveJSONTranscript\",\"AWSFlag.TranslateSaveTranslation\"]"));

            var jsonFilePath = Path.Combine(WorkingDirectory, "test.json");
            var jsonData = File.ReadAllBytes(jsonFilePath);
            IShareIntraModule jsonEventsModule = new StandardShareIntraModule(StandardArgument.NewSetJsonEvents(jsonData));
            Assert.IsTrue(jsonEventsModule.Description().StartsWith("JsonEvents: 12918 bytes"));
            Assert.IsTrue(StringifyBytes(jsonEventsModule, options).StartsWith("{\r\n  \"Items\": [\r\n    {\r\n      \"TaskEvent.ForEachTask\": {\r\n        \"Ordering\": \"Sequential\""));

            var binaryFilePath = Path.Combine(WorkingDirectory, "test.bin");
            var binaryData = File.ReadAllBytes(binaryFilePath);
            IShareIntraModule logFormatEventsModule = new StandardShareIntraModule(StandardArgument.NewSetLogFormatEvents(binaryData));
            Assert.IsTrue(logFormatEventsModule.Description().StartsWith("LogFormatEvents: 9439 bytes"));
            Assert.IsTrue(StringifyBytes(logFormatEventsModule, options).StartsWith("\"EQEAAHsiVGFza0V2ZW50LkZvckVhY2hUYXNrIjp7Ik9yZGVyaW5nIjoiU2VxdWVudGlhbCIsIklkZW50aWZpZXIiOiI5ZTEzOTY0Ni01YmM1LTQ"));

            var path = Path.Combine(WorkingDirectory, "SallyRide d2a8856b.json");
            IShareIntraModule openJsonFilePathModule = new StandardShareIntraModule(StandardArgument.NewSetFilePath(new FilePickerPath(path, "json", FilePickerMode.Open)));
            Assert.IsTrue(openJsonFilePathModule.Description().StartsWith($"FilePath: Open (json file) {path}"));

            var filePickerPath = JsonSerializer.Deserialize<FilePickerPath>(new ReadOnlySpan<byte>(openJsonFilePathModule.AsBytes(options)), options);
            Assert.IsTrue((filePickerPath.Mode == FilePickerMode.Open) && (filePickerPath.Extension == "json") && (filePickerPath.Path == path));
        }
    }

    [TestClass]
    public class AWSShareIntraModuleTests
    {
        private const string WorkingDirectory = @"C:\Users\Zev\Projects\C#\Tustler\Tustler\bin\Debug\netcoreapp3.1\FileCache\TestData";

        private string StringifyBytes(IShareIntraModule module, JsonSerializerOptions options)
        {
            // AsBytes() returns either a UTF8-encoded string or a UTF8-encoded Json document as a byte array
            return UTF8Encoding.UTF8.GetString(module.AsBytes(options));
        }

        [TestMethod]
        public async Task TestDescription()
        {
            var awsInterface = new AmazonWebServiceInterface( new TustlerInterfaces.RuntimeOptions() { IsMocked = true } );
            var options = new JsonSerializerOptions();
            options.Converters.Add(new CloudWeaver.Converters.StandardConverter());
            options.Converters.Add(new Converters.LanguageCodeDomainConverter());

            IShareIntraModule awsInterfaceModule = new AWSShareIntraModule(AWSArgument.NewSetAWSInterface(awsInterface));
            Assert.AreEqual("AWSInterface: mocking mode enabled", awsInterfaceModule.Description());
            Assert.IsTrue(StringifyBytes(awsInterfaceModule, options).StartsWith("{\"RuntimeOptions\":{\"IsMocked\":true,\"NotificationsARN\":null,\"NotificationsQueueURL\":null},\"S3\":{},\"Polly\":{},\"SNS\":{},\"SQS\":{},\"Transcribe\":{},\"Translate\":{}}"));

            IShareIntraModule bucketModule = new AWSShareIntraModule(AWSArgument.NewSetBucket(new Bucket() { Name = "test", CreationDate = new DateTime(637370970747723577) }));
            Assert.IsTrue(bucketModule.Description().StartsWith("Bucket: test"));
            Assert.IsTrue(StringifyBytes(bucketModule, options).StartsWith("{\"Name\":\"test\",\"CreationDate\":\"2020-09-30T21:11:14.7723577\"}"));

            var bucketsModel = new BucketViewModel();
            await bucketsModel.Refresh(awsInterface, true, new NotificationsList());
            IShareIntraModule bucketsModelModule = new AWSShareIntraModule(AWSArgument.NewSetBucketsModel(bucketsModel));
            Assert.IsTrue(bucketsModelModule.Description().StartsWith("BucketsModel: tator, test"));
            Assert.IsTrue(StringifyBytes(bucketsModelModule, options).StartsWith("{\"Buckets\":[{\"Name\":\"tator\",\"CreationDate\":"));

            IShareIntraModule s3MediaReferenceModule = new AWSShareIntraModule(AWSArgument.NewSetS3MediaReference(
                new S3MediaReference("test", "key", "audio/x-wav", "wav")));
            Assert.IsTrue(s3MediaReferenceModule.Description().StartsWith("S3MediaReference: key key from test (audio/x-wav)"));
            Assert.IsTrue(StringifyBytes(s3MediaReferenceModule, options).StartsWith("{\"BucketName\":\"test\",\"Key\":\"key\",\"MimeType\":\"audio/x-wav\",\"Extension\":\"wav\"}"));

            IShareIntraModule transcriptionJobNameModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobName("test_job"));
            Assert.IsTrue(transcriptionJobNameModule.Description().StartsWith("TranscriptionJobName: test_job"));
            Assert.IsTrue(StringifyBytes(transcriptionJobNameModule, options).StartsWith("test_job"));

            var path = Path.Combine(WorkingDirectory, "SallyRide d2a8856b.json");
            var data = File.ReadAllBytes(path);
            IShareIntraModule transcriptionJsonModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptJSON(data));
            Assert.IsTrue(transcriptionJsonModule.Description().StartsWith("TranscriptJSON: 59850 bytes"));
            Assert.IsTrue(StringifyBytes(transcriptionJsonModule, options).StartsWith("\"ew0KICAgICJqb2JOYW1lIjogImQyYTg4NTZiLWJkOWEtNDliZi1hNTRhLTVkOTFkZjRiNzNmNyIsDQogICAgImFjY291bnRJZCI6ICIyNjE5MTQwM"));

            IShareIntraModule transcriptionDefaultTranscriptModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionDefaultTranscript("test transcript"));
            Assert.IsTrue(transcriptionDefaultTranscriptModule.Description().StartsWith("TranscriptionDefaultTranscript: test transcript"));
            Assert.IsTrue(StringifyBytes(transcriptionDefaultTranscriptModule, options).StartsWith("test transcript"));

            IShareIntraModule transcriptURIModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptURI("test URI"));
            Assert.IsTrue(transcriptURIModule.Description().StartsWith("TranscriptURI: test URI"));
            Assert.IsTrue(StringifyBytes(transcriptURIModule, options).StartsWith("test URI"));

            var transcriptionJobsModel = new TranscriptionJobsViewModel();
            await transcriptionJobsModel.ListTasks(awsInterface, new NotificationsList());  // -> three jobs from Mock interface (note that the order varies)
            IShareIntraModule transcriptionJobsModelModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionJobsModel(transcriptionJobsModel));
            Assert.IsTrue(transcriptionJobsModelModule.Description().StartsWith("TranscriptionJobsModel: myJob1, myJob2, myJob3"));
            Assert.IsTrue(StringifyBytes(transcriptionJobsModelModule, options).StartsWith("{\"TranscriptionJobs\":[{\"TranscriptionJobName\":"));

            IShareIntraModule fileMediaReferenceModule = new StandardShareIntraModule(StandardArgument.NewSetFileMediaReference(
                new FileMediaReference("my path", "audio/x-wav", "wav")));
            Assert.IsTrue(fileMediaReferenceModule.Description().StartsWith("FileMediaReference: my path of type audio/x-wav"));
            Assert.IsTrue(StringifyBytes(fileMediaReferenceModule, options).StartsWith("{\"FilePath\":\"my path\",\"MimeType\":\"audio/x-wav\",\"Extension\":\"wav\"}"));

            //IShareIntraModule transcriptionLanguageCodeModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionLanguageCode("en-US"));
            //Assert.IsTrue(transcriptionLanguageCodeModule.Description().StartsWith("TranscriptionLanguageCode: en-US"));
            //Assert.IsTrue(StringifyBytes(transcriptionLanguageCodeModule).StartsWith("en-US"));

            IShareIntraModule transcriptionVocabularyNameModule = new AWSShareIntraModule(AWSArgument.NewSetTranscriptionVocabularyName("[None]"));
            Assert.IsTrue(transcriptionVocabularyNameModule.Description().StartsWith("TranscriptionVocabularyName: [None]"));
            Assert.IsTrue(StringifyBytes(transcriptionVocabularyNameModule, options).StartsWith("[None]"));

            //IShareIntraModule translationLanguageCodeSourceModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationLanguageCodeSource("en"));
            //Assert.IsTrue(translationLanguageCodeSourceModule.Description().StartsWith("TranslationLanguageCodeSource: en"));
            //Assert.IsTrue(StringifyBytes(translationLanguageCodeSourceModule).StartsWith("en"));

            var languageCodeDomain = new LanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US");
            IShareIntraModule languageCodeDomainModule = new AWSShareIntraModule(AWSArgument.NewSetLanguage(languageCodeDomain));
            Assert.IsTrue(languageCodeDomainModule.Description().StartsWith("Transcription Language: American English (en-US)"));

            var languageCode = JsonSerializer.Deserialize<LanguageCodeDomain>(new ReadOnlySpan<byte>(languageCodeDomainModule.AsBytes(options)), options);
            Assert.IsTrue((languageCode.LanguageDomain == LanguageDomain.Transcription) && (languageCode.Name == "American English") && (languageCode.Code == "en-US"));

            var items = new AWSShareIterationArgument[]
            {
                new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(new TustlerModels.LanguageCode() { Name="Arabic", Code="ar" })),
                new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(new TustlerModels.LanguageCode() { Name="Azerbaijani", Code="az" }))
            };
            var consumable = new AWSIterationStack(Guid.NewGuid(), items);
            IShareIntraModule translationTargetLanguagesModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationTargetLanguages(consumable));
            Assert.IsTrue(translationTargetLanguagesModule.Description().StartsWith("TranslationTargetLanguages: Arabic, Azerbaijani"));
            Assert.IsTrue(StringifyBytes(translationTargetLanguagesModule, options).StartsWith("[{\"Name\":\"Arabic\",\"Code\":\"ar\"},{\"Name\":\"Azerbaijani\",\"Code\":\"az\"}]"));

            IShareIntraModule translationTerminologyNamesModule = new AWSShareIntraModule(AWSArgument.NewSetTranslationTerminologyNames(
                new List<string>() { "bob", "jane" }));
            Assert.IsTrue(translationTerminologyNamesModule.Description().StartsWith("TranslationTerminologyNames: bob, jane"));
            Assert.IsTrue(StringifyBytes(translationTerminologyNamesModule, options).StartsWith("[\"bob\",\"jane\"]"));

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
            Assert.IsTrue(StringifyBytes(translationSegmentsModule, options).StartsWith("{\"NumChunks\":3,\"Chunks\":[{\"Key\":0,\"Value\":\"This is a test transcript.\"}"));

            IShareIntraModule subtitleFilePathModule = new AWSShareIntraModule(AWSArgument.NewSetSubtitleFilePath(new FileInfo("my path")));
            Assert.IsTrue(subtitleFilePathModule.Description().StartsWith($"SubtitleFilePath: {new FileInfo("my path").FullName}"));
            Assert.IsTrue(StringifyBytes(subtitleFilePathModule, options).StartsWith(new FileInfo("my path").FullName));
        }
    }
}
