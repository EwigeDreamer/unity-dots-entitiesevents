using Unity.Collections;
using Unity.Entities;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Helper methods for managing <see cref="EventSingleton{T}"/> entities.
    /// Can be used directly when finer control over singleton creation is needed.
    /// </summary>
    public static class EntitiesEventsHelper
    {
        /// <summary>
        /// Gets the existing singleton component or creates a new entity with a fresh event container.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="state">Reference to the system state.</param>
        /// <returns>The singleton component.</returns>
        public static EventSingleton<T> GetOrCreateSingleton<T>(ref SystemState state) where T : unmanaged
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<EventSingleton<T>>();
            var query = builder.Build(ref state);
            if (query.TryGetSingleton<EventSingleton<T>>(out var singleton))
                return singleton;

            var events = new Events<T>(512, Allocator.Persistent);
            singleton = new EventSingleton<T> { Events = events };
            state.EntityManager.CreateSingleton(singleton);
            return singleton;
        }

        /// <summary>
        /// Gets the existing singleton component or creates a new entity with a fresh event container.
        /// </summary>
        /// <typeparam name="T">Unmanaged event type.</typeparam>
        /// <param name="entityManager">The entity manager.</param>
        /// <returns>The singleton component.</returns>
        public static EventSingleton<T> GetOrCreateSingleton<T>(EntityManager entityManager) where T : unmanaged
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<EventSingleton<T>>();
            var query = builder.Build(entityManager);
            if (query.TryGetSingleton<EventSingleton<T>>(out var singleton))
                return singleton;
            var events = new Events<T>(512, Allocator.Persistent);
            singleton = new EventSingleton<T> { Events = events };
            entityManager.CreateSingleton(singleton);
            return singleton;
        }
    }
}