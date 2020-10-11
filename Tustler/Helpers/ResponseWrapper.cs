using CloudWeaver.Types;
using System;
using System.Collections.Generic;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public interface IOwnerType
    {
        public abstract IEnumerable<TaskFunctionSpecifier> TaskFunctions { get; }
    }

    public interface IResponseWrapper
    {
        public IOwnerType Owner { get; }
        public object Item { get; }             // the wrapped item inside the TaskResponse (support for XAML binding)
    }

    public class ResponseWrapper: IResponseWrapper
    {
        public ResponseWrapper(IOwnerType owner, TaskResponse response)
        {
            Owner = owner;
            this.TaskResponse = response;
        }

        public IOwnerType Owner
        {
            get;
        }

        public TaskResponse TaskResponse
        {
            get;
        }

        public object Item
        {
            get
            {
                return this.TaskResponse switch
                {
                    TaskResponse.TaskDescription response => response.Item,
                    TaskResponse.TaskInfo response => response.Item,
                    TaskResponse.TaskComplete response => new Tuple<string, DateTime>(response.Item1, response.Item2),
                    TaskResponse.TaskPrompt response => response.Item,
                    TaskResponse.TaskSelect response => response.Item,
                    TaskResponse.TaskMultiSelect response => response.Item,
                    TaskResponse.TaskSequence response => response.Item,
                    TaskResponse.TaskContinue response => response.Item,
                    TaskResponse.TaskSaveEvents response => response.Item,
                    TaskResponse.TaskConvertToBinary response => response.Item,
                    TaskResponse.TaskConvertToJson response => response.Item,
                    TaskResponse.Notification response => response.Item,
                    TaskResponse.BeginLoopSequence response => (response.Item1, response.Item2),
                    TaskResponse.ShowValue response => response.Item,
                    TaskResponse.SetArgument response => response.Item,
                    TaskResponse.RequestArgument response => response.Item,

                    _ => throw new System.InvalidOperationException(),      // e.g. TaskResponse.ChooseTask which has no Item
                };
            }
        }
    }

    public class DescriptionWrapper : IResponseWrapper
    {
        public DescriptionWrapper(IOwnerType owner, IEnumerable<string> descriptions)
        {
            Owner = owner;
            Descriptions = descriptions;
        }

        public IOwnerType Owner { get; }

        public IEnumerable<string> Descriptions { get; }

        public object Item => Descriptions;
    }
}
