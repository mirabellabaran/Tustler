using Amazon.Polly;
using Amazon.Polly.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfacePolly
    {
        public abstract Task<AWSResult<List<Voice>>> DescribeVoices(string languageCode);
        public abstract Task<AWSResult<LexiconAttributes>> GetLexicon(string lexiconName);
        public abstract Task<AWSResult<List<LexiconDescription>>> ListLexicons();
        public abstract Task<AWSResult<PollyAudioStream>> SynthesizeSpeech(string text, Engine engine, string voiceId);
        public abstract Task<AWSResult<SynthesisTask>> StartSpeechSynthesisTask(string bucketName, string key, string arn, string text, Engine engine, string voiceId);
        public abstract Task<AWSResult<SynthesisTask>> StartSpeechSynthesisTaskFromFile(string bucketName, string key, string arn, string filePath, Engine engine, string voiceId);
        public abstract Task<AWSResult<SynthesisTask>> GetSpeechSynthesisTask(string taskId);
        public abstract Task<AWSResult<List<SynthesisTask>>> ListSpeechSynthesisTasks();
    }
}
