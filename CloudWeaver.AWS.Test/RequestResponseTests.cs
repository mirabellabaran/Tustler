using CloudWeaver.Foundation.Types;
//using CloudWeaver.MediaServices;
using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerFFMPEG.Types.CodecInfo;
using TustlerFFMPEG.Types.MediaInfo;
using TustlerInterfaces;
using TustlerModels;
using static CloudWeaver.AWS.Converters;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class RequestResponseTests
    {
        [TestMethod]
        public async Task TestRequestResponse()
        {
            var typeResolver = await TypeResolver.Create();

            var agent = await InitializeTestAsync();

            var serializerOptions = CloudWeaver.Converters.CreateSerializerOptions(typeResolver);
            Assert.IsTrue(serializerOptions.Converters.Count == 8);

            // Test type serialization and deserialization: each of the following will throw if not correct
            // each call tests that the second argument can be deserialized into the type requested by the first argument

            // Deserialize an empty SubTaskInputs instance
            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSubTaskInputs),
                JsonSerializer.SerializeToUtf8Bytes(new SubTaskInputs(), serializerOptions),
                agent, typeResolver);

            // Deserialize a SubTaskInputs instance w/ four requests
            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSubTaskInputs),
                JsonSerializer.SerializeToUtf8Bytes(new SubTaskInputs(new IRequestIntraModule[] {
                    new AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode),
                    new AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName),
                    new AWSRequestIntraModule(AWSRequest.RequestBucket),
                    new StandardRequestIntraModule(StandardRequest.RequestFileMediaReference)
                }), serializerOptions),
                agent, typeResolver);

            TestRequest(
                typeResolver.CreateRequest("CloudWeaver.MediaServices.AVRequestIntraModule", "RequestCodecInfo"),
                typeResolver.CreateSerializedArgument("CloudWeaver.MediaServices.AVRequestIntraModule", "RequestCodecInfo", new CodecPair()),
                agent, typeResolver);

            // MG note this should fail because the object instance is of the wrong type
            TestRequest(
                typeResolver.CreateRequest("CloudWeaver.MediaServices.AVRequestIntraModule", "RequestMediaInfo"),
                typeResolver.CreateSerializedArgument("CloudWeaver.MediaServices.AVRequestIntraModule", "RequestMediaInfo", new MediaInfo()),
                agent, typeResolver);



            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestBucket),
                SerializableTypeGenerator.CreateBucket("test bucket", DateTime.SpecifyKind(new DateTime(2020, 1, 1, 12, 45, 30), DateTimeKind.Local)),
                agent, typeResolver);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestFileMediaReference),
                SerializableTypeGenerator.CreateFileMediaReference("some file path", "audio/x-wav", "wav"),
                agent, typeResolver);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestOpenJsonFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "json", FilePickerMode.Open),
                agent, typeResolver);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSaveJsonFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "json", FilePickerMode.Save),
                agent, typeResolver);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestOpenLogFormatFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "bin", FilePickerMode.Open),
                agent, typeResolver);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSaveLogFormatFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "bin", FilePickerMode.Save),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode),
                JsonSerializer.SerializeToUtf8Bytes(new LanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"), serializerOptions),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource),
                JsonSerializer.SerializeToUtf8Bytes(new LanguageCodeDomain(LanguageDomain.Transcription, "English", "en"), serializerOptions),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript),
                SerializableTypeGenerator.CreateTranscriptionDefaultTranscript("my default transcript"),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName),
                JsonSerializer.SerializeToUtf8Bytes(new VocabularyName("my vocabulary"), serializerOptions),
                agent, typeResolver);

            // with null
            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName),
                JsonSerializer.SerializeToUtf8Bytes(new VocabularyName(null), serializerOptions),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages),
                SerializableTypeGenerator.CreateTranslationTargetLanguageCodes("AWSShareIterationArgument",
                new LanguageCode[]
                {
                    new TustlerModels.LanguageCode() { Name = "Arabic", Code = "ar" },
                    new TustlerModels.LanguageCode() { Name = "Azerbaijani", Code = "az" }
                }
                .Cast<LanguageCode>()
                .Select(lc => new AWSShareIterationArgument(AWSIterationArgument.NewLanguageCode(lc))),
                serializerOptions),
                agent, typeResolver);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames),
                SerializableTypeGenerator.CreateTranslationTerminologyNames(new List<string>() { "Bob", "Sally" }),
                agent, typeResolver);
        }

        private void TestRequest(IRequestIntraModule request, byte[] data, Agent agent, TypeResolver typeResolver)
        {
            agent.AddArgument(request, GetModuleName(request, typeResolver), GetPropertyName(request, typeResolver), data);
        }

        private static async Task<Agent> InitializeTestAsync()
        {
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var taskLogger = new TaskLogger();

            var taskFunctionResolver = await TaskFunctionResolver.Create();
            var agent = new Agent(knownArguments, taskFunctionResolver, taskLogger, retainResponses: true);

            return agent;
        }

        private static string GetModuleName(IRequestIntraModule request, TypeResolver typeResolver)
        {
            return typeResolver.GetMatchingArgument(request);
            //return request switch
            //{
            //    // map the request to the shareable settable type
            //    StandardRequestIntraModule _ => "StandardShareIntraModule",
            //    AWSRequestIntraModule _ => "AWSShareIntraModule",
            //    AVRequestIntraModule _ => "AVShareIntraModule",
            //    _ => throw new ArgumentException()
            //};
        }

        private static string GetPropertyName(IRequestIntraModule request, TypeResolver typeResolver)
        {
            string result;

            if (request is AWSRequestIntraModule awsReq &&
                (awsReq.Request == AWSRequest.RequestTranscriptionLanguageCode ||
                awsReq.Request == AWSRequest.RequestTranslationLanguageCodeSource
                )
            )
            {
                result = "SetLanguage";
            }
            else if (request is StandardRequestIntraModule stdReq &&
                (
                stdReq.Request == StandardRequest.RequestOpenJsonFilePath ||
                stdReq.Request == StandardRequest.RequestSaveJsonFilePath ||
                stdReq.Request == StandardRequest.RequestOpenLogFormatFilePath ||
                stdReq.Request == StandardRequest.RequestSaveLogFormatFilePath
                ))
            {
                result = "SetFilePath";
            }
            else
            {
                var requestAsString = typeResolver.GetRequestAsString(request);
                //var requestAsString = request switch
                //{
                //    StandardRequestIntraModule standardRequest => standardRequest.Request.ToString(),
                //    AWSRequestIntraModule awsRequest => awsRequest.Request.ToString(),
                //    AVRequestIntraModule avRequest => avRequest.Request.ToString(),
                //    _ => throw new ArgumentException()
                //};

                result = $"Set{requestAsString[7..]}";
            }

            return result;
        }
    }
}