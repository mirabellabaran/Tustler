using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.TranscribeService.Model;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfaceTranscribe
    {
        public abstract Task<AWSResult<TranscriptionJob>> StartTranscriptionJob(string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName);
        public abstract Task<AWSResult<TranscriptionJob>> GetTranscriptionJob(string jobName);
        public abstract Task<AWSResult<bool>> DeleteTranscriptionJob(string jobName);
        public abstract Task<AWSResult<List<TranscriptionJobSummary>>> ListTranscriptionJobs();
        public abstract Task<AWSResult<List<VocabularyInfo>>> ListVocabularies();
    }
}
