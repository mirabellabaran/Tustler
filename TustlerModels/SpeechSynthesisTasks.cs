using Amazon.Polly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TustlerInterfaces;
using TustlerServicesLib;

namespace TustlerModels
{
    public class SpeechSynthesisTasksViewModel
    {
        public ObservableCollection<SpeechSynthesisTask> SpeechSynthesisTasks
        {
            get;
            private set;
        }

        //public bool NeedsRefresh
        //{
        //    get;
        //    set;
        //}

        public SpeechSynthesisTasksViewModel()
        {
            this.SpeechSynthesisTasks = new ObservableCollection<SpeechSynthesisTask>();
            //this.NeedsRefresh = true;
        }

        /// <summary>
        /// Create a new speech synthesis task and add to the task list
        /// </summary>
        /// <param name="notifications">A reference to the global notifications list</param>
        /// <param name="bucketName">The name of an S3 bucket to receive the synthesized output</param>
        /// <param name="key">The key prefix used to store the output within an S3 bucket</param>
        /// <param name="arn">The address (ARN) of the SNS topic used for providing status notification</param>
        /// <param name="filePath">The path to a file containing the text to convert to an audio stream</param>
        /// <param name="useNeural">If true then use the neural speech synthesis engine, otherwise use the standard engine</param>
        /// <param name="voiceId">The Id of the voice to use for synthesis</param>
        /// <returns></returns>
        public async Task<string> AddNewTask(NotificationsList notifications, string bucketName, string key, string arn, string filePath, bool useNeural, string voiceId)
        {
            var engine = useNeural ? Engine.Neural : Engine.Standard;
            var result = await TustlerAWSLib.Polly.StartSpeechSynthesisTaskFromFile(bucketName, key, arn, filePath, engine, voiceId).ConfigureAwait(true);
            return ProcessPollyNewSpeechSynthesisTask(notifications, result);
        }

        /// <summary>
        /// Fetch a list of all known speech synthesis tasks
        /// </summary>
        /// <returns></returns>
        public async Task ListTasks(NotificationsList notifications)
        {
            var result = await TustlerAWSLib.Polly.ListSpeechSynthesisTasks().ConfigureAwait(true);
            ProcessPollySpeechSynthesisTasks(notifications, result);
        }

        private void ProcessPollySpeechSynthesisTasks(NotificationsList notifications, AWSResult<List<Amazon.Polly.Model.SynthesisTask>> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
            }
            else
            {
                var tasks = result.Result.Select(task => new SpeechSynthesisTask
                {
                    TaskId = task.TaskId,
                    TaskStatus = task.TaskStatus,
                    TaskStatusReason = task.TaskStatusReason,
                    CreationTime = task.CreationTime,
                    Engine = task.Engine,
                    LanguageCode = task.LanguageCode,
                    LexiconNames = string.Join(", ", task.LexiconNames),
                    OutputFormat = task.OutputFormat,
                    OutputUri = task.OutputUri,
                    SampleRate = task.SampleRate,
                    SnsTopicArn = task.SnsTopicArn,
                    VoiceId = task.VoiceId
                });

                this.SpeechSynthesisTasks.Clear();

                foreach (var task in tasks)
                {
                    this.SpeechSynthesisTasks.Add(task);
                }
            }
        }

        /// <summary>
        /// Process the result from adding a new task
        /// </summary>
        /// <param name="notifications">A reference to the global notifications list</param>
        /// <param name="result">The result to process</param>
        /// <returns>The task Id</returns>
        private string ProcessPollyNewSpeechSynthesisTask(NotificationsList notifications, AWSResult<Amazon.Polly.Model.SynthesisTask> result)
        {
            if (result.IsError)
            {
                notifications.HandleError(result);
                return null;
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
                    LexiconNames = string.Join(", ", task.LexiconNames),
                    OutputFormat = task.OutputFormat,
                    OutputUri = task.OutputUri,
                    SampleRate = task.SampleRate,
                    SnsTopicArn = task.SnsTopicArn,
                    VoiceId = task.VoiceId
                });

                //NeedsRefresh = false;
                return task.TaskId;
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

        public string LexiconNames
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
