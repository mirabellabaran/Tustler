using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TustlerServicesLib
{
    /// <summary>
    /// A stack-like object that supports Stack semantics but retains all data
    /// </summary>
    public class RetainingStack<T>
    {
        private readonly Stack<T> _stack;
        private readonly ImmutableArray<T> _array;

        public RetainingStack(IEnumerable<T> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items), "Expecting a non-null value");

            _array = items.ToImmutableArray();
            _stack = new Stack<T>(items);
        }

        public int Count
        {
            get
            {
                return _stack.Count;
            }
        }

        public T Pop()
        {
            return _stack.Pop();
        }

        /// <summary>
        /// Refills the stack with the same items used at construction time
        /// </summary>
        public void Reset()
        {
            foreach (var item in _array)
            {
                _stack.Push(item);
            }
        }

        public override string ToString()
        {
            return $"RetainingStack of {typeof(T).Name}: store={_array.Count()}; stack={_stack.Count}";
        }
    }
}
