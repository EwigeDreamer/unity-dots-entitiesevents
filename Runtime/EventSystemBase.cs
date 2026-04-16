using Unity.Burst;
using Unity.Entities;

namespace ED.DOTS.EntitiesEvents
{
    /// <summary>
    /// Base system for managing events of type <typeparamref name="T"/>.
    /// Updates the double-buffered event container each frame and handles cleanup.
    /// </summary>
    /// <typeparam name="T">Unmanaged event type.</typeparam>
    [BurstCompile]
    [UpdateInGroup(typeof(EventSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation
                       | WorldSystemFilterFlags.ServerSimulation
                       | WorldSystemFilterFlags.LocalSimulation)]
    public abstract unsafe partial class EventSystemBase<T> : SystemBase where T : unmanaged
    {
        [BurstCompile]
        protected override void OnCreate()
        {
            RequireForUpdate<EventSingleton<T>>();
        }

        [BurstCompile]
        protected override void OnUpdate()
        {
            CompleteDependency();
            
            if (SystemAPI.TryGetSingleton<EventSingleton<T>>(out var singleton))
            {
                singleton.Events.Update();
            }
        }

        [BurstCompile]
        protected override void OnDestroy()
        {
            if (SystemAPI.TryGetSingleton<EventSingleton<T>>(out var singleton))
            {
                singleton.Events.Dispose();
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<EventSingleton<T>>());
            }
        }
    }
}