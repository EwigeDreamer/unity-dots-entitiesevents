using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using ED.DOTS.EntitiesEvents;
using Unity.Burst;
using UnityEngine;

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
            GetOrAddEventSystem<ConcurrentTestEvent_EventSystem>();
        }

        [BurstCompile]
        private struct ParallelForBatchWriteJob : IJobParallelForBatch
        {
            public EventWriter<ConcurrentTestEvent>.ParallelWriter Writer;
            public int BaseValue;

            public void Execute(int startIndex, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int index = startIndex + i;
                    var value = BaseValue + index;
                    Writer.WriteNoResize(new ConcurrentTestEvent { Value = value });
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
            var handle = job.ScheduleParallel(totalCount, batchSize);
            handle.Complete();

            events.Update();

            var reader = events.GetReader();
            int count = 0;
            foreach (var ev in reader.Read())
                count++;

            Assert.AreEqual(totalCount, count);
            events.Dispose();
        }

        [Test]
        public void ScheduleParallel_InSystem_CompletesAndReadsCorrectly()
        {
            var writerSystem = GetOrAddSystemToSimulationManaged<ConcurrentWriterSystem>();
            var readerSystem = GetOrAddSystemToSimulationManaged<ConcurrentReaderSystem>();
            
            // Первый кадр: writer планирует джоб, EventSystem обновляет буферы в конце кадра
            UpdateWorld(1);
            // После первого кадра reader ещё не видел событий, потому что читает до обновления буфера
            Assert.AreEqual(0, readerSystem.ReceivedCount);
            
            // Второй кадр: reader должен увидеть события, записанные в первом кадре
            UpdateWorld(1);
            Assert.AreEqual(ConcurrentWriterSystem.EventCount, readerSystem.ReceivedCount);
        }

        // --- Системы для интеграционного теста ---
        [DisableAutoCreation]
        public partial class ConcurrentWriterSystem : SystemBase
        {
            public const int EventCount = 100;
            private EventWriter<ConcurrentTestEvent> _writer;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<ConcurrentTestEvent>();
                EntityManager.EnsureEventBufferCapacity<ConcurrentTestEvent>(EventCount);
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
                foreach (var ev in _reader.Read())
                {
                    ReceivedCount++;
                    ReceivedSum += ev.Value;
                }
            }
        }
    }
}