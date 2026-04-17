using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// A thread-safe native container for double-buffered events of type <typeparamref name="T"/>.
    /// Provides <see cref="EventWriter{T}"/> and <see cref="EventReader{T}"/> for inter-system messaging.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [NativeContainer]
    public unsafe struct Events<T> : IDisposable where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal EventsData<T>* _data;

        private readonly Allocator _allocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Events{T}"/> struct.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of internal event buffers.</param>
        /// <param name="allocator">Allocator to use for all internal allocations.</param>
        public Events(int initialCapacity, Allocator allocator)
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
        /// Gets a value indicating whether this container has been allocated.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data != null;
        }

        /// <summary>
        /// Updates the internal double-buffering state.
        /// Swaps read/write buffers and clears the new write buffer.
        /// Call this once per frame to advance the event pipeline.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            CheckData();
            _data->Update();
        }

        /// <summary>
        /// Returns a writer for publishing events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventWriter<T> GetWriter()
        {
            CheckData();
            return new EventWriter<T>(this);
        }

        /// <summary>
        /// Returns a reader for consuming published events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventReader<T> GetReader()
        {
            CheckData();
            return new EventReader<T>(this);
        }

        /// <summary>
        /// Releases all resources held by this container.
        /// </summary>
        public void Dispose()
        {
            if (_data == null)
                return;

            _data->Dispose();
            UnsafeUtility.FreeTracked(_data, _allocator);
            _data = null;
        }

        /// <summary>
        /// Internal accessor for the underlying unsafe events pointer.
        /// </summary>
        public EventsData<T>* GetUnsafeData()
        {
            return _data;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckData()
        {
            if (_data == null)
                throw new InvalidOperationException("Events has not been allocated or has been disposed.");
        }
    }
}