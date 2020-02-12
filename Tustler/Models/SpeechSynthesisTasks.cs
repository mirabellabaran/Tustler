using Amazon.Polly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Tustler.Models
{
    public class SpeechSynthesisTasksViewModel
    {
        public ObservableCollection<SpeechSynthesisTask> SpeechSynthesisTasks
        {
            get;
            private set;
        }

        public bool NeedsRefresh
        {
            get;
            set;
        }

        public SpeechSynthesisTasksViewModel()
        {
            this.SpeechSynthesisTasks = new ObservableCollection<SpeechSynthesisTask>();
            this.NeedsRefresh = true;
        }

        public async Task Refresh(NotificationsList notifications, string bucketName, string key, string arn, string filePath, bool useNeural, string voiceId = "Joanna")
        {
            if (NeedsRefresh)
            {
                var engine = useNeural ? Engine.Neural : Engine.Standard;
                var result = await TustlerAWSLib.Polly.StartSpeechSynthesisTaskFromFile(bucketName, key, arn, filePath, engine, voiceId).ConfigureAwait(true);
                ProcessPollyNewSpeechSynthesisTask(notifications, result);
            }
        }

        private void ProcessPollyNewSpeechSynthesisTask(NotificationsList notifications, TustlerAWSLib.AWSResult<Amazon.Polly.Model.SynthesisTask> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var task = result.Result;
                this.SpeechSynthesisTasks.Add(new SpeechSynthesisTask
                {
                    TaskId = task.TaskId,
                    TaskStatus = task.TaskStatus,
                    TaskStatusReason = task.TaskStatusReason,
                    CreationTime = task.CreationTime,
                    Engine = task.Engine,
                    LanguageCode = task.LanguageCode,
                    LexiconNames = task.LexiconNames,
                    OutputFormat = task.OutputFormat,
                    OutputUri = task.OutputUri,
                    SampleRate = task.SampleRate,
                    SnsTopicArn = task.SnsTopicArn,
                    VoiceId = task.VoiceId
                });

                NeedsRefresh = false;
            }
        }

    }

    public class SpeechSynthesisTask
    {
        public string TaskId
        {
            get;
            internal set;
        }

        public string TaskStatus
        {
            get;
            internal set;
        }

        public string TaskStatusReason
        {
            get;
            internal set;
        }

        public DateTime CreationTime
        {
            get;
            internal set;
        }

        public Engine Engine
        {
            get;
            internal set;
        }

        public string LanguageCode
        {
            get;
            internal set;
        }

        public List<string> LexiconNames
        {
            get;
            internal set;
        }

        public string OutputFormat
        {
            get;
            internal set;
        }

        public string OutputUri
        {
            get;
            internal set;
        }

        public string SampleRate
        {
            get;
            internal set;
        }

        public string SnsTopicArn
        {
            get;
            internal set;
        }

        public string VoiceId
        {
            get;
            internal set;
        }
    }
}
