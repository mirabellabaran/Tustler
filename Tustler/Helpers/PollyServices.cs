using Amazon.Polly;
using Amazon.Polly.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
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
        public static async Task<AWSResult<PollyAudioStream>> SynthesizeSpeech(string text, bool useNeural, string voiceId)
        {
            return await Polly.SynthesizeSpeech(text, useNeural ? Engine.Neural : Engine.Standard, voiceId is null ? "Joanna" : voiceId).ConfigureAwait(true);
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
