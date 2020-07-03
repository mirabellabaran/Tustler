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
        /// <summary>
        /// Specifies whether stack items such as tasks are independant or sequentially dependant
        /// </summary>
        public enum ItemOrdering
        {
            Independant,    // execution of each item is independant of that of its peers
            Sequential      // items are executed sequentially with sequential dependancies
        }

        private readonly Stack<T> _stack;
        private readonly ImmutableArray<T> _array;
        private readonly ItemOrdering _ordering;

        public RetainingStack(IEnumerable<T> items, ItemOrdering ordering)
        {
            if (items is null) throw new ArgumentNullException(nameof(items), "Expecting a non-null value");

            _array = items.ToImmutableArray();
            _stack = new Stack<T>(items.Reverse());     // reversed so that calling Pop() removes items in _array order

            _ordering = ordering;
        }

        public ItemOrdering Ordering
        {
            get
            {
                return _ordering;
            }
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
