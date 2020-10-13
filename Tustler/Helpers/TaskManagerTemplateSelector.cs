#nullable enable
using CloudWeaver.Types;
using System;
using System.Windows;
using System.Windows.Controls;
using TustlerModels;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public class TaskManagerDataTemplateSelector : DataTemplateSelector
    {
        private DataTemplate? SelectResponseWrapperTemplate(FrameworkElement element, ResponseWrapper wrapper)
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

            DataTemplate? GetRequestTemplate(IRequestIntraModule request)
            {
                return request.Identifier.AsString() switch
                {
                    "RequestFileMediaReference" => element.FindResource("FileMediaReferenceRequestTemplate") as DataTemplate,
                    "RequestS3MediaReference" => element.FindResource("S3MediaReferenceRequestTemplate") as DataTemplate,
                    "RequestBucket" => element.FindResource("BucketRequestTemplate") as DataTemplate,
                    "RequestTranscriptionLanguageCode" => element.FindResource("TranscriptionLanguageCodeRequestTemplate") as DataTemplate,
                    "RequestTranscriptionVocabularyName" => element.FindResource("TranscriptionVocabularyNameRequestTemplate") as DataTemplate,
                    "RequestTranscriptionDefaultTranscript" => element.FindResource("TranscriptionDefaultTranscriptRequestTemplate") as DataTemplate,
                    "RequestTranscriptURI" => element.FindResource("TranscriptURIRequestTemplate") as DataTemplate,
                    "RequestTranslationLanguageCode" => element.FindResource("TranslationLanguageCodeRequestTemplate") as DataTemplate,
                    "RequestTranslationTargetLanguages" => element.FindResource("TranslationTargetLanguagesRequestTemplate") as DataTemplate,
                    "RequestTranslationTerminologyNames" => element.FindResource("TranslationTerminologyNamesRequestTemplate") as DataTemplate,
                    "RequestTranslationLanguageCodeSource" => element.FindResource("TranslationLanguageCodeSourceRequestTemplate") as DataTemplate,

                    // Note that the following FilePathRequestTemplates have the same underlying user control
                    "RequestOpenLogFormatFilePath" => element.FindResource("OpenLogFormatFilePathRequestTemplate") as DataTemplate,
                    "RequestSaveLogFormatFilePath" => element.FindResource("SaveLogFormatFilePathRequestTemplate") as DataTemplate,
                    "RequestOpenJsonFilePath" => element.FindResource("OpenJsonFilePathRequestTemplate") as DataTemplate,
                    "RequestSaveJsonFilePath" => element.FindResource("SaveJsonFilePathRequestTemplate") as DataTemplate,

                    _ => null
                };
            }

            DataTemplate? GetSetArgumentTemplate(IShareIntraModule arg)
            {
                return arg.Identifier switch
                {
                    WrappedItemIdentifier id => id.Item switch
                    {
                        "SetBucket" => element.FindResource("BucketTemplate") as DataTemplate,
                        "SetBucketsModel" => element.FindResource("BucketsModelTemplate") as DataTemplate,
                        "SetS3MediaReference" => element.FindResource("S3MediaReferenceTemplate") as DataTemplate,

                        "SetTranscriptURI" => element.FindResource("TranscriptURITemplate") as DataTemplate,
                        "SetTranscriptJSON" => element.FindResource("TranscriptJSONTemplate") as DataTemplate,
                        "SetTranscriptionJobsModel" => element.FindResource("TranscriptionJobsModelTemplate") as DataTemplate,
                        "SetTranscriptionJobName" => element.FindResource("TranscriptionJobNameTemplate") as DataTemplate,
                        "SetTranscriptionDefaultTranscript" => element.FindResource("DefaultTranscriptTemplate") as DataTemplate,

                        "SetTranslationSegments" => element.FindResource("TranslationSegmentsTemplate") as DataTemplate,

                        _ => throw new ArgumentException("Unknown argument for SetArgument")
                    },
                    _ => null
                };
            }

            DataTemplate? GetShowValueTemplate(IShowValue arg)
            {
                return arg.Identifier switch
                {
                    WrappedItemIdentifier id => id.Item switch
                    {
                        "DisplayTranscriptionJobsModel" => element.FindResource("TranscriptionJobsModelTemplate") as DataTemplate,
                        "DisplayTranscriptionJob" => element.FindResource("TranscriptionJobTemplate") as DataTemplate,
                        "DisplayBucketItemsModel" => element.FindResource("BucketItemsModelTemplate") as DataTemplate,
                        _ => throw new ArgumentException("Unknown argument for ShowArgument")
                    },
                    _ => null
                };
            }

            // Note that DisplayTranscriptionJobsModel and SetTranscriptionJobsModel share a template as both display the same data type
            // (the latter also sets an argument on the events stack)
            var template = wrapper.TaskResponse switch
            {
                TaskResponse.Notification note => GetErrorTemplate(note),
                TaskResponse.TaskInfo _ => element.FindResource("TaskInfoTemplate") as DataTemplate,
                TaskResponse.TaskComplete _ => element.FindResource("TaskCompleteTemplate") as DataTemplate,
                TaskResponse.TaskPrompt _ => element.FindResource("TaskPromptTemplate") as DataTemplate,
                TaskResponse.TaskSelect _ => element.FindResource("TaskSelectTemplate") as DataTemplate,
                TaskResponse.TaskMultiSelect _ => element.FindResource("TaskMultiSelectTemplate") as DataTemplate,
                TaskResponse.TaskSequence _ => element.FindResource("TaskSequenceTemplate") as DataTemplate,

                TaskResponse.ShowValue arg => GetShowValueTemplate(arg.Item),
                TaskResponse.SetArgument arg => GetSetArgumentTemplate(arg.Item),

                TaskResponse.RequestArgument arg => GetRequestTemplate(arg.Item),

                _ => wrapper.TaskResponse.Tag switch
                {
                    TaskResponse.Tags.ChooseTask => element.FindResource("ChooseTaskTemplate") as DataTemplate,
                    _ => null,
                }
            };

            return template;
        }

        private DataTemplate? SelectDescriptionWrapperTemplate(FrameworkElement element)
        {
            return element.FindResource("DescriptionWrapperTemplate") as DataTemplate;
        }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element)
            {
                return item switch
                {
                    ResponseWrapper wrapper => SelectResponseWrapperTemplate(element, wrapper),
                    DescriptionWrapper _ => SelectDescriptionWrapperTemplate(element),
                    _ => null
                };
            }

            return null;
        }
    }

    // a template selector used by the ContentControl on the BucketItemsModelTemplate DataTemplate of TaskManager.xaml
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