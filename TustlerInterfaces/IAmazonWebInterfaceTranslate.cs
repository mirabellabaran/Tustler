using Amazon;
using Amazon.Translate.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TustlerInterfaces
{
    public interface IAmazonWebInterfaceTranslate
    {
        public abstract Task<AWSResult<string>> TranslateText(string sourceLanguageCode, string targetLanguageCode, string text, List<string> terminologyNames);
        public abstract Task<AWSResult<TranslateJobStatus>> StartTextTranslationJob(string jobName, RegionEndpoint region, string dataAccessRoleArn, string sourceLanguageCode, List<string> targetLanguageCodes, string s3InputFolderName, string s3OutputFolderName, List<string> terminologyNames);
        public abstract Task<AWSResult<TranslateJobStatus>> StopTextTranslationJob(string jobId, RegionEndpoint region);
        public abstract Task<AWSResult<List<TextTranslationJobProperties>>> ListTextTranslationJobs(RegionEndpoint region);
        public abstract Task<AWSResult<List<TerminologyProperties>>> ListTerminologies();
    }
}
