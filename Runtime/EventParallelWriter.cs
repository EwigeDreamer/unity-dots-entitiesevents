using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Provides parallel write access to events of type <typeparamref name="T"/>.
    /// Suitable for use in <see cref="Unity.Jobs.IJobParallelFor"/> and similar.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public unsafe struct EventParallelWriter<T>
        where T : unmanaged
    {
        private NativeEventBuffer<T>.ParallelWriter _parallelWriter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        internal EventParallelWriter(in EventWriter<T> writer)
        {
            _parallelWriter = writer._writeBuffer->AsParallelWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = writer.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
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
            _parallelWriter.WriteNoResize(value);
        }
    }
}