using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Provides write access to events of type <typeparamref name="T"/>.
    /// Can be used in jobs with appropriate safety handles.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public unsafe struct EventWriter<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly NativeEventBuffer<T>* _writeBuffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        internal EventWriter(in Events<T> events)
        {
            var data = events._container._data;
            _writeBuffer = data->GetWriteBuffer();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Use the safety handle of the specific write buffer
            m_Safety = _writeBuffer->m_Safety;
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Writes an event into the buffer. The buffer will grow if necessary.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _writeBuffer->Write(value);
        }

        /// <summary>
        /// Writes an event without checking capacity.
        /// Ensure buffer capacity is sufficient before calling this method.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNoResize(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _writeBuffer->WriteNoResize(value);
        }

        /// <summary>
        /// Returns a parallel writer that can be used to write events from multiple threads.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventParallelWriter<T> AsParallelWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            return new EventParallelWriter<T>(this);
        }
    }
}