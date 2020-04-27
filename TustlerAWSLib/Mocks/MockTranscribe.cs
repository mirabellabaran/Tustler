using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib.Mocks
{
    public class MockTranscribe : IAmazonWebInterfaceTranscribe
    {
        private readonly AmazonWebServiceInterface awsInterface;

        private readonly Dictionary<string, VocabularyInfo> vocabularyDictionary;                       // keyed by vocabulary name
        private readonly ConcurrentDictionary<string, TranscriptionJob> transcriptionTaskDictionary;    // keyed by job name

        public MockTranscribe(AmazonWebServiceInterface awsInterface)
        {
            this.awsInterface = awsInterface;

            var vocabularies = new VocabularyInfo[]
            {
                new VocabularyInfo()
                {
                    LanguageCode = "en-GB",
                    LastModifiedTime = DateTime.Now - TimeSpan.FromDays(100),
                    VocabularyName = "Bob",
                    VocabularyState = VocabularyState.READY
                },
                new VocabularyInfo()
                {
                    LanguageCode = "es-US",
                    LastModifiedTime = DateTime.Now - TimeSpan.FromDays(100),
                    VocabularyName = "Manuel",
                    VocabularyState = VocabularyState.READY
                }
            }.Select(info => KeyValuePair.Create(info.VocabularyName, info));

            vocabularyDictionary = new Dictionary<string, VocabularyInfo>(vocabularies);

            transcriptionTaskDictionary = new ConcurrentDictionary<string, TranscriptionJob>();
        }

        private TranscriptionJob CreateTranscriptionJob(string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            var medias3Location = $"https://s3.ap-southeast-2.amazonaws.com/{bucketName}/{s3MediaKey}.mp3";

            return new TranscriptionJob()
            {
                CompletionTime = new DateTime(0),
                CreationTime = DateTime.Now,
                FailureReason = "",
                JobExecutionSettings = null,
                LanguageCode = languageCode,
                Media = new Media()
                {
                    MediaFileUri = medias3Location
                },
                MediaFormat = MediaFormat.Mp3,
                MediaSampleRateHertz = 16000,
                Settings = new Settings
                {
                    VocabularyName = vocabularyName,
                    ShowAlternatives = false
                },
                StartTime = DateTime.Now,
                Transcript = null,
                TranscriptionJobName = jobName,
                TranscriptionJobStatus = TranscriptionJobStatus.IN_PROGRESS
            };
        }

        public async Task<AWSResult<TranscriptionJob>> StartTranscriptionJob(string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            await Task.Delay(1000);

            if (transcriptionTaskDictionary.ContainsKey(jobName))
            {
                return await Task.FromResult(new AWSResult<TranscriptionJob>(null,
                    new AWSException(
                        nameof(StartTranscriptionJob),
                        "Conflicting parameters e.g. a jobname is already in use.",
                        new ConflictException("Try again with a different job name.")
                    )
                ));
            }

            var job = CreateTranscriptionJob(jobName, bucketName, s3MediaKey, languageCode, vocabularyName);
            transcriptionTaskDictionary.AddOrUpdate(jobName, job, (_, job) => job);     // asserting that the key will never be reused/updated

            // after a delay, set the job to complete
            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10000);

                // add a new 'file' to MockS3, and set the task as complete
                var mockS3 = awsInterface.S3 as MockS3;
                var key = $"TranscribeTaskOutput-{Guid.NewGuid()}.json";        // actually the output file is just the $"{jobName}.json"
                var newKey = mockS3.AddBucketItem(bucketName, key, "application/json", "json");    // key may be modified if it already exists

                var job = transcriptionTaskDictionary[jobName];
                job.TranscriptionJobStatus = TranscriptionJobStatus.COMPLETED;
                job.CompletionTime = DateTime.Now;
                job.Transcript = new Transcript() { TranscriptFileUri = $"https://s3.ap-southeast-2.amazonaws.com/{bucketName}/{key}" };
            });

            return await Task.FromResult(new AWSResult<TranscriptionJob>(job, null));
        }

        public async Task<AWSResult<TranscriptionJob>> GetTranscriptionJob(string jobName)
        {
            await Task.Delay(1000);

            var result = transcriptionTaskDictionary.ContainsKey(jobName) switch
            {
                true => new AWSResult<TranscriptionJob>(transcriptionTaskDictionary[jobName], null),
                false => new AWSResult<TranscriptionJob>(null, new AWSException("Mock GetTranscriptionJob", $"The job {jobName} does not exist", new KeyNotFoundException("Key not found")))
            };

            return await Task.FromResult(result);
        }

        public async Task<AWSResult<bool?>> DeleteTranscriptionJob(string jobName)
        {
            await Task.Delay(1000);

            var result = transcriptionTaskDictionary.ContainsKey(jobName) switch
            {
                true => new AWSResult<bool?>(transcriptionTaskDictionary.TryRemove(jobName, out TranscriptionJob _), null),
                false => new AWSResult<bool?>(null, new AWSException("Mock DeleteTranscriptionJob", $"The job {jobName} does not exist", new KeyNotFoundException("Key not found")))
            };

            return await Task.FromResult(result);
        }

        public async Task<AWSResult<List<TranscriptionJobSummary>>> ListTranscriptionJobs()
        {
            await Task.Delay(1000);

            var jobSummaries = new List<TranscriptionJobSummary>(transcriptionTaskDictionary.Values.Select(job =>
            {
                return new TranscriptionJobSummary()
                {
                    CompletionTime = job.CompletionTime,
                    CreationTime = job.CreationTime,
                    FailureReason = job.FailureReason,
                    LanguageCode = job.LanguageCode,
                    OutputLocationType = OutputLocationType.CUSTOMER_BUCKET,
                    StartTime = job.StartTime,
                    TranscriptionJobName = job.TranscriptionJobName,
                    TranscriptionJobStatus = job.TranscriptionJobStatus
                };
            }));

            return await Task.FromResult(new AWSResult<List<TranscriptionJobSummary>>(jobSummaries, null));
        }

        public async Task<AWSResult<List<VocabularyInfo>>> ListVocabularies()
        {
            await Task.Delay(1000);

            var vocabularies = new List<VocabularyInfo>(vocabularyDictionary.Values);
            return await Task.FromResult(new AWSResult<List<VocabularyInfo>>(vocabularies, null));
        }
    }
}
