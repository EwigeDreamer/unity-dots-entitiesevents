# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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