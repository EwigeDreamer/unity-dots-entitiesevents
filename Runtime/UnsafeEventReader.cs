using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Unsafe reader for <see cref="UnsafeEvents{T}"/>.
    /// Provides direct pointer-based read access without safety checks.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    public unsafe struct UnsafeEventReader<T> where T : unmanaged
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

            /// <summary>
            /// Resets the enumerator to the beginning.
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