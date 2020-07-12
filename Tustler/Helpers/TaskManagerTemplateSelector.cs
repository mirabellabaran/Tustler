#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TustlerFSharpPlatform;
using TustlerModels;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public class TaskManagerDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is TaskResponse response)
            {
                DataTemplate? GetErrorTemplate(TaskResponse.Notification note)
                {
                    return note.Item switch
                    {
                        ApplicationErrorInfo _ => element.FindResource("ApplicationErrorInfoTemplate") as DataTemplate,
                        ApplicationMessageInfo _ => element.FindResource("ApplicationMessageInfoTemplate") as DataTemplate,
                        _ => throw new NotImplementedException($"TaskManagerTemplateSelector: SelectTemplate(): Unexpected Application Error type."),
                    };
                }

                DataTemplate? GetRequestTemplate(TaskResponse response)
                {
                    return response.Tag switch
                    {
                        TaskResponse.Tags.RequestFileMediaReference => element.FindResource("FileMediaReferenceRequestTemplate") as DataTemplate,
                        TaskResponse.Tags.RequestS3MediaReference => element.FindResource("S3MediaReferenceRequestTemplate") as DataTemplate,
                        TaskResponse.Tags.RequestBucket => element.FindResource("BucketRequestTemplate") as DataTemplate,
                        TaskResponse.Tags.RequestTranscriptionLanguageCode => element.FindResource("TranscriptionLanguageCodeRequestTemplate") as DataTemplate,
                        TaskResponse.Tags.RequestTranslationLanguageCode => element.FindResource("TranslationLanguageCodeRequestTemplate") as DataTemplate,
                        TaskResponse.Tags.RequestVocabularyName => element.FindResource("VocabularyNameRequestTemplate") as DataTemplate,
                        _ => null
                    };
                }

                // Note that ShowTranscriptionJobsSummary and SetTranscriptionJobsModel share a template as both display the same data type
                // (the latter also sets an argument on the events stack)
                var template = response switch
                {
                    TaskResponse.Notification note => GetErrorTemplate(note),
                    TaskResponse.TaskInfo _ => element.FindResource("TaskInfoTemplate") as DataTemplate,
                    TaskResponse.TaskComplete _ => element.FindResource("TaskCompleteTemplate") as DataTemplate,
                    TaskResponse.TaskPrompt _ => element.FindResource("TaskPromptTemplate") as DataTemplate,
                    TaskResponse.TaskSelect _ => element.FindResource("TaskSelectTemplate") as DataTemplate,
                    TaskResponse.TaskMultiSelect _ => element.FindResource("TaskMultiSelectTemplate") as DataTemplate,
                    TaskResponse.TaskSequence _ => element.FindResource("TaskSequenceTemplate") as DataTemplate,

                    TaskResponse.ShowTranscriptionJobsSummary _ => element.FindResource("TranscriptionJobsModelTemplate") as DataTemplate,

                    TaskResponse.SetBucket _ => element.FindResource("BucketTemplate") as DataTemplate,
                    TaskResponse.SetBucketsModel _ => element.FindResource("BucketsModelTemplate") as DataTemplate,
                    TaskResponse.SetBucketItemsModel _ => element.FindResource("BucketItemsModelTemplate") as DataTemplate,
                    TaskResponse.SetTranscriptionJobsModel _ => element.FindResource("TranscriptionJobsModelTemplate") as DataTemplate,
                    TaskResponse.SetTranscriptionJobName _ => element.FindResource("TranscriptionJobNameTemplate") as DataTemplate,
                    TaskResponse.SetFileUpload _ => element.FindResource("FileUploadTemplate") as DataTemplate,
                    _ => GetRequestTemplate(response)
                };

                return template;
            }

            return null;
        }
    }

    public class TaskManagerChildDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is object)
            {
                var template = item switch
                {
                    Bucket _ => element.FindResource("BucketTemplate") as DataTemplate,
                    BucketItem _ => element.FindResource("BucketItemTemplate") as DataTemplate,
                    _ => null
                };

                return template;
            }

            return null;
        }
    }
}