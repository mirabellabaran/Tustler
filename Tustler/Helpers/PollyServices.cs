using Amazon.Polly;
using Amazon.Polly.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerAWSLib;

namespace Tustler.Helpers
{
    /// <summary>
    /// Manages Polly services
    /// </summary>
    public static class PollyServices
    {
        public static async Task<AWSResult<PollyResponse>> SynthesizeSpeech(string text, bool useNeural, string voiceId = "Joanna")
        {
            return await Polly.SynthesizeSpeech(text, useNeural ? Engine.Neural : Engine.Standard, voiceId).ConfigureAwait(true);
        }

        public static (MemoryStream AudioStream, string ContentType, long ContentLength) ProcessSynthesizeSpeechResult(NotificationsList notifications, AWSResult<PollyResponse> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return (null, null, 0);
            }
            else
            {
                var response = result.Result;

                return (response.AudioStream, response.ContentType, response.ContentLength);
            }
        }
    }
}
