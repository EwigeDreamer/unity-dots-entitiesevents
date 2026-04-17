using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Provides read access to events of type <typeparamref name="T"/>.
    /// Can be safely cached across frames; always reads from the current active read buffer.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct EventReader<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EventsData<T>* _data;

        internal EventReader(in Events<T> events)
        {
            _data = events._container._data;
            // m_Safety не хранится — проверки выполняются на уровне конкретного буфера
        }

        /// <summary>
        /// Returns an enumerator that iterates over all events in the current read buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            var readBuffer = _data->GetReadBuffer();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(readBuffer->m_Safety);
#endif
            return new Enumerator(readBuffer);
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
            public void Dispose() { }
        }
    }
}