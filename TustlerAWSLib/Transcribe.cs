using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TustlerAWSLib
{
    public class Transcribe
    {
        /// <summary>
        /// Start a new transcription job, an asynchronous speech recognition task that produces a transcript from an audio file
        /// </summary>
        /// <param name="jobName">An arbitrary name for the job (must be unique in the S3 bucket)</param>
        /// <param name="bucketName">An S3 buvket name</param>
        /// <param name="s3MediaKey">An S3 bucket key referring to a audio file</param>
        /// <param name="languageCode">The launguage code of the language used in the audio file</param>
        /// <param name="vocabularyName">The name of an optional trascribe vocabulary</param>
        /// <returns></returns>
        /// <remarks>Supported formats: Flac, Mp3, Mp4, Wav</remarks>
        public async static Task<AWSResult<TranscriptionJob>> StartTranscriptionJob(string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            try
            {
                using (var client = new AmazonTranscribeServiceClient())
                {
                    var medias3Location = $"s3://{bucketName}/{s3MediaKey}";
                    var request = new StartTranscriptionJobRequest
                    {
                        TranscriptionJobName = jobName,
                        LanguageCode = languageCode,
                        OutputBucketName = bucketName,
                        Settings =
                        {
                            VocabularyName = vocabularyName,
                            ShowAlternatives = false
                        },
                        Media =
                        {
                            MediaFileUri = medias3Location
                        },
                        JobExecutionSettings = null        // requires configuring a service role
                    };
                    var response = await client.StartTranscriptionJobAsync(request);

                    return new AWSResult<TranscriptionJob>(response.TranscriptionJob, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(StartTranscriptionJob), "Not connected.", ex));
            }
            catch (InternalFailureException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(StartTranscriptionJob), "There was an internal error. Check the error message and try your request again.", ex));
            }
            catch (LimitExceededException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(StartTranscriptionJob), "Either you have sent too many requests or your input file is too long.", ex));
            }
        }

        public async static Task<AWSResult<TranscriptionJob>> GetTranscriptionJob(string jobName)
        {
            try
            {
                using (var client = new AmazonTranscribeServiceClient())
                {
                    var request = new GetTranscriptionJobRequest
                    {
                        TranscriptionJobName = jobName,
                    };
                    var response = await client.GetTranscriptionJobAsync(request);

                    return new AWSResult<TranscriptionJob>(response.TranscriptionJob, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(GetTranscriptionJob), "Not connected.", ex));
            }
            catch (InternalFailureException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(GetTranscriptionJob), "There was an internal error. Check the error message and try your request again.", ex));
            }
            catch (LimitExceededException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(GetTranscriptionJob), "Either you have sent too many requests or your input file is too long.", ex));
            }
            catch (NotFoundException ex)
            {
                return new AWSResult<TranscriptionJob>(null, new AWSException(nameof(GetTranscriptionJob), "We can't find the requested resource. Check the name and try your request again.", ex));
            }
        }

        public async static Task<AWSResult<List<TranscriptionJobSummary>>> ListTranscriptionJobs()
        {
            try
            {
                using (var client = new AmazonTranscribeServiceClient())
                {
                    var request = new ListTranscriptionJobsRequest();
                    var result = new List<TranscriptionJobSummary>();
                    ListTranscriptionJobsResponse response;
                    do
                    {
                        response = await client.ListTranscriptionJobsAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.TranscriptionJobSummaries);
                    } while (response.NextToken != null);

                    return new AWSResult<List<TranscriptionJobSummary>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<TranscriptionJobSummary>>(null, new AWSException(nameof(ListTranscriptionJobs), "Not connected.", ex));
            }
            catch (InternalFailureException ex)
            {
                return new AWSResult<List<TranscriptionJobSummary>>(null, new AWSException(nameof(ListTranscriptionJobs), "There was an internal error. Check the error message and try your request again.", ex));
            }
            catch (LimitExceededException ex)
            {
                return new AWSResult<List<TranscriptionJobSummary>>(null, new AWSException(nameof(ListTranscriptionJobs), "Either you have sent too many requests or your input file is too long.", ex));
            }
        }

        public async static Task<AWSResult<List<VocabularyInfo>>> ListVocabularies()
        {
            try
            {
                using (var client = new AmazonTranscribeServiceClient())
                {
                    var request = new ListVocabulariesRequest();
                    var result = new List<VocabularyInfo>();
                    ListVocabulariesResponse response;
                    do
                    {
                        response = await client.ListVocabulariesAsync(request);
                        request.NextToken = response.NextToken;

                        result.AddRange(response.Vocabularies);
                    } while (response.NextToken != null);

                    return new AWSResult<List<VocabularyInfo>>(result, null);
                }
            }
            catch (HttpRequestException ex)
            {
                return new AWSResult<List<VocabularyInfo>>(null, new AWSException(nameof(ListVocabularies), "Not connected.", ex));
            }
            catch (InternalFailureException ex)
            {
                return new AWSResult<List<VocabularyInfo>>(null, new AWSException(nameof(ListVocabularies), "There was an internal error. Check the error message and try your request again.", ex));
            }
            catch (LimitExceededException ex)
            {
                return new AWSResult<List<VocabularyInfo>>(null, new AWSException(nameof(ListVocabularies), "Either you have sent too many requests or your input file is too long.", ex));
            }
        }
    }
}
