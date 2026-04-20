using UnityEngine;
using Unity.Collections;
using ED.DOTS.EntitiesEvents;

namespace ED.DOTS.EntitiesEvents.Samples
{
    /// <summary>
    /// Example of manual event handling without ECS systems.
    /// Creates an Events container, writes values on key press, and manually updates/reads.
    /// </summary>
    public class ManualEventsExample : MonoBehaviour
    {
        private Events<int> _events;
        private EventWriter<int> _writer;
        private EventReader<int> _reader;
        private int _counter;

        private void Start()
        {
            _events = new Events<int>(64, Allocator.Persistent);
            _writer = _events.GetWriter();
            _reader = _events.GetReader();
            _counter = 0;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                _writer.Write(_counter);
                Debug.Log($"[Manual] Wrote event: {_counter}");
                _counter++;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _events.Update();

                int sum = 0;
                int count = 0;
                foreach (int value in _reader.Read())
                {
                    Debug.Log($"[Manual] Read event: {value}");
                    sum += value;
                    count++;
                }

                Debug.Log($"[Manual] Total after Update: {count} events, sum = {sum}");
            }
        }

        private void OnDestroy()
        {
            if (_events.IsCreated)
                _events.Dispose();
        }
    }
}