using Unity.Entities;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Extension methods for obtaining <see cref="EventWriter{T}"/> and <see cref="EventReader{T}"/>
    /// from ECS systems and entity managers.
    /// </summary>
    public static class EntitiesEventsExtensions
    {
        /// <summary>
        /// Gets an event writer for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="state">Reference to the system state.</param>
        /// <returns>An <see cref="EventWriter{T}"/> for publishing events.</returns>
        public static EventWriter<T> GetEventWriter<T>(this ref SystemState state)
            where T : unmanaged
        {
            // This call registers a write access to the EventSingleton<T> component with the ECS dependency system.
            // Even though we don't use the returned handle, the act of requesting a writable ComponentTypeHandle
            // informs the ECS scheduler that this system intends to write to the singleton.
            // This ensures that any jobs writing to the event buffer are completed before the event system updates the buffer,
            // preventing race conditions and potential data corruption.
            // The handle is not stored because we only need the side effect of dependency registration, not direct access.
            state.GetComponentTypeHandle<EventSingleton<T>>();

            return EntitiesEventsHelper.GetOrCreateSingleton<T>(ref state).Events.GetWriter();
        }

        /// <summary>
        /// Gets an event writer for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="systemBase">The system base instance.</param>
        /// <returns>An <see cref="EventWriter{T}"/> for publishing events.</returns>
        public static EventWriter<T> GetEventWriter<T>(this SystemBase systemBase)
            where T : unmanaged
        {
            // This call registers a write access to the EventSingleton<T> component with the ECS dependency system.
            // Even though we don't use the returned handle, the act of requesting a writable ComponentTypeHandle
            // informs the ECS scheduler that this system intends to write to the singleton.
            // This ensures that any jobs writing to the event buffer are completed before the event system updates the buffer,
            // preventing race conditions and potential data corruption.
            // The handle is not stored because we only need the side effect of dependency registration, not direct access.
            systemBase.CheckedStateRef.GetComponentTypeHandle<EventSingleton<T>>();

            return GetEventWriter<T>(ref systemBase.CheckedStateRef);
        }

        /// <summary>
        /// Gets an event writer for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="entityManager">The entity manager.</param>
        /// <returns>An <see cref="EventWriter{T}"/> for publishing events.</returns>
        public static EventWriter<T> GetEventWriter<T>(this EntityManager entityManager)
            where T : unmanaged
        {
            // This call registers a write access to the EventSingleton<T> component with the ECS dependency system.
            // Even though we don't use the returned handle, the act of requesting a writable ComponentTypeHandle
            // informs the ECS scheduler that this system intends to write to the singleton.
            // This ensures that any jobs writing to the event buffer are completed before the event system updates the buffer,
            // preventing race conditions and potential data corruption.
            // The handle is not stored because we only need the side effect of dependency registration, not direct access.
            entityManager.GetComponentTypeHandle<EventSingleton<T>>(false);

            return EntitiesEventsHelper.GetOrCreateSingleton<T>(entityManager).Events.GetWriter();
        }

        /// <summary>
        /// Gets an event reader for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="state">Reference to the system state.</param>
        /// <returns>An <see cref="EventReader{T}"/> for consuming events.</returns>
        public static EventReader<T> GetEventReader<T>(this ref SystemState state)
            where T : unmanaged
        {
            return EntitiesEventsHelper.GetOrCreateSingleton<T>(ref state).Events.GetReader();
        }

        /// <summary>
        /// Gets an event reader for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="systemBase">The system base instance.</param>
        /// <returns>An <see cref="EventReader{T}"/> for consuming events.</returns>
        public static EventReader<T> GetEventReader<T>(this SystemBase systemBase)
            where T : unmanaged
        {
            return GetEventReader<T>(ref systemBase.CheckedStateRef);
        }

        /// <summary>
        /// Gets an event reader for the specified event type.
        /// Creates a singleton entity with the event container if it does not exist.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="entityManager">The entity manager.</param>
        /// <returns>An <see cref="EventReader{T}"/> for consuming events.</returns>
        public static EventReader<T> GetEventReader<T>(this EntityManager entityManager)
            where T : unmanaged
        {
            return EntitiesEventsHelper.GetOrCreateSingleton<T>(entityManager).Events.GetReader();
        }

        /// <summary>
        /// Ensures that the internal event buffers have at least the specified capacity.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="state">Reference to the system state.</param>
        /// <param name="capacity">Minimum capacity required.</param>
        public static unsafe void EnsureEventBufferCapacity<T>(this ref SystemState state, int capacity)
            where T : unmanaged
        {
            var singleton = EntitiesEventsHelper.GetOrCreateSingleton<T>(ref state);
            singleton.Events.GetUnsafeData()->EnsureCapacity(capacity);
        }

        /// <summary>
        /// Ensures that the internal event buffers have at least the specified capacity.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="entityManager">The entity manager.</param>
        /// <param name="capacity">Minimum capacity required.</param>
        public static unsafe void EnsureEventBufferCapacity<T>(this EntityManager entityManager, int capacity)
            where T : unmanaged
        {
            var singleton = EntitiesEventsHelper.GetOrCreateSingleton<T>(entityManager);
            singleton.Events.GetUnsafeData()->EnsureCapacity(capacity);
        }

        /// <summary>
        /// Ensures that the internal event buffers have at least the specified capacity.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="entityManager">The entity manager.</param>
        /// <param name="capacity">Minimum capacity required.</param>
        public static unsafe void EnsureEventBufferCapacity<T>(this SystemBase systemBase, int capacity)
            where T : unmanaged
        {
            EnsureEventBufferCapacity<T>(ref systemBase.CheckedStateRef, capacity);
        }
    }
}