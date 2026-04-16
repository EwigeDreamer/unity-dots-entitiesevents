using System;
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
    [NativeContainer]
    public unsafe struct Events<T> : IDisposable where T : unmanaged
    {
        internal UnsafeEvents<T> _container;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Events<T>>();
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Events{T}"/> struct.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of internal event buffers.</param>
        /// <param name="allocator">Allocator to use for all internal allocations.</param>
        public Events(int initialCapacity, Allocator allocator)
        {
            _container = new UnsafeEvents<T>(initialCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<Events<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            if (UnsafeUtility.IsNativeContainerType<T>())
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Gets a value indicating whether this container has been allocated.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _container.IsCreated;
        }

        /// <summary>
        /// Updates the internal double-buffering state.
        /// Swaps read/write buffers and clears the new write buffer.
        /// Call this once per frame to advance the event pipeline.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _container.Update();
        }

        /// <summary>
        /// Returns a writer for publishing events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventWriter<T> GetWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            return new EventWriter<T>(this);
        }

        /// <summary>
        /// Returns a reader for consuming published events.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventReader<T> GetReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            return new EventReader<T>(this);
        }

        /// <summary>
        /// Releases all resources held by this container.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _container.Dispose();
        }

        /// <summary>
        /// Internal accessor for the underlying unsafe events pointer.
        /// </summary>
        internal EventsData<T>* GetUnsafeData()
        {
            return _container._data;
        }
    }
}