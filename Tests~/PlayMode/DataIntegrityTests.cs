using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using ED.DOTS.EntitiesEvents;
using Unity.Burst;

[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Tests.DataIntegrityEvent))]

namespace ED.DOTS.EntitiesEvents.Tests
{
    public struct DataIntegrityEvent
    {
        public int Value;
    }

    [TestFixture]
    public class DataIntegrityTests : ECSTestBase
    {
        protected override void RegisterEventSystems(World world)
        {
            GetOrAddEventSystem<DataIntegrityEvent_EventSystem>();
        }

        [BurstCompile]
        private struct ParallelWriteJob : IJobParallelFor
        {
            public EventWriter<DataIntegrityEvent>.ParallelWriter Writer;

            public void Execute(int index)
            {
                Writer.WriteNoResize(new DataIntegrityEvent { Value = index });
            }
        }

        [BurstCompile]
        private struct ParallelForBatchWriteJob : IJobParallelForBatch
        {
            public EventWriter<DataIntegrityEvent>.ParallelWriter Writer;

            public void Execute(int startIndex, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int index = startIndex + i;
                    Writer.WriteNoResize(new DataIntegrityEvent { Value = index });
                }
            }
        }

        [Test]
        public unsafe void SequentialWrite_DataIntegrity()
        {
            const int eventCount = 500;
            var events = new Events<DataIntegrityEvent>(eventCount, Allocator.Persistent);
            var writer = events.GetWriter();

            for (int i = 0; i < eventCount; i++)
            {
                writer.Write(new DataIntegrityEvent { Value = i });
            }

            events.Update();

            var reader = events.GetReader();
            var received = new HashSet<int>();
            foreach (var ev in reader.Read())
                received.Add(ev.Value);

            Assert.AreEqual(eventCount, received.Count);
            for (int i = 0; i < eventCount; i++)
                Assert.IsTrue(received.Contains(i), $"Missing value {i}");

            events.Dispose();
        }

        [Test]
        public unsafe void ParallelWrite_DataIntegrity()
        {
            const int eventCount = 1000;
            var events = new Events<DataIntegrityEvent>(eventCount, Allocator.Persistent);
            events.GetUnsafeData()->EnsureCapacity(eventCount);

            var writer = events.GetWriter();
            var parallelWriter = writer.AsParallelWriter();

            var job = new ParallelWriteJob { Writer = parallelWriter };
            var handle = job.Schedule(eventCount, 64);
            handle.Complete();

            events.Update();

            var reader = events.GetReader();
            var received = new HashSet<int>();
            foreach (var ev in reader.Read())
                received.Add(ev.Value);

            Assert.AreEqual(eventCount, received.Count);
            for (int i = 0; i < eventCount; i++)
                Assert.IsTrue(received.Contains(i), $"Missing value {i}");

            events.Dispose();
        }

        [Test]
        public unsafe void ScheduleParallel_DataIntegrity()
        {
            const int totalCount = 1000;
            const int batchSize = 64;

            var events = new Events<DataIntegrityEvent>(totalCount, Allocator.Persistent);
            events.GetUnsafeData()->EnsureCapacity(totalCount);

            var writer = events.GetWriter();
            var parallelWriter = writer.AsParallelWriter();

            var job = new ParallelForBatchWriteJob { Writer = parallelWriter };
            var handle = job.ScheduleParallel(totalCount, batchSize);
            handle.Complete();

            events.Update();

            var reader = events.GetReader();
            var received = new HashSet<int>();
            foreach (var ev in reader.Read())
                received.Add(ev.Value);

            Assert.AreEqual(totalCount, received.Count);
            for (int i = 0; i < totalCount; i++)
                Assert.IsTrue(received.Contains(i), $"Missing value {i}");

            events.Dispose();
        }

        [DisableAutoCreation]
        public partial class DataIntegrityWriterSystem : SystemBase
        {
            public const int EventCount = 800;
            private EventWriter<DataIntegrityEvent> _writer;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<DataIntegrityEvent>();
                EntityManager.EnsureEventBufferCapacity<DataIntegrityEvent>(EventCount);
            }

            protected override void OnUpdate()
            {
                var parallelWriter = _writer.AsParallelWriter();
                var job = new ParallelWriteJob { Writer = parallelWriter };
                Dependency = job.Schedule(EventCount, 64, Dependency);
            }
        }

        [DisableAutoCreation]
        public partial class DataIntegrityReaderSystem : SystemBase
        {
            private EventReader<DataIntegrityEvent> _reader;
            public NativeHashSet<int> ReceivedSet;

            protected override void OnCreate()
            {
                _reader = this.GetEventReader<DataIntegrityEvent>();
                ReceivedSet = new NativeHashSet<int>(1000, Allocator.Persistent);
            }

            protected override void OnDestroy()
            {
                ReceivedSet.Dispose();
            }

            protected override void OnUpdate()
            {
                ReceivedSet.Clear();
                foreach (var ev in _reader.Read())
                    ReceivedSet.Add(ev.Value);
            }
        }

        [Test]
        public void InSystem_ParallelWrite_DataIntegrity()
        {
            var writerSystem = GetOrAddSystemToSimulationManaged<DataIntegrityWriterSystem>();
            var readerSystem = GetOrAddSystemToSimulationManaged<DataIntegrityReaderSystem>();

            UpdateWorld(1);
            Assert.AreEqual(0, readerSystem.ReceivedSet.Count);

            UpdateWorld(1);
            var received = readerSystem.ReceivedSet;
            int expectedCount = DataIntegrityWriterSystem.EventCount;
            Assert.AreEqual(expectedCount, received.Count);

            for (int i = 0; i < expectedCount; i++)
                Assert.IsTrue(received.Contains(i), $"Missing value {i}");
        }
    }
}