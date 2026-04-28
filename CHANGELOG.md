# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.4] - 2026-04-28

### Added
- Documented limitation: mixing synchronous and parallel writes to the same event type within a single frame is not allowed and will trigger a safety exception. Added a detailed warning and recommendations in the README.
- Added `ParallelWriteRaceConditionTest` to cover the mixed‑write scenario (commented by default) and confirm that independent parallel writers work correctly.

## [1.0.3] - 2026-04-23

### Fixed
- Fixed missing ECS dependency tracking between event writer systems and the internal event update system.  
  Added explicit `GetComponentTypeHandle<EventSingleton<T>>()` calls in `GetEventWriter` extensions and in `EventSystemBase<T>.OnUpdate`.  
  This ensures proper job completion before buffer swapping, preventing potential race conditions.

## [1.0.2] - 2026-04-21

### Changed
- Merged `EventParallelWriter<T>` into `EventWriter<T>.ParallelWriter` and removed duplicate parallel writer from `NativeEventBuffer<T>` for a cleaner and more maintainable architecture.
- Updated tests and examples to use `EventWriter<T>.ParallelWriter`.

## [1.0.1] - 2026-04-21

### Changed
- Removed redundant safety checks in `EventWriter<T>` to reduce overhead.

## [1.0.0] - 2026-04-20

### Added
- Initial release of the Entities Events library for Unity DOTS.
- Core `Events<T>` container with double buffering based on `NativeEventBuffer<T>`.
- Thread-safe `EventWriter<T>` and `EventReader<T>`.
- `EventParallelWriter<T>` for parallel writes from `IJobParallelFor` and `IJobParallelForBatch`.
- Source generator to automatically create event systems and register generic singletons.
- Extension methods for `SystemState`, `SystemBase`, and `EntityManager` to easily get readers/writers.
- Comprehensive PlayMode test suite covering core logic, ECS integration, and parallel writes.
- Basic, Advanced, and Manual usage examples.