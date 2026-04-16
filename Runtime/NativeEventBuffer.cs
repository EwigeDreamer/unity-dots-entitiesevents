using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Native container that stores events of type <typeparamref name="T"/> in a resizable list.
    /// Supports parallel writing via <see cref="ParallelWriter"/>.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [NativeContainer]
    public unsafe struct NativeEventBuffer<T> : IDisposable where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList<T>* _listPtr;

        private Allocator _allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeEventBuffer{T}"/> struct.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the internal list.</param>
        /// <param name="allocator">Allocator to use for memory allocations.</param>
        public NativeEventBuffer(int initialCapacity, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob, Persistent or registered custom allocator", nameof(allocator));
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "InitialCapacity must be >= 0");
#endif

            var size = UnsafeUtility.SizeOf<UnsafeList<T>>();
            var alignment = UnsafeUtility.AlignOf<UnsafeList<T>>();
            _listPtr = (UnsafeList<T>*)UnsafeUtility.MallocTracked(size, alignment, allocator, 1);
            UnsafeUtility.MemClear(_listPtr, size);
            var list = new UnsafeList<T>(initialCapacity, allocator);
            UnsafeUtility.CopyStructureToPtr(ref list, _listPtr);
            _allocator = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<NativeEventBuffer<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            if (UnsafeUtility.IsNativeContainerType<T>())
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeEventBuffer<T>>();
#endif

        /// <summary>
        /// Gets a value indicating whether this buffer has been allocated.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _listPtr != null;
        }

        /// <summary>
        /// Writes an event into the buffer. The buffer will grow if necessary.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(in T value)
        {
            CheckWriteAccess();
            _listPtr->Add(value);
        }

        /// <summary>
        /// Writes an event into the buffer without checking capacity.
        /// Must be used with <see cref="ParallelWriter"/> after ensuring sufficient capacity.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNoResize(in T value)
        {
            CheckWriteAccess();
            _listPtr->AddNoResize(value);
        }

        /// <summary>
        /// Clears all events from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWriteAccess();
            _listPtr->Clear();
        }

        /// <summary>
        /// Ensures that the buffer has at least the specified capacity.
        /// </summary>
        /// <param name="capacity">Minimum capacity required.</param>
        public void EnsureCapacity(int capacity)
        {
            CheckWriteAccess();
            var newCapacity = _listPtr->Capacity;
            while (newCapacity < capacity) newCapacity *= 2;
            _listPtr->SetCapacity(newCapacity);
        }

        /// <summary>
        /// Returns a parallel writer that can be used to write events from multiple threads.
        /// </summary>
        /// <returns>A <see cref="ParallelWriter"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParallelWriter AsParallelWriter()
        {
            // No write access check needed because we are not modifying state yet.
            // The parallel writer itself will perform checks when writing.
            return new ParallelWriter(this);
        }

        /// <summary>
        /// Releases all resources held by this buffer.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
                return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _listPtr->Dispose();
            UnsafeUtility.FreeTracked(_listPtr, _allocator);
            _listPtr = null;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        /// <summary>
        /// Provides parallel write access to a <see cref="NativeEventBuffer{T}"/>.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            private UnsafeList<T>.ParallelWriter _writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif

            internal ParallelWriter(in NativeEventBuffer<T> buffer)
            {
                _writer = buffer._listPtr->AsParallelWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = buffer.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
                AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
            }

            /// <summary>
            /// Writes an event into the buffer without checking capacity.
            /// Ensure capacity before using this method.
            /// </summary>
            /// <param name="value">Event data to write.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteNoResize(in T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                _writer.AddNoResize(value);
            }
        }
    }
}