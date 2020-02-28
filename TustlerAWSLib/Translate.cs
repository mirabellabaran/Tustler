using Amazon;
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

        /// <summary>
        /// Start a batch translation job (only available in some regions)
        /// </summary>
        /// <param name="jobName">An arbitrary name for the job</param>
        /// <param name="region">The name of an AWS RegionEndpoint that support batch translation</param>
        /// <param name="dataAccessRoleArn">A service role allowing the batch translation service read/write access to the S3 app bucket</param>
        /// <param name="sourceLanguageCode">The language code for the source documents</param>
        /// <param name="targetLanguageCodes">One or more language codes for the output documents</param>
        /// <param name="s3InputFolderName">An S3 pseudo folder containing the source documents</param>
        /// <param name="s3OutputFolderName">An S3 pseudo folder where output documents will be written</param>
        /// <param name="terminologyNames">The names of any predefined translation terminologies (optional)</param>
        /// <returns></returns>
        public async static Task<AWSResult<TranslateJobStatus>> StartTextTranslationJob(string jobName, RegionEndpoint region, string dataAccessRoleArn, string sourceLanguageCode, List<string> targetLanguageCodes, string s3InputFolderName, string s3OutputFolderName, List<string> terminologyNames)
        {
            try
            {
                using (var client = new AmazonTranslateClient(region))
                {
                    var request = new StartTextTranslationJobRequest
                    {
                        JobName = jobName,
                        DataAccessRoleArn = dataAccessRoleArn,
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
            catch (AmazonTranslateException ex)
            {
                return new AWSResult<TranslateJobStatus>(null, new AWSException(nameof(StartTextTranslationJob), "Translate error. Check your Amazon region permits starting a long-running translation job.", ex));
            }
        }

        public async static Task<AWSResult<TranslateJobStatus>> StopTextTranslationJob(string jobId, RegionEndpoint region)
        {
            try
            {
                using (var client = new AmazonTranslateClient(region))
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

        public async static Task<AWSResult<List<TextTranslationJobProperties>>> ListTextTranslationJobs(RegionEndpoint region)
        {
            try
            {
                using (var client = new AmazonTranslateClient(region))
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
            catch (AmazonTranslateException ex)
            {
                return new AWSResult<List<TextTranslationJobProperties>>(null, new AWSException(nameof(ListTextTranslationJobs), "Translate error.", ex));
            }
        }

        public async static Task<AWSResult<List<TerminologyProperties>>> ListTerminologies()
        {
            try
            {
                using (var client = new AmazonTranslateClient())
                {
                    var request = new ListTerminologiesRequest();
                    var result = new List<TerminologyProperties>();
                    ListTerminologiesResponse response;
                    do
                    {
                        response = await client.ListTerminologiesAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.TerminologyPropertiesList);
                    } while (response.NextToken != null);

                    return new AWSResult<List<TerminologyProperties>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<TerminologyProperties>>(null, new AWSException(nameof(ListTerminologies), "Not connected.", ex));
            }
            catch (InternalServerException ex)
            {
                return new AWSResult<List<TerminologyProperties>>(null, new AWSException(nameof(ListTerminologies), "An internal server error occurred. Retry your request.", ex));
            }
            catch (TooManyRequestsException ex)
            {
                return new AWSResult<List<TerminologyProperties>>(null, new AWSException(nameof(ListTerminologies), "You have made too many requests within a short period of time. Wait for a short time and then try your request again.", ex));
            }
        }

    }
}
