using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Unsafe writer for <see cref="UnsafeEvents{T}"/>.
    /// Provides direct pointer-based write access without safety checks.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    public unsafe struct UnsafeEventWriter<T> where T : unmanaged
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
}