using Amazon.Polly;
using CloudWeaver.Foundation.Types;
using System.IO;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels.Services
{
    /// <summary>
    /// Manages Polly services
    /// </summary>
    public static class PollyServices
    {
        public static async Task<AWSResult<PollyAudioStream>> SynthesizeSpeech(AmazonWebServiceInterface awsInterface, string text, bool useNeural, string voiceId)
        {
            return await awsInterface.Polly.SynthesizeSpeech(text, useNeural ? Engine.Neural : Engine.Standard, voiceId is null ? "Joanna" : voiceId).ConfigureAwait(true);
        }

        public static (MemoryStream AudioStream, string ContentType) ProcessSynthesizeSpeechResult(NotificationsList notifications, AWSResult<PollyAudioStream> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return (null, null);
            }
            else
            {
                var response = result.Result;

                return (response.AudioStream, response.ContentType);
            }
        }

    }
}
