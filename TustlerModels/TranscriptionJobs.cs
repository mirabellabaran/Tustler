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

        public async Task<bool> AddNewTask(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            var result = await awsInterface.Transcribe.StartTranscriptionJob(jobName, bucketName, s3MediaKey, languageCode, vocabularyName).ConfigureAwait(true);
            return ProcessNewTranslationJob(notifications, result);
        }

        public async Task ListTasks(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            var translationJobs = await awsInterface.Transcribe.ListTranscriptionJobs().ConfigureAwait(true);
            await ProcessExistingTranslationJobsAsync(awsInterface, notifications, translationJobs);
        }

        private bool ProcessNewTranslationJob(NotificationsList notifications, AWSResult<Amazon.TranscribeService.Model.TranscriptionJob> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return false;
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
                    OutputURI = job.Transcript?.TranscriptFileUri       // Always null: Transcribe sets the Transcript property to null when job starts. Use GetTranscriptionJob().
                });

                return true;
            }
        }

        private async Task ProcessExistingTranslationJobsAsync(AmazonWebServiceInterface awsInterface, NotificationsList notifications, AWSResult<List<TranscriptionJobSummary>> transcriptionJobs)
        {
            if (transcriptionJobs.IsError)
            {
                notifications.HandleError(transcriptionJobs);
            }
            else
            {
                var jobLookup = new Dictionary<string, TranscriptionJob>(this.TranscriptionJobs.Select(job => KeyValuePair.Create(job.TranscriptionJobName, job)));

                var jobs = transcriptionJobs.Result;
                if (jobs.Count > 0)
                {
                    // patch the existing jobs with any updates from the transcription summaries
                    foreach (var job in jobs)
                    {
                        if (jobLookup.ContainsKey(job.TranscriptionJobName))
                        {
                            var existingJob = jobLookup[job.TranscriptionJobName];

                            // report any status changes: if a job is complete but was previously incomplete then notify
                            if ((string.Compare(job.TranscriptionJobStatus, TranscriptionJobStatus.COMPLETED, StringComparison.InvariantCulture) == 0)
                            && (string.Compare(existingJob.TranscriptionJobStatus, TranscriptionJobStatus.COMPLETED, StringComparison.InvariantCulture) != 0))
                            {
                                // Transcribe require an explicit lookup to retrieve the OutputURI
                                var result = await awsInterface.Transcribe.GetTranscriptionJob(job.TranscriptionJobName).ConfigureAwait(true);
                                if (result.IsError)
                                {
                                    notifications.HandleError(result);
                                }
                                else
                                {
                                    var jobDetails = result.Result;
                                    existingJob.OutputURI = jobDetails.Transcript?.TranscriptFileUri;

                                    notifications.ShowMessage("Transcription job completed", $"The output transcript can be found at {existingJob.OutputURI}");
                                }
                            }

                            // apply patches
                            existingJob.TranscriptionJobStatus = job.TranscriptionJobStatus;
                            existingJob.CompletionTime = job.CompletionTime;
                        }
                        else
                        {
                            // Refresh has picked up an unknown job (ie not added during the current session)
                            // Transcribe maintains job state across sessions; use DeleteTranscriptionJob to remove this state.
                            var newJob = new TranscriptionJob
                            {
                                TranscriptionJobName = job.TranscriptionJobName,
                                TranscriptionJobStatus = job.TranscriptionJobStatus,
                                CreationTime = job.CreationTime,
                                StartTime = job.StartTime,
                                CompletionTime = job.CompletionTime,
                                LanguageCode = job.LanguageCode,
                                FailureReason = job.FailureReason,
                                MediaFormat = null,
                                MediaSampleRateHertz = 0,
                                OutputURI = ""
                            };

                            jobLookup.Add(job.TranscriptionJobName, newJob);
                        }
                    }
                }

                // re-add all items (if any)
                this.TranscriptionJobs.Clear();
                foreach (var kvp in jobLookup)
                {
                    this.TranscriptionJobs.Add(kvp.Value);
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
