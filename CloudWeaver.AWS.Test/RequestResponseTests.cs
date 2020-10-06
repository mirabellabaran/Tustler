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
                new StandardRequestIntraModule(StandardRequest.RequestFileMediaReference),
                SerializableTypeGenerator.CreateFileMediaReference("some file path", "audio/x-wav", "wav"),
                agent);

            TestRequest(
                new AWSRequestIntraModule(AWSRequest.RequestTranslationLanguageCodeSource),
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