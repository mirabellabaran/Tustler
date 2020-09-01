using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TustlerServicesLib
{
    // a type whose internal items can be consumed until none are left
    public interface IConsumable
    {
        public int Count { get; }
    }

    /// <summary>
    /// A stack-like object that supports Stack semantics but retains all data
    /// </summary>
    /// <remarks>A retaining stack can be consumed but the original contents are always retrievable</remarks>
    public class RetainingStack<T> : IConsumable, IEnumerable<T>
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

        public RetainingStack(IEnumerable<T> items) : this(items, ItemOrdering.Sequential)
        {
        }

        public RetainingStack(IEnumerable<T> items, ItemOrdering ordering)
        {
            if (items is null) throw new ArgumentNullException(nameof(items), "Expecting a non-null value");

            _array = items.ToImmutableArray();
            _stack = new Stack<T>(items.Reverse());     // reversed so that calling Pop() removes items in _array order

            Ordering = ordering;
        }

        public ItemOrdering Ordering { get; }

        public int Count
        {
            get
            {
                return _stack.Count;
            }
        }

        // Get the current item (head of stack) without consuming it
        public T Current
        {
            get
            {
                return _stack.Peek();
            }
        }

        // Consume the current item (head of stack)
        public T Consume()
        {
            return _stack.Pop();
        }

        public override string ToString()
        {
            return $"RetainingStack of {typeof(T).Name}: store={_array.Count()}; stack={_stack.Count}";
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)_array).GetEnumerator();
        }
    }
}
