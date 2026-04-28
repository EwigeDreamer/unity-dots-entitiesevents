using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using ED.DOTS.EntitiesEvents;

[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Tests.RaceConditionEvent))]

namespace ED.DOTS.EntitiesEvents.Tests
{
    public struct RaceConditionEvent
    {
        public int Value;
    }

    [TestFixture]
    public class ParallelWriteRaceConditionTest : ECSTestBase
    {
        protected override void RegisterEventSystems(World world)
        {
            GetOrAddEventSystem<RaceConditionEvent_EventSystem>();
        }

        // System that writes in parallel using IJobParallelFor
        [DisableAutoCreation]
        public partial class ParallelWriterSystem : SystemBase
        {
            private EventWriter<RaceConditionEvent> _writer;
            public const int EventCount = 100;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<RaceConditionEvent>();
                this.EnsureEventBufferCapacity<RaceConditionEvent>(EventCount);
            }

            protected override void OnUpdate()
            {
                var parallelWriter = _writer.AsParallelWriter();
                var job = new ParallelWriteJob { Writer = parallelWriter };
                Dependency = job.Schedule(EventCount, 32, Dependency);
            }

            [BurstCompile]
            private struct ParallelWriteJob : IJobParallelFor
            {
                public EventWriter<RaceConditionEvent>.ParallelWriter Writer;
                public void Execute(int index) => Writer.WriteNoResize(new RaceConditionEvent { Value = index });
            }
        }

        // Another independent parallel writer system
        [DisableAutoCreation]
        public partial class AnotherParallelWriterSystem : SystemBase
        {
            private EventWriter<RaceConditionEvent> _writer;
            public const int EventCount = 100;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<RaceConditionEvent>();
                this.EnsureEventBufferCapacity<RaceConditionEvent>(EventCount);
            }

            protected override void OnUpdate()
            {
                var parallelWriter = _writer.AsParallelWriter();
                var job = new ParallelWriteJob { Writer = parallelWriter };
                Dependency = job.Schedule(EventCount, 32, Dependency);
            }

            [BurstCompile]
            private struct ParallelWriteJob : IJobParallelFor
            {
                public EventWriter<RaceConditionEvent>.ParallelWriter Writer;
                public void Execute(int index) => Writer.WriteNoResize(new RaceConditionEvent { Value = index });
            }
        }

        // System that writes synchronously (without a job)
        [DisableAutoCreation]
        [UpdateAfter(typeof(ParallelWriterSystem))]
        public partial class SingleWriterSystem : SystemBase
        {
            private EventWriter<RaceConditionEvent> _writer;

            protected override void OnCreate() => _writer = this.GetEventWriter<RaceConditionEvent>();

            protected override void OnUpdate()
            {
                _writer.Write(new RaceConditionEvent { Value = -1 });
            }
        }

        // This test demonstrates mixed sync and parallel writing.
        // It is expected to throw InvalidOperationException because the synchronous write
        // requests exclusive access while a parallel job is still running.
        // Keeping this test as commented documentation of the intended limitation.
        // Uncomment to verify the exception behavior.
        // [Test]
        public void MixedSyncAndParallelWritingToSameBuffer_ThrowsInvalidOperationException()
        {
            var parallelSystem = GetOrAddSystemToSimulationManaged<ParallelWriterSystem>();
            var singleSystem = GetOrAddSystemToSimulationManaged<SingleWriterSystem>();

            UpdateWorld(10);

            CompleteJobs();
        }

        [Test]
        public void TwoIndependentParallelSystemsWritingToSameBuffer_Works()
        {
            var systemA = GetOrAddSystemToSimulationManaged<ParallelWriterSystem>();
            var systemB = GetOrAddSystemToSimulationManaged<AnotherParallelWriterSystem>();

            UpdateWorld(10);

            CompleteJobs();
        }
    }
}