using NUnit.Framework;
using Unity.Entities;
using ED.DOTS.EntitiesEvents;

[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Tests.IntegrationTestEvent))]

namespace ED.DOTS.EntitiesEvents.Tests
{
    public struct IntegrationTestEvent
    {
        public int Value;
    }
    
    [TestFixture]
    public class ECSIntegrationTests : ECSTestBase
    {
        protected override void RegisterEventSystems(World world)
        {
            GetOrAddEventSystem<IntegrationTestEvent_EventSystem>();
        }
        
        // --- Тестовые системы ---

        // ISystem вариант писателя
        [DisableAutoCreation]
        public partial struct WriterISystem : ISystem
        {
            private EventWriter<IntegrationTestEvent> _writer;

            public void OnCreate(ref SystemState state)
            {
                _writer = state.GetEventWriter<IntegrationTestEvent>();
            }

            public void OnUpdate(ref SystemState state)
            {
                // Пишем по одному событию за кадр
                _writer.Write(new IntegrationTestEvent { Value = 1 });
            }
        }

        // SystemBase вариант писателя
        [DisableAutoCreation]
        public partial class WriterSystemBase : SystemBase
        {
            private EventWriter<IntegrationTestEvent> _writer;

            protected override void OnCreate()
            {
                _writer = this.GetEventWriter<IntegrationTestEvent>();
            }

            protected override void OnUpdate()
            {
                _writer.Write(new IntegrationTestEvent { Value = 2 });
            }
        }

        // ISystem читатель
        [DisableAutoCreation]
        public partial struct ReaderISystem : ISystem
        {
            private EventReader<IntegrationTestEvent> _reader;
            public int ReceivedCount;

            public void OnCreate(ref SystemState state)
            {
                _reader = state.GetEventReader<IntegrationTestEvent>();
            }

            public void OnUpdate(ref SystemState state)
            {
                ReceivedCount = 0;
                foreach (var ev in _reader)
                {
                    ReceivedCount++;
                }
            }
        }

        // SystemBase читатель
        [DisableAutoCreation]
        public partial class ReaderSystemBase : SystemBase
        {
            private EventReader<IntegrationTestEvent> _reader;
            public int ReceivedCount { get; private set; }

            protected override void OnCreate()
            {
                _reader = this.GetEventReader<IntegrationTestEvent>();
            }

            protected override void OnUpdate()
            {
                ReceivedCount = 0;
                foreach (var ev in _reader)
                {
                    ReceivedCount++;
                }
            }
        }

        // --- Тесты ---

        [Test]
        public void GetWriter_FromSystemState_CreatesSingletonAndReturnsValidWriter()
        {
            var query = EntityManager.CreateEntityQuery(typeof(EventSingleton<IntegrationTestEvent>));
            Assert.IsFalse(query.TryGetSingleton<EventSingleton<IntegrationTestEvent>>(out _));

            // Просто получаем writer через хелпер, он должен создать синглтон
            var writer = EntityManager.GetEventWriter<IntegrationTestEvent>();
            writer.Write(new IntegrationTestEvent { Value = 0 });

            // Синглтон должен существовать
            Assert.IsTrue(query.TryGetSingleton<EventSingleton<IntegrationTestEvent>>(out _));
        }

        [Test]
        public void WriterISystem_And_ReaderISystem_ExchangeEvents()
        {
            // Создаём системы
            ref var writer = ref GetOrAddSystemToSimulation<WriterISystem>();
            ref var reader = ref GetOrAddSystemToSimulation<ReaderISystem>();

            // Первый кадр: запись
            UpdateWorld(1);
            // После первого кадра события ещё не должны быть прочитаны, т.к. reader выполняется до обновления буфера?
            // В стандартной схеме reader должен читать события предыдущего кадра.
            // У нас EventSystemGroup обновляет буферы в конце кадра.
            // Значит в первом кадре reader видит 0 событий.
            Assert.AreEqual(0, reader.ReceivedCount);

            // Второй кадр: writer снова пишет, reader должен увидеть события из первого кадра
            UpdateWorld(1);
            Assert.AreEqual(1, reader.ReceivedCount);
        }

        [Test]
        public void WriterSystemBase_And_ReaderSystemBase_ExchangeEvents()
        {
            var writer = GetOrAddSystemToSimulationManaged<WriterSystemBase>();
            var reader = GetOrAddSystemToSimulationManaged<ReaderSystemBase>();

            UpdateWorld(1);
            Assert.AreEqual(0, reader.ReceivedCount);

            UpdateWorld(1);
            Assert.AreEqual(1, reader.ReceivedCount);
        }

        [Test]
        public unsafe void EnsureBufferCapacity_FromSystemState_Works()
        {
            var capacity = 1024;
            EntityManager.EnsureBufferCapacity<IntegrationTestEvent>(capacity);
            var singleton = EntitiesEventsHelper.GetOrCreateSingleton<IntegrationTestEvent>(EntityManager);
            var writer = singleton.Events.GetWriter();
            for (int i = 0; i < capacity; i++)
            {
                writer.WriteNoResize(new IntegrationTestEvent { Value = i });
            }
            // Если дошли сюда без исключений — ёмкости хватило.
        }

        [Test]
        public void GetWriter_FromEntityManager_ReturnsSameWriter()
        {
            var writer1 = EntityManager.GetEventWriter<IntegrationTestEvent>();
            var writer2 = EntityManager.GetEventWriter<IntegrationTestEvent>();
            // Они должны указывать на один и тот же буфер (но это внутренняя деталь, можно проверить через запись)
            writer1.Write(new IntegrationTestEvent { Value = 5 });
            writer2.Write(new IntegrationTestEvent { Value = 6 });

            var reader = EntityManager.GetEventReader<IntegrationTestEvent>();
            // Пока Update не вызван, reader не должен видеть события
            var enumerator = reader.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());

            // Вызовем Update вручную через хелпер (нужно получить Events)
            var singleton = EntitiesEventsHelper.GetOrCreateSingleton<IntegrationTestEvent>(EntityManager);
            singleton.Events.Update();

            var readerAfter = EntityManager.GetEventReader<IntegrationTestEvent>();
            int count = 0;
            foreach (var ev in readerAfter)
                count++;
            Assert.AreEqual(2, count);
        }
    }
}