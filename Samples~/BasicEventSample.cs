using ED.DOTS.EntitiesEvents;
using Unity.Entities;
using UnityEngine;

// Register the event type for source generation
[assembly: RegisterEvent(typeof(ED.DOTS.EntitiesEvents.Samples.BasicEvent))]

namespace ED.DOTS.EntitiesEvents.Samples
{
    /// <summary>
    /// Simple event structure used in the basic example.
    /// </summary>
    public struct BasicEvent
    {
        public int Value;
    }

    /// <summary>
    /// System that sends a BasicEvent every time the Space key is pressed.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BasicEventSenderSystem : SystemBase
    {
        private EventWriter<BasicEvent> _writer;

        protected override void OnCreate()
        {
            // Cache the writer for performance
            _writer = this.GetEventWriter<BasicEvent>();
        }

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Incrementing value just for demonstration
                int frameCount = UnityEngine.Time.frameCount;
                _writer.Write(new BasicEvent { Value = frameCount });
                Debug.Log($"[Sender] Sent BasicEvent with Value = {frameCount}");
            }
        }
    }

    /// <summary>
    /// System that reads BasicEvent and logs them to the console.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BasicEventSenderSystem))] // Ensure reading after sending
    public partial class BasicEventReceiverSystem : SystemBase
    {
        private EventReader<BasicEvent> _reader;

        protected override void OnCreate()
        {
            // Cache the reader
            _reader = this.GetEventReader<BasicEvent>();
        }

        protected override void OnUpdate()
        {
            // Read all events received this frame
            foreach (var evt in _reader.Read())
            {
                Debug.Log($"[Receiver] Received BasicEvent with Value = {evt.Value}");
            }
        }
    }
}