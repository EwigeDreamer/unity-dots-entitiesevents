using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Manages double-buffered event storage for type <typeparamref name="T"/>.
    /// Internally owns two <see cref="NativeEventBuffer{T}"/> instances allocated on the heap
    /// and swaps their roles on <see cref="Update"/>.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    internal unsafe struct EventsData<T> : IDisposable where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private NativeEventBuffer<T>* _buffer1;

        [NativeDisableUnsafePtrRestriction]
        private NativeEventBuffer<T>* _buffer2;

        private bool _state; // true: buffer2 is write, buffer1 is read; false: buffer1 is write, buffer2 is read

        private readonly Allocator _allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsData{T}"/> struct with the specified capacity and allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for both internal buffers.</param>
        /// <param name="allocator">Allocator to use for all internal allocations.</param>
        public EventsData(int initialCapacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob, Persistent or registered custom allocator", nameof(allocator));
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "InitialCapacity must be >= 0");
#endif

            _allocator = allocator;

            // Allocate and initialize buffer1
            var size = UnsafeUtility.SizeOf<NativeEventBuffer<T>>();
            var alignment = UnsafeUtility.AlignOf<NativeEventBuffer<T>>();
            _buffer1 = (NativeEventBuffer<T>*)UnsafeUtility.MallocTracked(size, alignment, allocator, 1);
            UnsafeUtility.MemClear(_buffer1, size);
            var temp1 = new NativeEventBuffer<T>(initialCapacity, allocator);
            UnsafeUtility.CopyStructureToPtr(ref temp1, _buffer1);

            // Allocate and initialize buffer2
            _buffer2 = (NativeEventBuffer<T>*)UnsafeUtility.MallocTracked(size, alignment, allocator, 1);
            UnsafeUtility.MemClear(_buffer2, size);
            var temp2 = new NativeEventBuffer<T>(initialCapacity, allocator);
            UnsafeUtility.CopyStructureToPtr(ref temp2, _buffer2);

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
                _buffer2->Clear();
            }
            else
            {
                // Now buffer1 is write, buffer2 is read
                _buffer1->Clear();
            }
        }

        /// <summary>
        /// Returns a reference to the buffer currently designated for writing.
        /// </summary>
        public NativeEventBuffer<T>* GetWriteBuffer()
        {
            return _state ? _buffer2 : _buffer1;
        }

        /// <summary>
        /// Returns a reference to the buffer currently designated for reading.
        /// </summary>
        public NativeEventBuffer<T>* GetReadBuffer()
        {
            return _state ? _buffer1 : _buffer2;
        }

        /// <summary>
        /// Ensures that both internal buffers have at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Minimum capacity required.</param>
        public void EnsureCapacity(int capacity)
        {
            _buffer1->EnsureCapacity(capacity);
            _buffer2->EnsureCapacity(capacity);
        }

        /// <summary>
        /// Disposes both internal buffers and frees allocated memory.
        /// </summary>
        public void Dispose()
        {
            if (_buffer1 != null)
            {
                _buffer1->Dispose();
                UnsafeUtility.FreeTracked(_buffer1, _allocator);
                _buffer1 = null;
            }

            if (_buffer2 != null)
            {
                _buffer2->Dispose();
                UnsafeUtility.FreeTracked(_buffer2, _allocator);
                _buffer2 = null;
            }
        }
    }
}