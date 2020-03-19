#nullable enable
using Amazon.TranscribeService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace Tustler.Models
{
    public class TranscriptionJobsViewModel
    {
        public ObservableCollection<TranscriptionJob> TranscriptionJobs
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public TranscriptionJobsViewModel()
        {
            this.TranscriptionJobs = new ObservableCollection<TranscriptionJob>();
            this.NeedsRefresh = true;
        }

        public async Task AddNewTask(NotificationsList notifications, string jobName, string bucketName, string s3MediaKey, string languageCode, string vocabularyName)
        {
            var result = await TustlerAWSLib.Transcribe.StartTranscriptionJob(jobName, bucketName, s3MediaKey, languageCode, vocabularyName).ConfigureAwait(true);
            ProcessPollyNewTranslationJob(notifications, result);
        }

        public async Task ListTasks(NotificationsList notifications)
        {
            if (NeedsRefresh)
            {
                var translationJobs = await TustlerAWSLib.Transcribe.ListTranscriptionJobs().ConfigureAwait(true);
                ProcessTranslationJobs(notifications, translationJobs);
            }
        }

        private void ProcessPollyNewTranslationJob(NotificationsList notifications, AWSResult<Amazon.TranscribeService.Model.TranscriptionJob> result)
        {
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
                    OutputLocationType = job.Transcript?.TranscriptFileUri,
                    FailureReason = job.FailureReason,
                    MediaFormat = job.MediaFormat,
                    MediaSampleRateHertz = job.MediaSampleRateHertz
                });
            }
        }

        private void ProcessTranslationJobs(NotificationsList errorList, AWSResult<List<TranscriptionJobSummary>> transcriptionJobs)
        {
            if (transcriptionJobs.IsError)
            {
                errorList.HandleError(transcriptionJobs);
            }
            else
            {
                var jobs = transcriptionJobs.Result;
                if (jobs.Count > 0)
                {
                    var transcriptionJobModelItems = from job in jobs
                                                     select new TranscriptionJob
                                                     {
                                                         TranscriptionJobName = job.TranscriptionJobName,
                                                         TranscriptionJobStatus = job.TranscriptionJobStatus,
                                                         CreationTime = job.CreationTime,
                                                         StartTime = job.StartTime,
                                                         CompletionTime = job.CompletionTime,
                                                         LanguageCode = job.LanguageCode,
                                                         OutputLocationType = job.OutputLocationType,
                                                         FailureReason = job.FailureReason,
                                                         MediaFormat = null,
                                                         MediaSampleRateHertz = 0
                                                     };

                    this.TranscriptionJobs.Clear();
                    foreach (var item in transcriptionJobModelItems)
                    {
                        this.TranscriptionJobs.Add(item);
                    }
                }

                NeedsRefresh = false;
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
        public string OutputLocationType { get; internal set; }
        public string FailureReason { get; internal set; }
        public string? MediaFormat { get; internal set; }
        public int? MediaSampleRateHertz { get; internal set; }

    }
}
