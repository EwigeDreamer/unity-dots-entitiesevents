using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;

namespace ED.DOTS.EntitiesEvents.Tests
{
    /// <summary>
    /// Base class for ECS PlayMode tests that require an isolated world.
    /// Provides automatic setup and cleanup of a fresh World for each test.
    /// </summary>
    public abstract class ECSTestBase
    {
        protected World World { get; private set; }
        protected EntityManager EntityManager => World.EntityManager;

        private World _previousWorld;

        [SetUp]
        public virtual void SetUp()
        {
            // Store the previous default world to restore later
            _previousWorld = World.DefaultGameObjectInjectionWorld;

            // Create a fresh world for this test
            World = new World("Test World", WorldFlags.Game);

            // Set it as the default injection world so that queries and singletons work
            World.DefaultGameObjectInjectionWorld = World;

            // Ensure systems do not automatically update unless explicitly called
            World.GetOrCreateSystemManaged<InitializationSystemGroup>();
            World.GetOrCreateSystemManaged<SimulationSystemGroup>();
            World.GetOrCreateSystemManaged<PresentationSystemGroup>();

            RegisterEventSystems(World);
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Restore the previous default world
            World.DefaultGameObjectInjectionWorld = _previousWorld;

            // Dispose the test world and all its resources
            if (World.IsCreated)
                World.Dispose();

            World = null;
        }
        
        protected abstract void RegisterEventSystems(World world);

        /// <summary>
        /// Helper to complete all scheduled jobs and update the world for a number of frames.
        /// </summary>
        protected void UpdateWorld(int frameCount = 1)
        {
            for (int i = 0; i < frameCount; i++)
            {
                World.Update();
                EntityManager.CompleteAllTrackedJobs();
            }
        }

        /// <summary>
        /// Ensures all tracked jobs are completed.
        /// </summary>
        protected void CompleteJobs() => EntityManager.CompleteAllTrackedJobs();
    }
}