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
            // This call registers a read access to the EventSingleton<T> component with the ECS dependency system.
            // Requesting a read-only ComponentTypeHandle informs the scheduler that this system will read the singleton.
            // Combined with the write declarations in the writer systems, this creates a proper dependency chain:
            // all writer systems' jobs will complete before this system's OnUpdate runs.
            // This ensures that when we call Events.Update() and swap buffers, no pending write jobs are still accessing the old write buffer.
            // The handle is not stored because we only need the dependency registration side effect.
            GetComponentTypeHandle<EventSingleton<T>>(true);
            
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