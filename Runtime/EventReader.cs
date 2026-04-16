using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Provides read access to events of type <typeparamref name="T"/>.
    /// Can be used in jobs with appropriate safety handles.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct EventReader<T>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly NativeEventBuffer<T>* _readBuffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        internal EventReader(in Events<T> events)
        {
            var data = events._container._data;
            _readBuffer = data->GetReadBuffer();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Use the safety handle of the specific read buffer
            m_Safety = _readBuffer->m_Safety;
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
#endif
        }

        /// <summary>
        /// Returns an enumerator that iterates over all events in the read buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return new Enumerator(_readBuffer);
        }

        /// <summary>
        /// Enumerator for iterating over events in the read buffer.
        /// </summary>
        [BurstCompile]
        public struct Enumerator : IEnumerator<T>
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly T* _ptr;
            private readonly int _length;
            private int _index;

            internal Enumerator(NativeEventBuffer<T>* buffer)
            {
                var listPtr = buffer->_listPtr;
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

            object IEnumerator.Current => Current;

            /// <summary>
            /// Resets the enumerator to the beginning.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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