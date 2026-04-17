using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Provides write access to events of type <typeparamref name="T"/>.
    /// Can be safely cached across frames; always writes to the current active write buffer.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public unsafe struct EventWriter<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EventsData<T>* _data;

        internal EventWriter(in Events<T> events)
        {
            _data = events._container._data;
            // m_Safety не хранится — проверки выполняются на уровне конкретного буфера
        }

        /// <summary>
        /// Writes an event into the current write buffer. The buffer will grow if necessary.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(in T value)
        {
            var writeBuffer = _data->GetWriteBuffer();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(writeBuffer->m_Safety);
#endif
            writeBuffer->Write(value);
        }

        /// <summary>
        /// Writes an event without checking capacity.
        /// Ensure buffer capacity is sufficient before calling this method.
        /// </summary>
        /// <param name="value">Event data to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNoResize(in T value)
        {
            var writeBuffer = _data->GetWriteBuffer();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(writeBuffer->m_Safety);
#endif
            writeBuffer->WriteNoResize(value);
        }

        /// <summary>
        /// Returns a parallel writer that can be used to write events from multiple threads.
        /// The parallel writer captures the current write buffer at call time.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventParallelWriter<T> AsParallelWriter()
        {
            var writeBuffer = _data->GetWriteBuffer();
            return new EventParallelWriter<T>(writeBuffer);
        }
    }
}