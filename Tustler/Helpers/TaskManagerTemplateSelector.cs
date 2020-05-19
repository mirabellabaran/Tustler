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
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null && item is TaskResponse)
            {
                TaskResponse response = item as TaskResponse;

                DataTemplate GetErrorTemplate(TaskResponse.Notification note)
                {
                    return note.Item switch
                    {
                        ApplicationErrorInfo _ => element.FindResource("ApplicationErrorInfoTemplate") as DataTemplate,
                        ApplicationMessageInfo _ => element.FindResource("ApplicationMessageInfoTemplate") as DataTemplate,
                        _ => throw new NotImplementedException($"TaskManagerTemplateSelector: SelectTemplate(): Unexpected Application Error type."),
                    };
                }

                var template = response switch
                {
                    TaskResponse.Notification note => GetErrorTemplate(note),
                    TaskResponse.TaskInfo _ => element.FindResource("TaskInfoTemplate") as DataTemplate,
                    TaskResponse.TaskComplete _ => element.FindResource("TaskCompleteTemplate") as DataTemplate,
                    TaskResponse.TaskSelect _ => element.FindResource("TaskSelectTemplate") as DataTemplate,
                    TaskResponse.Bucket _ => element.FindResource("BucketTemplate") as DataTemplate,
                    TaskResponse.BucketItem _ => element.FindResource("BucketItemTemplate") as DataTemplate,
                    TaskResponse.BucketsModel _ => element.FindResource("BucketsModelTemplate") as DataTemplate,
                    TaskResponse.BucketItemsModel _ => element.FindResource("BucketItemsModelTemplate") as DataTemplate,
                    TaskResponse.TranscriptionJobsModel _ => element.FindResource("TranscriptionJobsModelTemplate") as DataTemplate,
                    _ => null
                };

                return template;
            }

            return null;
        }
    }

    public class TaskManagerChildDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item != null)
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