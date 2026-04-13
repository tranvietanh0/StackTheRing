# Continuous extraction retest (per-entry serialization)

## 1. Automated test status
- No automated suite was executed because the Unity 6 entry-gate logic lives inside the Unity editor and no CLI-accessible regression tests exist for these conveyor/entry interactions.

## 2. Remaining obvious critical static risks
- Entry detection still depends on `CollectAreaBucketService` being assigned before runtime; if the service is null or its target bucket queue is empty, rows will keep circling without being cleared and nothing in this code will break that loop.
- The in-memory `processingEntryIndices`/`processingAtEntry` guards are still unvalidated under multi-row contention; without instrumentation or tests we cannot prove the async entry handler never leaves an index marked during an exception, so this is the largest static risk remaining.

## 3. Unresolved questions
- Is there an existing Unity regression harness we can run (or add) to cover the newly serialized entry handling so future regressions are caught automatically?
