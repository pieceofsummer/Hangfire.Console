using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Hangfire.Console.Utils
{
    internal class AggregateQueue<TItem, TKey> : IProducerConsumerCollection<TItem>
        where TItem : class
        where TKey : class
    {
        private readonly IProducerConsumerCollection<TItem> _queue = new ConcurrentQueue<TItem>();
        
        // tracking of similar items already in queue
        private SpinLock _lookupLock = new SpinLock();
        private readonly IDictionary<TKey, TItem> _lookup = new Dictionary<TKey, TItem>();
        private readonly Func<TItem, TKey> _keySelector = _ => default(TKey);
        private readonly Func<TKey, TItem, TItem, bool> _updateAction;
        private readonly TItem _placeholder;
        
        // I/O statistics
        private long _input = 0, _enqueued = 0, _dequeued = 0;

        public AggregateQueue()
        {
        }

        public AggregateQueue(Func<TItem, TKey> keySelector, Func<TKey, TItem, TItem, bool> updateAction, TItem placeholder)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _updateAction = updateAction ?? throw new ArgumentNullException(nameof(updateAction));
            _placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
        }
        
        #region Forwarded implementations

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        public object SyncRoot => null;

        /// <inheritdoc />
        public int Count => _queue.Count;

        /// <inheritdoc />
        public IEnumerator<TItem> GetEnumerator() => _queue.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public void CopyTo(TItem[] array, int index) => _queue.CopyTo(array, index);

        /// <inheritdoc />
        public void CopyTo(Array array, int index) => _queue.CopyTo((TItem[])array, index);

        /// <inheritdoc />
        public TItem[] ToArray() => _queue.ToArray();

        #endregion

        /// <inheritdoc />
        public bool TryAdd(TItem item)
        {
            // increment input counter
            Interlocked.Increment(ref _input);

            var itemKey = _keySelector(item);
            if (itemKey != null)
            {
                var locked = false;
                try
                {
                    _lookupLock.Enter(ref locked);

                    if (_lookup.TryGetValue(itemKey, out var prevItem))
                    {
                        // previous item with the same key is still in queue
                        var updated = _updateAction(itemKey, prevItem, item);
                        if (updated)
                        {
                            // do not add the original item, use placeholder instead
                            item = _placeholder;
                        }
                        
                        // Callback failed to update the previous item, hence the
                        // current item will be enqueued as a regular one instead.
                        // Also, to prevent duplicate key from being added to lookup:
                        itemKey = null;
                    }
                }
                finally
                {
                    if (locked)
                        _lookupLock.Exit();
                }
            }

            if (!_queue.TryAdd(item))
            {
                // decrement input counter
                Interlocked.Decrement(ref _input);
                return false;
            }
            
            if (item == _placeholder)
            {
                // pretend it wasn't here
                return true;
            }
            
            // increment enqueued counter
            Interlocked.Increment(ref _enqueued);

            if (itemKey != null)
            {
                var locked = false;
                try
                {
                    _lookupLock.Enter(ref locked);
                    
                    // register enqueued item for future updates
                    _lookup.Add(itemKey, item);
                }
                finally
                {
                    if (locked)
                        _lookupLock.Exit();
                }
                
                //Debug.WriteLine("[AggregateQueue] item with key {0} added at {1}", itemKey, item);
            }

            return true;
        }

        /// <inheritdoc />
        public bool TryTake(out TItem item)
        {
            if (!_queue.TryTake(out item))
            {
                // nothing yet
                return false;
            }
            
            if (item == _placeholder)
            {
                // pretend it wasn't here
                return true;
            }
            
            // increment dequeued counter
            Interlocked.Increment(ref _dequeued);
            
            var itemKey = _keySelector(item);
            if (itemKey != null)
            {
                var locked = false;
                try
                {
                    _lookupLock.Enter(ref locked);
                    
                    // prevent dequeued item from future updates
                    _lookup.Remove(itemKey);
                }
                finally
                {
                    if (locked)
                        _lookupLock.Exit();
                }
                
                //Debug.WriteLine("[AggregateQueue] item with key {0} removed at {1}", itemKey, item);
            }

            return true;
        }
        
        #region Statistics
        
        public long CountInput => Volatile.Read(ref _input);

        public long CountEnqueued => Volatile.Read(ref _enqueued);

        public long CountDequeued => Volatile.Read(ref _dequeued);

        public long CountInQueue => CountEnqueued - CountDequeued;
        
        #endregion
    }
}