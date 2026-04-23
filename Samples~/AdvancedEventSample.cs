using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using ED.DOTS.EntitiesEvents;

// Register the event type for source generation
[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Samples.ParallelEvent))]

namespace ED.DOTS.EntitiesEvents.Samples
{
    /// <summary>
    /// Event structure for the parallel write example.
    /// </summary>
    public struct ParallelEvent
    {
        public int Index;
    }

    /// <summary>
    /// Unmanaged system that schedules a parallel batch job to write events when P is pressed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ParallelEventSenderSystem : ISystem
    {
        private EventWriter<ParallelEvent> _writer;
        private const int EventsCount = 1000;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _writer = state.GetEventWriter<ParallelEvent>();
            state.EnsureEventBufferCapacity<ParallelEvent>(EventsCount);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                var job = new ParallelWriteBatchJob
                {
                    Writer = _writer.AsParallelWriter()
                };

                state.Dependency = job.ScheduleParallel(EventsCount, 64, state.Dependency);
                Debug.Log($"[Sender] Scheduled parallel batch job to write {EventsCount} events.");
            }
        }

        /// <summary>
        /// Parallel batch job that writes events using EventParallelWriter.
        /// </summary>
        [BurstCompile]
        private struct ParallelWriteBatchJob : IJobParallelForBatch
        {
            public EventWriter<ParallelEvent>.ParallelWriter Writer;

            public void Execute(int startIndex, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int index = startIndex + i;
                    Writer.WriteNoResize(new ParallelEvent { Index = index });
                }
            }
        }
    }

    /// <summary>
    /// System that reads ParallelEvent in the next frame and logs summary.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ParallelEventSenderSystem))]
    public partial struct ParallelEventReceiverSystem : ISystem
    {
        private EventReader<ParallelEvent> _reader;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _reader = state.GetEventReader<ParallelEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = 0;
            int min = int.MaxValue;
            int max = int.MinValue;

            foreach (var evt in _reader.Read())
            {
                count++;
                if (evt.Index < min) min = evt.Index;
                if (evt.Index > max) max = evt.Index;
            }

            if (count > 0)
            {
                Debug.Log($"[Receiver] Received {count} ParallelEvents. Min index: {min}, Max index: {max}");
            }
        }
    }
}