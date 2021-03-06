using Amazon;
using Amazon.Translate.Model;
using CloudWeaver.Foundation.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerAWSLib;
using TustlerInterfaces;

namespace TustlerModels
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

        public async Task AddNewTask(AmazonWebServiceInterface awsInterface, NotificationsList notifications, string jobName, RegionEndpoint region, string dataAccessRoleArn, string sourceLanguageCode, List<string> targetLanguageCodes, string s3InputFolderName, string s3OutputFolderName, List<string> terminologyNames)
        {
            var result = await awsInterface.Translate.StartTextTranslationJob(jobName, region, dataAccessRoleArn, sourceLanguageCode, targetLanguageCodes, s3InputFolderName, s3OutputFolderName, terminologyNames).ConfigureAwait(true);
            ProcessPollyNewTranslationJob(notifications, result);
        }

        public async Task ListTasks(AmazonWebServiceInterface awsInterface, NotificationsList notifications, RegionEndpoint region)
        {
            if (NeedsRefresh)
            {
                var translationJobs = await awsInterface.Translate.ListTextTranslationJobs(region).ConfigureAwait(true);
                ProcessTranslationJobs(notifications, translationJobs);
            }
        }

        private void ProcessPollyNewTranslationJob(NotificationsList notifications, AWSResult<TranslateJobStatus> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var jobStatus = result.Result;
                this.TranslationJobs.Add(new TranslationJob
                {
                    JobId = jobStatus.JobId,
                    JobStatus = jobStatus.JobStatus,
                    JobDetail = null
                });
            }
        }

        private void ProcessTranslationJobs(NotificationsList errorList, AWSResult<List<TextTranslationJobProperties>> translationJobs)
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
                                                       JobId = job.JobId,
                                                       JobStatus = job.JobStatus,
                                                       JobDetail = new JobProperties
                                                       {
                                                           JobName = job.JobName,
                                                           SubmittedTime = job.SubmittedTime,
                                                           EndTime = job.EndTime,
                                                           InputDocumentsCount = job.JobDetails.InputDocumentsCount,
                                                           TranslatedDocumentsCount = job.JobDetails.TranslatedDocumentsCount,
                                                           DocumentsWithErrorsCount = job.JobDetails.DocumentsWithErrorsCount,
                                                           Message = job.Message,
                                                           OutputS3Folder = job.OutputDataConfig.S3Uri
                                                       }
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
        public string JobId { get; internal set; }
        public string JobStatus { get; internal set; }
        public JobProperties JobDetail { get; internal set; }
    }

    public class JobProperties
    {
        public string JobName { get; internal set; }
        public DateTime SubmittedTime { get; internal set; }
        public DateTime EndTime { get; internal set; }
        public int InputDocumentsCount { get; internal set; }
        public int TranslatedDocumentsCount { get; internal set; }
        public int DocumentsWithErrorsCount { get; internal set; }
        public string Message { get; internal set; }
        public string OutputS3Folder { get; internal set; }
    }
}
