#nullable enable
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
{
    /// <summary>
    /// Represents the current state of all Transcription jobs (running or completed)
    /// </summary>
    /// <remarks>
    /// Data from Amazon is either Amazon.TranscribeService.Model.TranscriptionJob or Amazon.TranscribeService.Model.TranscriptionJobSummary
    /// The local model (TustlerModels.TranscriptionJob) pulls attributes from both of these Amazon models where they have been set
    /// </remarks>
    public class TranscriptionJobsViewModel
    {
        private readonly Dictionary<string, TranscriptionJob> jobLookup;            // the definitive source of knowledge

        public ObservableCollection<TranscriptionJob> TranscriptionJobs
        {
            get;
        }

        public TranscriptionJob this[string jobName]
        {
            get
            {
                return jobLookup[jobName];
            }
        }

        public TranscriptionJobsViewModel()
        {
            this.jobLookup = new Dictionary<string, TranscriptionJob>();
            this.TranscriptionJobs = new ObservableCollection<TranscriptionJob>();
        }

        public async Task<bool> AddNewTask(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            var transcriptionJob = await awsInterface.Transcribe.StartTranscriptionJob(jobName, bucketName, s3MediaKey, languageCode, vocabularyName).ConfigureAwait(true);
            return ProcessTranscriptionJobDetails(notifications, transcriptionJob);
        }

        public async Task ListTasks(AmazonWebServiceInterface awsInterface, NotificationsList notifications)
        {
            var transcriptionJobs = await awsInterface.Transcribe.ListTranscriptionJobs().ConfigureAwait(true);
            await ProcessTranscriptionJobSummaries(awsInterface, notifications, transcriptionJobs);
        }

        public async Task<bool> GetTaskByName(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName)
        {
            var transcriptionJob = await awsInterface.Transcribe.GetTranscriptionJob(jobName).ConfigureAwait(true);
            return ProcessTranscriptionJobDetails(notifications, transcriptionJob);
        }

        public async Task<bool> DeleteTaskByName(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName)
        {
            var result = await awsInterface.Transcribe.DeleteTranscriptionJob(jobName).ConfigureAwait(true);
            if (result.IsError)
            {
                notifications.HandleError(result);
                return false;
            }
            else
            {
                return result.Result;
            }
        }

        private bool ProcessTranscriptionJobDetails(NotificationsList notifications, AWSResult<Amazon.TranscribeService.Model.TranscriptionJob> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return false;
            }
            else
            {
                var jobUpdate = result.Result;
                var job = new TranscriptionJob
                {
                    TranscriptionJobName = jobUpdate.TranscriptionJobName,
                    TranscriptionJobStatus = jobUpdate.TranscriptionJobStatus,
                    CreationTime = jobUpdate.CreationTime,
                    StartTime = jobUpdate.StartTime,
                    CompletionTime = jobUpdate.CompletionTime,
                    LanguageCode = jobUpdate.LanguageCode,
                    FailureReason = jobUpdate.FailureReason,
                    MediaFormat = jobUpdate.MediaFormat,
                    MediaSampleRateHertz = jobUpdate.MediaSampleRateHertz,
                    OutputURI = jobUpdate.Transcript?.TranscriptFileUri       // Note that Transcribe sets the Transcript property to null when the job first starts
                };

                if (jobLookup.ContainsKey(job.TranscriptionJobName))
                {
                    // processing GetTaskByName: update the existing task
                    this.jobLookup[job.TranscriptionJobName] = job;
                }
                else
                {
                    // processing AddNewTask: add a new task
                    this.jobLookup.Add(job.TranscriptionJobName, job);
                }

                // re-add all items to the consumed collection
                this.TranscriptionJobs.Clear();
                foreach (var kvp in jobLookup)
                {
                    this.TranscriptionJobs.Add(kvp.Value);
                }

                return true;
            }
        }

        private async Task ProcessTranscriptionJobSummaries(AmazonWebServiceInterface awsInterface, NotificationsList notifications, AWSResult<List<TranscriptionJobSummary>> transcriptionJobs)
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
                                // Transcribe requires an explicit lookup to retrieve the OutputURI
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

                    // re-add all items (if any)
                    this.TranscriptionJobs.Clear();
                    foreach (var kvp in jobLookup)
                    {
                        this.TranscriptionJobs.Add(kvp.Value);
                    }
                }
            }
        }
    }
#nullable disable

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
