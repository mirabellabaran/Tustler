using Amazon.Translate;
using Amazon.Translate.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TustlerAWSLib
{
    public class Translate
    {
        /// <summary>
        /// Translate the supplied text from the specified source language to the target language
        /// </summary>
        /// <param name="sourceLanguageCode">The language code of the source text</param>
        /// <param name="targetLanguageCode">The language code of the target text</param>
        /// <param name="text">The text to be translated</param>
        /// <param name="terminologyNames">A list of names of translation terminlogies (defined previously)</param>
        /// <returns></returns>
        public async static Task<AWSResult<string>> TranslateText(string sourceLanguageCode, string targetLanguageCode, string text, List<string> terminologyNames)
        {
            try
            {
                using (var client = new AmazonTranslateClient())
                {
                    var request = new TranslateTextRequest
                    {
                        SourceLanguageCode = sourceLanguageCode,
                        TargetLanguageCode = targetLanguageCode,
                        TerminologyNames = terminologyNames,
                        Text = text
                    };
                    var response = await client.TranslateTextAsync(request);

                    return new AWSResult<string>(response.TranslatedText, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "Not connected.", ex));
            }
            catch (InternalServerException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "An internal server error occurred. Retry your request.", ex));
            }
            catch (ServiceUnavailableException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "The Amazon Translate service is temporarily unavailable. Please wait a bit and then retry your request.", ex));
            }
            catch (TextSizeLimitExceededException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "The size of the text you submitted exceeds the size limit. Reduce the size of the text and then retry your request.", ex));
            }
            catch (TooManyRequestsException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "You have made too many requests within a short period of time. Wait for a short time and then try your request again.", ex));
            }
            catch (UnsupportedLanguagePairException ex)
            {
                return new AWSResult<string>(null, new AWSException(nameof(TranslateText), "Amazon Translate does not support translation from the language of the source text into the requested target language.", ex));
            }
        }

        public async static Task<AWSResult<TranslateJobStatus>> StartTextTranslationJob(string jobName, string sourceLanguageCode, List<string> targetLanguageCodes, string s3InputFolderName, string s3OutputFolderName, List<string> terminologyNames)
        {
            try
            {
                using (var client = new AmazonTranslateClient())
                {
                    var request = new StartTextTranslationJobRequest
                    {
                        JobName = jobName,
                        SourceLanguageCode = sourceLanguageCode,
                        TargetLanguageCodes = targetLanguageCodes,
                        InputDataConfig = new InputDataConfig
                        {
                            ContentType = "text/plain",
                            S3Uri = s3InputFolderName
                        },
                        OutputDataConfig = new OutputDataConfig
                        {
                            S3Uri = s3OutputFolderName
                        },
                        TerminologyNames = terminologyNames,
                    };
                    var response = await client.StartTextTranslationJobAsync(request);

                    return new AWSResult<TranslateJobStatus>(new TranslateJobStatus(response.JobId, response.JobStatus), null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StartTextTranslationJob), "Not connected.", ex));
            }
            catch (InternalServerException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StartTextTranslationJob), "An internal server error occurred. Retry your request.", ex));
            }
            catch (TooManyRequestsException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StartTextTranslationJob), "You have made too many requests within a short period of time. Wait for a short time and then try your request again.", ex));
            }
            catch (UnsupportedLanguagePairException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StartTextTranslationJob), "Amazon Translate does not support translation from the language of the source text into the requested target language.", ex));
            }
        }

        public async static Task<AWSResult<TranslateJobStatus>> StopTextTranslationJob(string jobId)
        {
            try
            {
                using (var client = new AmazonTranslateClient())
                {
                    var request = new StopTextTranslationJobRequest
                    {
                        JobId = jobId
                    };
                    var response = await client.StopTextTranslationJobAsync(request);

                    return new AWSResult<TranslateJobStatus>(new TranslateJobStatus(response.JobId, response.JobStatus), null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StopTextTranslationJob), "Not connected.", ex));
            }
            catch (InternalServerException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StopTextTranslationJob), "An internal server error occurred. Retry your request.", ex));
            }
            catch (TooManyRequestsException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StopTextTranslationJob), "You have made too many requests within a short period of time. Wait for a short time and then try your request again.", ex));
            }
        }

        public async static Task<AWSResult<List<TextTranslationJobProperties>>> ListTextTranslationJobs()
        {
            try
            {
                using (var client = new AmazonTranslateClient())
                {
                    var request = new ListTextTranslationJobsRequest();
                    var result = new List<TextTranslationJobProperties>();
                    ListTextTranslationJobsResponse response;
                    do
                    {
                        response = await client.ListTextTranslationJobsAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.TextTranslationJobPropertiesList);
                    } while (response.NextToken != null);

                    return new AWSResult<List<TextTranslationJobProperties>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<TextTranslationJobProperties>>(null, new AWSException(nameof(ListTextTranslationJobs), "Not connected.", ex));
            }
            catch (InternalServerException ex)
            {
                return new AWSResult<List<TextTranslationJobProperties>>(null, new AWSException(nameof(ListTextTranslationJobs), "An internal server error occurred. Retry your request.", ex));
            }
            catch (TooManyRequestsException ex)
            {
                return new AWSResult<List<TextTranslationJobProperties>>(null, new AWSException(nameof(ListTextTranslationJobs), "You have made too many requests within a short period of time. Wait for a short time and then try your request again.", ex));
            }
        }
    }
}
