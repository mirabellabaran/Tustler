using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace TustlerServicesLib
{
    public class InfiniteList<T> : IEnumerable<T>
    {
        private ImmutableArray<T> list;

        /// <summary>
        /// Create an InfiniteList with the specified default value
        /// </summary>
        /// <remarks>The default value is returned when the list is iterated past the end of any set values</remarks>
        /// <param name="defaultValue"></param>
        public InfiniteList(T defaultValue)
        {
            list = ImmutableArray<T>.Empty;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Get the default value for the list
        /// </summary>
        public T DefaultValue
        {
            get;
            internal set;
        }

        /// <summary>
        /// Get the length of the list
        /// </summary>
        /// <remarks>Will always return MaxInteger</remarks>
        public int Count
        {
            get
            {
                return int.MaxValue;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        /// <summary>
        /// Clear all items from the list
        /// </summary>
        public void Clear()
        {
            list = list.Clear();
        }

        /// <summary>
        /// Add an item to the end of the list
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            list = list.Add(item);
        }

        /// <summary>
        /// Retrieve and remove the first element in the underlying list
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            if (list.Length > 0)
            {
                var firstItem = list[0];
                list = list.RemoveAt(0);
                return firstItem;
            }
            else
            {
                return DefaultValue;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        private IEnumerator<T> GetEnumeratorImpl()
        {
            foreach (var item in list)
            {
                yield return item;
            }

            while (true)
            {
                yield return DefaultValue;
            }
        }
    }
}
