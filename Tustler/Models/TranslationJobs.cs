using Amazon.Translate.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tustler.Models
{
    public class TranslationJobsViewModel
    {
        public ObservableCollection<TranslationJob> TranslationJobs
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public TranslationJobsViewModel()
        {
            this.TranslationJobs = new ObservableCollection<TranslationJob>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(NotificationsList notifications)
        {
            if (NeedsRefresh)
            {
                var translationJobs = await TustlerAWSLib.Translate.ListTextTranslationJobs().ConfigureAwait(true);
                ProcessTranslationJobs(notifications, translationJobs);
            }
        }

        private void ProcessTranslationJobs(NotificationsList errorList, TustlerAWSLib.AWSResult<List<TextTranslationJobProperties>> translationJobs)
        {
            if (translationJobs.IsError)
            {
                errorList.HandleError(translationJobs);
            }
            else
            {
                var jobs = translationJobs.Result;
                if (jobs.Count > 0)
                {
                    var translationJobModelItems = from job in jobs
                                                   select new TranslationJob
                                                   {
                                                       SubmittedTime = job.SubmittedTime,
                                                       EndTime = job.EndTime,
                                                       JobId = job.JobId,
                                                       JobName = job.JobName,
                                                       JobStatus = job.JobStatus,
                                                       InputDocumentsCount = job.JobDetails.InputDocumentsCount,
                                                       TranslatedDocumentsCount = job.JobDetails.TranslatedDocumentsCount,
                                                       DocumentsWithErrorsCount = job.JobDetails.DocumentsWithErrorsCount,
                                                       Message = job.Message,
                                                       OutputS3Folder = job.OutputDataConfig.S3Uri
                                                   };

                    this.TranslationJobs.Clear();
                    foreach (var item in translationJobModelItems)
                    {
                        this.TranslationJobs.Add(item);
                    }
                }

                NeedsRefresh = false;
            }
        }
    }

    public class TranslationJob
    {
        public DateTime SubmittedTime { get; internal set; }
        public DateTime EndTime { get; internal set; }
        public string JobId { get; internal set; }
        public string JobName { get; internal set; }
        public string JobStatus { get; internal set; }
        public int InputDocumentsCount { get; internal set; }
        public int TranslatedDocumentsCount { get; internal set; }
        public int DocumentsWithErrorsCount { get; internal set; }
        public string Message { get; internal set; }
        public string OutputS3Folder { get; internal set; }
    }
}
