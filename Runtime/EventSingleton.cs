using Unity.Entities;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Singleton component that holds an <see cref="Events{T}"/> container for event messaging.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    internal struct EventSingleton<T> : IComponentData where T : unmanaged
    {
        /// <summary>
        /// The event container instance.
        /// </summary>
        public Events<T> Events;
    }
}