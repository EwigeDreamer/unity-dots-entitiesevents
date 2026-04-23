# Entities Events

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

A library that adds event functionality to Unity's Entity Component System (ECS). Enables messaging between systems using `EventWriter` and `EventReader`.

> **Note:** The Entities Events library implements a **broadcast (one‑to‑many)** communication pattern. Any event written by a system is delivered to all systems that read that event type. Events are automatically cleared after one frame, making them ideal for notifications, state changes, and one‑time messages where multiple consumers may be interested.

## Features
* Thread‑safe event writing and reading.
* Supports parallel writing from multi‑threaded jobs (`IJobParallelFor`, `IJobParallelForBatch`).
* Double buffering ensures that events written in one frame become available for reading in the next.
* Automatic lifecycle management via ECS singletons and generated systems.
* Source generator eliminates manual event type registration.

## Requirements
* Unity 6000.0 or higher
* Packages:
  * `com.unity.entities` 1.4.5 or higher
  * `com.unity.collections` 2.6.5 or higher
  * `com.unity.burst` 1.8.27 or higher

## Installation
Add the package via Package Manager using the git URL:
```
https://github.com/EwigeDreamer/unity-dots-entitiesevents.git
```

## Basic Usage
Define an event struct:

```csharp
public struct MyEvent
{
    public int Value;
}
```

Register the event type with an assembly attribute (generates the required system):

```csharp
using ED.DOTS.EntitiesEvents;

[assembly: RegisterEvent(typeof(MyEvent))]
```

Sender system:

```csharp
using Unity.Entities;
using ED.DOTS.EntitiesEvents;

public partial class SenderSystem : SystemBase
{
    private EventWriter<MyEvent> _writer;

    protected override void OnCreate()
    {
        _writer = this.GetEventWriter<MyEvent>();
    }

    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            _writer.Write(new MyEvent { Value = 123 });
    }
}
```

Receiver system:

```csharp
using Unity.Entities;
using ED.DOTS.EntitiesEvents;

public partial class ReceiverSystem : SystemBase
{
    private EventReader<MyEvent> _reader;

    protected override void OnCreate()
    {
        _reader = this.GetEventReader<MyEvent>();
    }

    protected override void OnUpdate()
    {
        foreach (var evt in _reader.Read())
        {
            Debug.Log($"Received: {evt.Value}");
        }
    }
}
```

## Parallel Writing from Jobs

Use `EventWriter<T>.ParallelWriter` for writing from multi‑threaded jobs. The writer is thread‑safe and supports both `IJobParallelFor` and `IJobParallelForBatch` with `ScheduleParallel`, allowing many threads to write concurrently without data corruption.

```csharp
[BurstCompile]
struct ParallelJob : IJobParallelFor
{
    public EventWriter<MyEvent>.ParallelWriter Writer;

    public void Execute(int index)
    {
        Writer.WriteNoResize(new MyEvent { Value = index });
    }
}
```

In your system, cache the writer and schedule the job:

```csharp
private EventWriter<MyEvent> _writer;

protected override void OnCreate()
{
    _writer = this.GetEventWriter<MyEvent>();
}

protected override void OnUpdate()
{
    // Ensure capacity before scheduling
    EntityManager.EnsureEventBufferCapacity<MyEvent>(eventCount);

    var job = new ParallelJob { Writer = _writer.AsParallelWriter() };
    Dependency = job.Schedule(eventCount, 64, Dependency);
}
```

For batch parallel jobs, use `IJobParallelForBatch` and `ScheduleParallel` — the writer remains safe under high contention.

Always call `EnsureEventBufferCapacity` before parallel writes to avoid reallocations inside the job.

## Manual Usage (Without ECS)
You can create an `Events<T>` container directly:

```csharp
var events = new Events<int>(64, Allocator.Persistent);
var writer = events.GetWriter();
var reader = events.GetReader();

writer.Write(42);
events.Update(); // swaps buffers

foreach (int val in reader.Read())
{
    Debug.Log(val); // 42
}

events.Dispose();
```

## Performance Considerations
* Cache `EventWriter` and `EventReader` in `OnCreate` — they safely update internal pointers when buffers are swapped.
* Always call `EnsureBufferCapacity` before parallel writes to avoid reallocations inside jobs.
* Ensure all write jobs are completed before calling `Update()` on the container (generated systems handle this automatically via `CompleteDependency()`).

## License

[MIT License](LICENSE.md)

## Acknowledgements
This project is based on [EntitiesEvents](https://github.com/annulusgames/EntitiesEvents) by [annulusgames](https://github.com/annulusgames). Added support for Netcode for Entities, improved parallel job handling, and extended test coverage.

Created with the support of artificial intelligence.