using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Unsafe low-level container for <see cref="EventsData{T}"/>.
    /// Handles manual memory allocation and provides methods to interact with the event data.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    public unsafe struct UnsafeEvents<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal EventsData<T>* _data;

        private readonly Allocator _allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeEvents{T}"/> struct.
        /// Allocates memory for <see cref="EventsData{T}"/> using the specified allocator.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity for internal buffers.</param>
        /// <param name="allocator">Allocator to use for memory allocation.</param>
        public UnsafeEvents(int initialCapacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob, Persistent or registered custom allocator", nameof(allocator));
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "InitialCapacity must be >= 0");
#endif

            var size = UnsafeUtility.SizeOf<EventsData<T>>();
            var alignment = UnsafeUtility.AlignOf<EventsData<T>>();
            _data = (EventsData<T>*)UnsafeUtility.MallocTracked(size, alignment, allocator, 1);
            UnsafeUtility.MemClear(_data, size);

            var data = new EventsData<T>(initialCapacity, allocator);
            UnsafeUtility.CopyStructureToPtr(ref data, _data);

            _allocator = allocator;
        }

        /// <summary>
        /// Gets a value indicating whether this container is allocated.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data != null;
        }

        /// <summary>
        /// Updates the internal double-buffering state.
        /// Swaps read/write buffers and clears the new write buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            CheckData();
            _data->Update();
        }

        /// <summary>
        /// Returns an unsafe writer for writing events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeEventWriter<T> GetWriter()
        {
            CheckData();
            return new UnsafeEventWriter<T>(in this);
        }

        /// <summary>
        /// Returns an unsafe reader for reading events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeEventReader<T> GetReader()
        {
            CheckData();
            return new UnsafeEventReader<T>(in this);
        }

        /// <summary>
        /// Disposes the internal <see cref="EventsData{T}"/> and frees allocated memory.
        /// </summary>
        public void Dispose()
        {
            if (_data == null)
                return;

            _data->Dispose();
            UnsafeUtility.FreeTracked(_data, _allocator);
            _data = null;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckData()
        {
            if (_data == null)
                throw new InvalidOperationException("UnsafeEvents has not been allocated or has been disposed.");
        }
    }

    /// <summary>
    /// Unsafe writer for <see cref="UnsafeEvents{T}"/>.
    /// Provides direct pointer-based write access without safety checks.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    public unsafe struct UnsafeEventWriter<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly NativeEventBuffer<T>* _writeBuffer;

        internal UnsafeEventWriter(in UnsafeEvents<T> events)
        {
            _writeBuffer = events._data->GetWriteBuffer();
        }

        /// <summary>
        /// Writes an event into the buffer. The buffer may grow.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(in T value)
        {
            _writeBuffer->Write(value);
        }

        /// <summary>
        /// Writes an event without checking capacity. Ensure capacity before use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNoResize(in T value)
        {
            _writeBuffer->WriteNoResize(value);
        }

        /// <summary>
        /// Returns a parallel writer for concurrent writes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeEventBuffer<T>.ParallelWriter AsParallelWriter()
        {
            return _writeBuffer->AsParallelWriter();
        }
    }

    /// <summary>
    /// Unsafe reader for <see cref="UnsafeEvents{T}"/>.
    /// Provides direct pointer-based read access without safety checks.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    public unsafe struct UnsafeEventReader<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly NativeEventBuffer<T>* _readBuffer;

        internal UnsafeEventReader(in UnsafeEvents<T> events)
        {
            _readBuffer = events._data->GetReadBuffer();
        }

        /// <summary>
        /// Returns an enumerator that iterates over all events in the read buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_readBuffer);
        }

        /// <summary>
        /// Enumerator for iterating over events in the read buffer.
        /// </summary>
        public struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly T* _ptr;
            private readonly int _length;
            private int _index;

            internal Enumerator(NativeEventBuffer<T>* buffer)
            {
                // Access the internal UnsafeList<T> pointer from NativeEventBuffer.
                // Since we are inside ED.DOTS.EntitiesEvents, we can access internal fields.
                // For now, we'll assume NativeEventBuffer exposes a method to get the list pointer.
                // We'll add an internal method in NativeEventBuffer later if needed.
                // Placeholder logic:
                var listPtr = buffer->ListPtr;
                _ptr = listPtr->Ptr;
                _length = listPtr->Length;
                _index = -1;
            }

            /// <summary>
            /// Moves to the next element.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index++;
                return _index < _length;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _ptr[_index];
            }

            /// <summary>
            /// Resets the enumerator.
            /// </summary>
            public void Reset()
            {
                _index = -1;
            }

            /// <summary>
            /// Disposes the enumerator.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }
}