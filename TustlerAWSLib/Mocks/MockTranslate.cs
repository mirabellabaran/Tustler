using Amazon;
using Amazon.Translate;
using Amazon.Translate.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TustlerInterfaces;

namespace TustlerAWSLib.Mocks
{
    /// <summary>
    /// A mock version of the Tustler AWS Translate API
    /// </summary>
    /// <remarks>Note that all functions taking RegionEndpoint as a parameter are untested as they currently require access to S3 buckets in only selected regions</remarks>
    public class MockTranslate : IAmazonWebInterfaceTranslate
    {
        private readonly AmazonWebServiceInterface awsInterface;

        private readonly Dictionary<string, TerminologyProperties> terminologyDictionary;                       // keyed by terminology name
        private readonly ConcurrentDictionary<string, TextTranslationJobProperties> translationTaskDictionary;  // keyed by JobId

        public MockTranslate(AmazonWebServiceInterface awsInterface)
        {
            this.awsInterface = awsInterface;

            var terminologies = new TerminologyProperties[]
            {
                new TerminologyProperties()
                {
                    Arn = "arn:aws:translate:ap-southeast-2:261914005867:terminology/parenting_connect/LATEST",
                    CreatedAt = DateTime.Now - TimeSpan.FromDays(100),
                    Description = null,
                    EncryptionKey = null,
                    LastUpdatedAt = DateTime.Now - TimeSpan.FromDays(100),
                    Name = "parenting_connect",
                    SizeBytes = 42,
                    SourceLanguageCode = "en",
                    TargetLanguageCodes = new List<string>() { "en" },
                    TermCount = 1
                },
                new TerminologyProperties()
                {
                    Arn = "arn:aws:translate:ap-southeast-2:261914005867:terminology/test/LATEST",
                    CreatedAt = DateTime.Now - TimeSpan.FromDays(100),
                    Description = null,
                    EncryptionKey = null,
                    LastUpdatedAt = DateTime.Now - TimeSpan.FromDays(100),
                    Name = "test",
                    SizeBytes = 42,
                    SourceLanguageCode = "en",
                    TargetLanguageCodes = new List<string>() { "en" },
                    TermCount = 1
                }
            }.Select(property => KeyValuePair.Create(property.Name, property));

            terminologyDictionary = new Dictionary<string, TerminologyProperties>(terminologies);

            translationTaskDictionary = new ConcurrentDictionary<string, TextTranslationJobProperties>();
        }

        private TextTranslationJobProperties CreateTranslationJob(string jobId, string jobName, string dataAccessRoleArn, string s3InputFolderName, string s3OutputFolderName, string sourceLanguageCode, List<string> targetLanguageCodes, List<string> terminologyNames)
        {
            return new TextTranslationJobProperties()
            {
                DataAccessRoleArn = dataAccessRoleArn,
                EndTime = new DateTime(0),
                InputDataConfig = new InputDataConfig
                {
                    ContentType = "text/plain",
                    S3Uri = s3InputFolderName
                },
                JobDetails = null,
                JobId = jobId,
                JobName = jobName,
                JobStatus = JobStatus.IN_PROGRESS,
                Message = "",
                OutputDataConfig = new OutputDataConfig()
                {
                    S3Uri = s3OutputFolderName
                },
                SourceLanguageCode = sourceLanguageCode,
                SubmittedTime = DateTime.Now,
                TargetLanguageCodes = targetLanguageCodes,
                TerminologyNames = terminologyNames
            };
        }

        public async Task<AWSResult<string>> TranslateText(string sourceLanguageCode, string targetLanguageCode, string text, List<string> terminologyNames)
        {
            await Task.Delay(1000);

            var result = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
            return await Task.FromResult(new AWSResult<string>(result, null));
        }

        public async Task<AWSResult<TranslateJobStatus>> StartTextTranslationJob(string jobName, RegionEndpoint region, string dataAccessRoleArn, string sourceLanguageCode, List<string> targetLanguageCodes, string s3InputFolderName, string s3OutputFolderName, List<string> terminologyNames)
        {
            await Task.Delay(1000);

            //if (translationTaskDictionary.ContainsKey(jobName))
            //{
            //    return await Task.FromResult(new AWSResult<TranslateJobStatus>(null,
            //        new AWSException(
            //            nameof(StartTextTranslationJob),
            //            "Conflicting parameters e.g. a jobname is already in use.",
            //            new ConflictException("Try again with a different job name.")
            //        )
            //    ));
            //}

            // TODO for each item in s3InputFolderName...

            var jobId = Guid.NewGuid().ToString();
            var job = CreateTranslationJob(jobId, jobName, dataAccessRoleArn, s3InputFolderName, s3OutputFolderName, sourceLanguageCode, targetLanguageCodes, terminologyNames);
            translationTaskDictionary.AddOrUpdate(jobId, job, (_, job) => job);     // asserting that the key will never be reused/updated

            // after a delay, set the job to complete
            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(10000);

                // add a new 'file' to MockS3, and set the task as complete
                var mockS3 = awsInterface.S3 as MockS3;
                var key = $"TranslateTaskOutput-{Guid.NewGuid()}.json";
                var bucketName = "tator";       // ??? should be parsed from input file url
                var newKey = mockS3.AddBucketItem(bucketName, key, "application/json", "json");    // key may be modified if it already exists

                var job = translationTaskDictionary[jobName];
                job.JobStatus = JobStatus.COMPLETED;
                job.EndTime = DateTime.Now;
            });

            var result = new TranslateJobStatus(job.JobId, job.JobStatus);
            return await Task.FromResult(new AWSResult<TranslateJobStatus>(result, null));
        }

        public async Task<AWSResult<TranslateJobStatus>> StopTextTranslationJob(string jobId, RegionEndpoint region)
        {
            await Task.Delay(1000);

            TranslateJobStatus SetJobToStopped(string jobId)
            {
                // asserting that key will always exist (addFactory not relevant)
                var updatedTask = translationTaskDictionary.AddOrUpdate(jobId, _ => null, (_, job) => {
                    job.JobStatus = JobStatus.STOPPED;
                    return job;
                });

                return new TranslateJobStatus(updatedTask.JobId, updatedTask.JobStatus);
            }

            var result = translationTaskDictionary.ContainsKey(jobId) switch
            {
                true => new AWSResult<TranslateJobStatus>(SetJobToStopped(jobId), null),
                false => new AWSResult<TranslateJobStatus>(null, new AWSException("Mock StopTextTranslationJob", $"The job {jobId} does not exist", new KeyNotFoundException("Key not found")))
            };

            return await Task.FromResult(result);
        }

        public async Task<AWSResult<List<TextTranslationJobProperties>>> ListTextTranslationJobs(RegionEndpoint region)
        {
            await Task.Delay(1000);

            var jobProperties = new List<TextTranslationJobProperties>(translationTaskDictionary.Values);
            return await Task.FromResult(new AWSResult<List<TextTranslationJobProperties>>(jobProperties, null));
        }

        public async Task<AWSResult<List<TerminologyProperties>>> ListTerminologies()
        {
            await Task.Delay(1000);

            var terminologies = new List<TerminologyProperties>(terminologyDictionary.Values);
            return await Task.FromResult(new AWSResult<List<TerminologyProperties>>(terminologies, null));
        }
    }
}
