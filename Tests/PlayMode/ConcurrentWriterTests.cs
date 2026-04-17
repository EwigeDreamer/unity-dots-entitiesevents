using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using ED.DOTS.EntitiesEvents;
using Unity.Burst;

[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Tests.ConcurrentTestEvent))]

namespace ED.DOTS.EntitiesEvents.Tests
{
    public struct ConcurrentTestEvent
    {
        public int Value;
    }

    [TestFixture]
    public class ConcurrentWriterTests : ECSTestBase
    {
        protected override void RegisterEventSystems(World world)
        {
            world.GetOrCreateSystemManaged<ConcurrentTestEvent_EventSystem>();
        }

        [BurstCompile]
        private struct ParallelForBatchWriteJob : IJobParallelForBatch
        {
            public EventParallelWriter<ConcurrentTestEvent> Writer;
            public int BaseValue;

            public void Execute(int startIndex, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int index = startIndex + i;
                    Writer.WriteNoResize(new ConcurrentTestEvent { Value = BaseValue + index });
                }
            }
        }

        [Test]
        public unsafe void ScheduleParallel_ConcurrentWrites_AllEventsRecordedWithoutCorruption()
        {
            const int totalCount = 100;
            const int batchSize = 32;

            var events = new Events<ConcurrentTestEvent>(totalCount, Allocator.Persistent);
            events.GetUnsafeData()->EnsureCapacity(totalCount);

            var writer = events.GetWriter();
            var parallelWriter = writer.AsParallelWriter();

            var job = new ParallelForBatchWriteJob
            {
                Writer = parallelWriter,
                BaseValue = 0
            };
            var handle = job.ScheduleParallel(totalCount, batchSize, default);
            handle.Complete();

            events.Update();

            var reader = events.GetReader();
            int count = 0;
            foreach (var ev in reader)
                count++;

            Assert.AreEqual(totalCount, count);
            events.Dispose();
        }

        [Test]
        public void ScheduleParallel_InSystem_CompletesAndReadsCorrectly()
        {
            var writerSystem = World.GetOrCreateSystemManaged<ConcurrentWriterSystem>();
            var readerSystem = World.GetOrCreateSystemManaged<ConcurrentReaderSystem>();

            UpdateWorld(1);
            Assert.AreEqual(0, readerSystem.ReceivedCount);

            UpdateWorld(1);
            Assert.AreEqual(ConcurrentWriterSystem.EventCount, readerSystem.ReceivedCount);
            Assert.AreEqual(ConcurrentWriterSystem.ExpectedSum, readerSystem.ReceivedSum);
        }

        // --- Системы для интеграционного теста ---
        [DisableAutoCreation]
        public partial class ConcurrentWriterSystem : SystemBase
        {
            public const int EventCount = 3000;
            public const int ExpectedSum = (0 + EventCount - 1) * EventCount / 2; // сумма 0..(EventCount-1)
            private EventWriter<ConcurrentTestEvent> _writer;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<ConcurrentTestEvent>();
                EntityManager.EnsureBufferCapacity<ConcurrentTestEvent>(EventCount);
            }

            protected override void OnUpdate()
            {
                var parallelWriter = _writer.AsParallelWriter();
                var job = new ParallelForBatchWriteJob
                {
                    Writer = parallelWriter,
                    BaseValue = 0
                };
                Dependency = job.ScheduleParallel(EventCount, 64, Dependency);
            }
        }

        [DisableAutoCreation]
        public partial class ConcurrentReaderSystem : SystemBase
        {
            private EventReader<ConcurrentTestEvent> _reader;
            public int ReceivedCount { get; private set; }
            public int ReceivedSum { get; private set; }

            protected override void OnCreate()
            {
                _reader = this.GetEventReader<ConcurrentTestEvent>();
            }

            protected override void OnUpdate()
            {
                ReceivedCount = 0;
                ReceivedSum = 0;
                foreach (var ev in _reader)
                {
                    ReceivedCount++;
                    ReceivedSum += ev.Value;
                }
            }
        }
    }
}