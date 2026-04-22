using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using ED.DOTS.EntitiesEvents;
using Unity.Burst;
using UnityEngine;

[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Tests.ParallelTestEvent))]

namespace ED.DOTS.EntitiesEvents.Tests
{
    public struct ParallelTestEvent
    {
        public int Value;
    }

    [TestFixture]
    public class ParallelWriterTests : ECSTestBase
    {
        protected override void RegisterEventSystems(World world)
        {
            GetOrAddEventSystem<ParallelTestEvent_EventSystem>();
        }

        // Джоб для параллельной записи событий
        [BurstCompile]
        private struct ParallelWriteJob : IJobParallelFor
        {
            public EventWriter<ParallelTestEvent>.ParallelWriter Writer;
            public int BaseValue;

            public void Execute(int index)
            {
                var value = BaseValue + index;
                Writer.WriteNoResize(new ParallelTestEvent { Value = value });
            }
        }

        [Test]
        public unsafe void ParallelWrite_ThenRead_AllEventsReceived()
        {
            const int eventCount = 1000;

            var events = new Events<ParallelTestEvent>(eventCount, Allocator.Persistent);
            var writer = events.GetWriter();

            // Убедимся, что ёмкости достаточно для параллельной записи без ресайза
            events.GetUnsafeData()->EnsureCapacity(eventCount);

            var parallelWriter = writer.AsParallelWriter();

            var job = new ParallelWriteJob
            {
                Writer = parallelWriter,
                BaseValue = 0
            };
            var handle = job.Schedule(eventCount, 64);
            handle.Complete();

            // После записи делаем Update, чтобы переключить буферы
            events.Update();

            var reader = events.GetReader();
            int count = 0;
            using var enumerator = reader.Read().GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }

            Assert.AreEqual(eventCount, count);
            events.Dispose();
        }

        [Test]
        public unsafe void ParallelWriteAndRead_Concurrently_BeforeUpdate_NoSafetyException()
        {
            const int eventCount = 100;
            var events = new Events<ParallelTestEvent>(eventCount, Allocator.Persistent);
            events.GetUnsafeData()->EnsureCapacity(eventCount);

            var writer = events.GetWriter();
            var parallelWriter = writer.AsParallelWriter();

            // Запускаем джоб записи
            var job = new ParallelWriteJob { Writer = parallelWriter, BaseValue = 0 };
            var handle = job.Schedule(eventCount, 64);

            var reader = events.GetReader();

            // Попытка чтения во время работы джоба записи НЕ должна вызывать исключение,
            // так как буферы разные.
            Assert.DoesNotThrow(() =>
            {
                int count = 0;
                foreach (var _ in reader.Read())
                {
                    count++;
                }
                // На данном этапе read-буфер пуст (мы ещё не делали Update),
                // поэтому count будет 0, но главное — отсутствие исключения.
            });

            handle.Complete();
            events.Dispose();
        }

        [Test]
        public void ParallelWrite_InSystem_And_ReadInSystem_Works()
        {
            var writerSystem = GetOrAddSystemToSimulationManaged<ParallelWriterTestSystem>();
            var readerSystem = GetOrAddSystemToSimulationManaged<ParallelReaderTestSystem>();

            // Первый кадр: writer запускает джоб и записывает события
            UpdateWorld(1);
            // События ещё не должны быть доступны для чтения
            Assert.AreEqual(0, readerSystem.ReceivedCount);

            // Второй кадр: reader должен увидеть события
            UpdateWorld(1);
            Assert.AreEqual(ParallelWriterTestSystem.EventCount, readerSystem.ReceivedCount);
        }

        // --- Тестовые системы для интеграции джобов в ECS ---

        [DisableAutoCreation]
        public partial class ParallelWriterTestSystem : SystemBase
        {
            public const int EventCount = 100;
            private EventWriter<ParallelTestEvent> _writer;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<ParallelTestEvent>();
                // Гарантируем ёмкость
                EntityManager.EnsureEventBufferCapacity<ParallelTestEvent>(EventCount);
            }

            protected override void OnUpdate()
            {
                var parallelWriter = _writer.AsParallelWriter();
                var job = new ParallelWriteJob
                {
                    Writer = parallelWriter,
                    BaseValue = 0
                };
                Dependency = job.Schedule(EventCount, 8, Dependency);
            }
        }

        [DisableAutoCreation]
        public partial class ParallelReaderTestSystem : SystemBase
        {
            private EventReader<ParallelTestEvent> _reader;
            public int ReceivedCount { get; private set; }

            protected override void OnCreate()
            {
                _reader = this.GetEventReader<ParallelTestEvent>();
            }

            protected override void OnUpdate()
            {
                ReceivedCount = 0;
                foreach (var ev in _reader.Read())
                {
                    ReceivedCount++;
                }
            }
        }
    }
}