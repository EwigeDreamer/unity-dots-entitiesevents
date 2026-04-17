using NUnit.Framework;
using Unity.Collections;
using ED.DOTS.EntitiesEvents;
using Unity.Entities;

namespace ED.DOTS.EntitiesEvents.Tests
{
    
    [TestFixture]
    public class CoreTests : ECSTestBase
    {

        protected override void RegisterEventSystems(World world) { }
        
        [Test]
        public void CreateAndDispose_Works()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);
            Assert.IsTrue(events.IsCreated);
            events.Dispose();
            Assert.IsFalse(events.IsCreated);
        }

        [Test]
        public void WriteAndRead_SameFrame_ReadsNothing()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);
            var writer = events.GetWriter();
            writer.Write(new IntegrationTestEvent { Value = 42 });

            var reader = events.GetReader();
            using var enumerator = reader.GetEnumerator();

            Assert.IsFalse(enumerator.MoveNext());

            events.Dispose();
        }

        [Test]
        public void WriteThenUpdate_ThenRead_ReturnsEvents()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);

            var writer = events.GetWriter();
            writer.Write(new IntegrationTestEvent { Value = 1 });
            writer.Write(new IntegrationTestEvent { Value = 2 });

            events.Update();

            var reader = events.GetReader();
            using var enumerator = reader.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current.Value);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());

            events.Dispose();
        }

        [Test]
        public void MultipleWrites_ReadAll_InOrder()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);

            var writer = events.GetWriter();
            for (int i = 0; i < 100; i++)
            {
                writer.Write(new IntegrationTestEvent { Value = i });
            }

            events.Update();

            var reader = events.GetReader();
            using var enumerator = reader.GetEnumerator();

            int expected = 0;
            while (enumerator.MoveNext())
            {
                Assert.AreEqual(expected, enumerator.Current.Value);
                expected++;
            }
            Assert.AreEqual(100, expected);

            events.Dispose();
        }

        [Test]
        public void Update_ClearsWriteBuffer()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);
            var reader = events.GetReader();
            
            var enumerator = reader.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();

            var writer = events.GetWriter();
            writer.Write(new IntegrationTestEvent { Value = 123 });
            
            enumerator = reader.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();
            
            events.Update();
            
            enumerator = reader.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(123, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();

            var writer2 = events.GetWriter();
            writer2.Write(new IntegrationTestEvent { Value = 456 });
            
            enumerator = reader.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(123, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();
            
            events.Update();

            enumerator = reader.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(456, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();

            events.Dispose();
        }
        
        [Test]
        public void CachedWriterAndReader_WorkAcrossUpdates()
        {
            var events = new Events<IntegrationTestEvent>(16, Allocator.Persistent);

            // Кэшируем writer и reader до вызова Update
            var writer = events.GetWriter();
            var reader = events.GetReader();

            // Первый кадр: запись
            writer.Write(new IntegrationTestEvent { Value = 100 });
            events.Update();

            // Чтение должно вернуть событие из первого кадра
            var enumerator = reader.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(100, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();

            // Второй кадр: запись через тот же writer
            writer.Write(new IntegrationTestEvent { Value = 200 });
            events.Update();

            // Чтение через тот же reader
            enumerator = reader.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(200, enumerator.Current.Value);
            Assert.IsFalse(enumerator.MoveNext());
            enumerator.Dispose();

            events.Dispose();
        }
    }
}