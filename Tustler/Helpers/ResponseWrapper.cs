using CloudWeaver.Types;
using System.Collections.Generic;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    public interface IOwnerType
    {
        public abstract IEnumerable<TaskFunctionSpecifier> TaskFunctions { get; }
    }

    public class ResponseWrapper
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
    }
}
