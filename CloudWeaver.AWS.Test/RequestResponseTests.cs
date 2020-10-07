using CloudWeaver.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace CloudWeaver.AWS.Test
{
    [TestClass]
    public class RequestResponseTests
    {
        [TestMethod]
        public void TestRequestResponse()
        {
            var agent = InitializeTest();

            // the following will all throw if not correct

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestBucket),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestBucket),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Translation, "English", "en"),
                agent);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestFileMediaReference),
                SerializableTypeGenerator.CreateFileMediaReference("some file path", "audio/x-wav", "wav"),
                agent);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestOpenJsonFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "json", FilePickerMode.Open),
                agent);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSaveJsonFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "json", FilePickerMode.Save),
                agent);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestOpenLogFormatFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "bin", FilePickerMode.Open),
                agent);

            TestRequest(
                new StandardRequestIntraModule(StandardRequest.RequestSaveLogFormatFilePath),
                SerializableTypeGenerator.CreateFilePath(new System.IO.FileInfo("some path"), "bin", FilePickerMode.Save),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionLanguageCode),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionDefaultTranscript),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranscriptionVocabularyName),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTargetLanguages),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationTerminologyNames),
                SerializableTypeGenerator.CreateLanguageCodeDomain(LanguageDomain.Transcription, "American English", "en-US"),
                agent);
        }

        private void TestRequest(IRequestIntraModule request, byte[] data, Agent agent)
        {
            agent.AddArgument(GetModuleName(request), GetPropertyName(request), data);
        }

        private static Agent InitializeTest()
        {
            var notificationsList = new NotificationsList();
            var awsInterface = new AmazonWebServiceInterface(new RuntimeOptions() { IsMocked = true });

            KnownArgumentsCollection knownArguments = new KnownArgumentsCollection();
            knownArguments.AddModule(new StandardKnownArguments(notificationsList));
            knownArguments.AddModule(new AWSKnownArguments(awsInterface));

            var specifier = new TaskFunctionSpecifier(null, null, null, false);
            var taskLogger = new TaskLogger();

            var agent = new Agent(knownArguments, specifier, taskLogger, retainResponses: true);

            return agent;
        }

        private static string GetModuleName(IRequestIntraModule request)
        {
            return request switch
            {
                StandardRequestIntraModule _ => "StandardShareIntraModule",
                AWSRequestIntraModule _ => "AWSShareIntraModule",
                _ => throw new ArgumentException()
            };
        }

        private static string GetPropertyName(IRequestIntraModule request)
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
                var requestAsString = request switch
                {
                    StandardRequestIntraModule standardRequest => standardRequest.Request.ToString(),
                    AWSRequestIntraModule awsRequest => awsRequest.Request.ToString(),
                    _ => throw new ArgumentException()
                };

                result = $"Set{requestAsString.Substring(7)}";
            }

            return result;
        }
    }
}