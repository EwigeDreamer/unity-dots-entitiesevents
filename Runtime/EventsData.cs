using System;
using Unity.Collections;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Manages double-buffered event storage for type <typeparamref name="T"/>.
    /// Internally owns two <see cref="NativeEventBuffer{T}"/> instances and swaps their roles on <see cref="Update"/>.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    internal struct EventsData<T> : IDisposable
        where T : unmanaged
    {
        private NativeEventBuffer<T> _buffer1;
        private NativeEventBuffer<T> _buffer2;
        private bool _state; // true: buffer2 is write, buffer1 is read; false: buffer1 is write, buffer2 is read

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsData{T}"/> struct with the specified capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for both internal buffers.</param>
        /// <param name="allocator">Allocator to use for all internal allocations.</param>
        public EventsData(int initialCapacity, Allocator allocator)
        {
            _buffer1 = new NativeEventBuffer<T>(initialCapacity, allocator);
            _buffer2 = new NativeEventBuffer<T>(initialCapacity, allocator);
            _state = false; // initially buffer1 is write, buffer2 is read
        }

        /// <summary>
        /// Swaps the roles of internal buffers and clears the new write buffer.
        /// Call this once per frame to advance the event pipeline.
        /// </summary>
        public void Update()
        {
            _state = !_state;
            if (_state)
            {
                // Now buffer2 is write, buffer1 is read
                _buffer2.Clear();
            }
            else
            {
                // Now buffer1 is write, buffer2 is read
                _buffer1.Clear();
            }
        }

        /// <summary>
        /// Returns the buffer currently designated for writing.
        /// </summary>
        public NativeEventBuffer<T> GetWriteBuffer()
        {
            return _state ? _buffer2 : _buffer1;
        }

        /// <summary>
        /// Returns the buffer currently designated for reading.
        /// </summary>
        public NativeEventBuffer<T> GetReadBuffer()
        {
            return _state ? _buffer1 : _buffer2;
        }

        /// <summary>
        /// Ensures that both internal buffers have at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Minimum capacity required.</param>
        public void EnsureCapacity(int capacity)
        {
            _buffer1.EnsureCapacity(capacity);
            _buffer2.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Disposes both internal buffers.
        /// </summary>
        public void Dispose()
        {
            _buffer1.Dispose();
            _buffer2.Dispose();
        }
    }
}