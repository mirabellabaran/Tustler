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
        /// Start a new transcription job that produces a transcript from an audio file
        /// </summary>
        /// <param name="jobName">An arbitrary name for the job (must be unique in the S3 bucket)</param>
        /// <param name="bucketName">An S3 buvket name</param>
        /// <param name="s3MediaKey">An S3 bucket key referring to a audio file</param>
        /// <param name="mediaFormat">The format of the audio file</param>
        /// <param name="languageCode">The launguage code of the language used in the audio file</param>
        /// <param name="vocabularyName">The name of an optional trascribe vocabulary</param>
        /// <returns></returns>
        public async static Task<AWSResult<TranscriptionJob>> StartTranscriptionJob(string jobName, string bucketName, string s3MediaKey, string mediaFormat, string languageCode, string vocabularyName)
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
                        MediaFormat = mediaFormat,
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
    }
}

//// TranscribeStartTranscriptionJob start an asynchronous speech recognition (transcription) task
//func TranscribeStartTranscriptionJob(transcribeService* transcribeservice.TranscribeService, jobName string, bucketName string, medias3Location string, languageCode string) (*string, error) {

//	input := &transcribeservice.StartTranscriptionJobInput{
//		LanguageCode: aws.String(languageCode),
//		Media: &transcribeservice.Media{
//			MediaFileUri: aws.String(medias3Location), // S3 location of the input media file
//		},
//		//MediaFormat: aws.String(mediaFormat),
//		OutputBucketName:     aws.String(bucketName),
//		TranscriptionJobName: aws.String(jobName),
//	}

//	result, err := transcribeService.StartTranscriptionJob(input)
//	if err != nil {
//		msg := fmt.Sprintf("Transcription failed for job %s", jobName)
//		return nil, getTatorError("TranscribeStartTranscriptionJob", msg, err)
//	}

//	job, err := getJSONString(result.TranscriptionJob)
//	return job, err
//}

//// TranscribeGetTranscriptionJob get the status of an asynchronous speech recognition (transcription) task
//func TranscribeGetTranscriptionJob(transcribeService* transcribeservice.TranscribeService, jobName string) (*string, error) {

//	input := &transcribeservice.GetTranscriptionJobInput{
//		TranscriptionJobName: aws.String(jobName),
//	}

//	result, err := transcribeService.GetTranscriptionJob(input)
//	if err != nil {
//		msg := fmt.Sprintf("Get transcription job failed (job name: %s)", jobName)
//		return nil, getTatorError("TranscribeGetTranscriptionJob", msg, err)
//	}

//	job, err := getJSONString(result.TranscriptionJob)
//	return job, err
//}

//// TranscribeListTranscriptionJobs display a list of speech recognition (transcription) tasks
//func TranscribeListTranscriptionJobs(transcribeService* transcribeservice.TranscribeService) (*string, error) {

//	result, err := transcribeService.ListTranscriptionJobs(nil)
//	if err != nil {
//		msg := "List transcription jobs failed"
//		return nil, getTatorError("TranscribeListTranscriptionJobs", msg, err)
//	}

//	jobs, err := getJSONString(result)
//	return jobs, err
//}
