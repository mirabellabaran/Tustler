#nullable enable
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels
{
    public class TranscriptionJobsViewModel
    {
        public ObservableCollection<TranscriptionJob> TranscriptionJobs
        {
            get;
            private set;
        }

        public TranscriptionJobsViewModel()
        {
            this.TranscriptionJobs = new ObservableCollection<TranscriptionJob>();
        }

        public async Task<string?> AddNewTask(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            var result = await awsInterface.Transcribe.StartTranscriptionJob(jobName, bucketName, s3MediaKey, languageCode, vocabularyName).ConfigureAwait(true);
            return ProcessPollyNewTranslationJob(notifications, result);
        }

        public async Task ListTasks(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            var translationJobs = await awsInterface.Transcribe.ListTranscriptionJobs().ConfigureAwait(true);
            ProcessTranslationJobs(notifications, translationJobs);
        }

        private string? ProcessPollyNewTranslationJob(NotificationsList notifications, AWSResult<Amazon.TranscribeService.Model.TranscriptionJob> result)
        {
            string? s3OutputKey = null;

            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var job = result.Result;
                this.TranscriptionJobs.Add(new TranscriptionJob
                {
                    TranscriptionJobName = job.TranscriptionJobName,
                    TranscriptionJobStatus = job.TranscriptionJobStatus,
                    CreationTime = job.CreationTime,
                    StartTime = job.StartTime,
                    CompletionTime = job.CompletionTime,
                    LanguageCode = job.LanguageCode,
                    FailureReason = job.FailureReason,
                    MediaFormat = job.MediaFormat,
                    MediaSampleRateHertz = job.MediaSampleRateHertz,
                    OutputURI = job.Transcript?.TranscriptFileUri
                });

                s3OutputKey = Path.GetFileName(job.Transcript?.TranscriptFileUri);
            }

            return s3OutputKey;
        }

        private void ProcessTranslationJobs(NotificationsList notifications, AWSResult<List<TranscriptionJobSummary>> transcriptionJobs)
        {
            if (transcriptionJobs.IsError)
            {
                notifications.HandleError(transcriptionJobs);
            }
            else
            {
                var jobs = transcriptionJobs.Result;
                if (jobs.Count > 0)
                {
                    //var transcriptionJobModelItems = from job in jobs
                    //                                 select new TranscriptionJob
                    //                                 {
                    //                                     TranscriptionJobName = job.TranscriptionJobName,
                    //                                     TranscriptionJobStatus = job.TranscriptionJobStatus,
                    //                                     CreationTime = job.CreationTime,
                    //                                     StartTime = job.StartTime,
                    //                                     CompletionTime = job.CompletionTime,
                    //                                     LanguageCode = job.LanguageCode,
                    //                                     FailureReason = job.FailureReason,
                    //                                     MediaFormat = null,
                    //                                     MediaSampleRateHertz = 0,
                    //                                 };

                    // patch the existing jobs with any updates from the transcription summaries
                    var statusLookup = new Dictionary<string, TranscriptionJob>(this.TranscriptionJobs.Select(job => KeyValuePair.Create(job.TranscriptionJobName, job)));
                    foreach (var job in jobs)
                    {
                        if (statusLookup.ContainsKey(job.TranscriptionJobName))
                        {
                            var existingJob = statusLookup[job.TranscriptionJobName];

                            // report any status changes
                            // if a job is complete but was previously incomplete then notify
                            if ((string.Compare(job.TranscriptionJobStatus, TranscriptionJobStatus.COMPLETED, StringComparison.InvariantCulture) == 0)
                            && (string.Compare(existingJob.TranscriptionJobStatus, TranscriptionJobStatus.COMPLETED, StringComparison.InvariantCulture) != 0))
                            {
                                notifications.ShowMessage("Transcription job completed", $"The output transcript can be found at {existingJob.OutputURI}");
                            }

                            // apply patches
                            existingJob.TranscriptionJobStatus = job.TranscriptionJobStatus;
                            existingJob.CompletionTime = job.CompletionTime;
                        }
                        else
                        {
                            throw new KeyNotFoundException("Unexpected job name in job listing");   // ??? can Transcribe maintain job state across sessions ???
                        }
                    }

                    //this.TranscriptionJobs.Clear();
                    //foreach (var item in transcriptionJobModelItems)
                    //{
                    //    this.TranscriptionJobs.Add(item);
                    //}
                }
            }
        }
    }

    public class TranscriptionJob
    {
        public string TranscriptionJobName { get; internal set; }
        public string TranscriptionJobStatus { get; internal set; }
        public DateTime CreationTime { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public DateTime CompletionTime { get; internal set; }
        public string LanguageCode { get; internal set; }
        public string FailureReason { get; internal set; }
        public string? MediaFormat { get; internal set; }
        public int? MediaSampleRateHertz { get; internal set; }
        public string? OutputURI { get; internal set; }
    }
}
