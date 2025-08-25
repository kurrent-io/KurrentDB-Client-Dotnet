# Stream State Handling: Prioritized, Operation-Centric Implementation Plan

> **Document Version**: 4.0 (Final)
> **Date**: 2025-07-25
> **Status**: Actionable Task List

This document provides a detailed, granular implementation plan to resolve the stream state ambiguity issue. The work is broken down by **operation**, with each task being self-contained (implementation, testing, documentation) to enable parallel development.

---

## Execution Priority

To maximize impact and unblock critical management functions, the tasks should be executed in the following order:

1.  **Task 1 (Prerequisite)**: Implement the `GetStreamState` Public Operation.
2.  **Task 2 & 3 (High Priority)**: Refactor `Delete` and `Tombstone` Operations in parallel.
3.  **Tasks 4-7 (Parallelizable)**: All remaining refactoring tasks can be worked on in any order.

---

## Prerequisite Task

This single task must be completed first. It delivers a complete, new public feature that all other tasks depend on.

### Task 1: Implement the `GetStreamState` Public Operation
-   **Description**: Introduce a new, public-facing management operation that can reliably determine the state of any given stream. This operation's logic should be based on the existing (but flawed) logic within the current `GetStreamInfo` method.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Management.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/Management/GetStreamStateTests.cs` (new file)
-   **Actions**:
    1.  **Define API Contract**: In `StreamsClientModels.cs`, define the public `enum StreamState { Active, Deleted, Tombstoned, NotFound }` and the `GetStreamStateError` result variant.
    2.  **Implement Operation**: In `KurrentStreamsClient.Management.cs`, implement the `public ValueTask<Result<StreamState, GetStreamStateError>> GetStreamState(...)` method. Extract and correct the logic from the existing `GetStreamInfo` method to read the stream's metadata stream (`$$<stream-name>`) and, if necessary, the primary stream to reliably determine the state.
    3.  **Write Integration Tests**: Create a new test file and write comprehensive tests verifying that `GetStreamState` returns the correct `StreamState` value for streams that are active, not found, deleted, and tombstoned.
    4.  **Write Documentation**: Add complete XML documentation for the new `GetStreamState` method, the `StreamState` enum, and the `GetStreamStateError` variant.

---

## Parallelizable Refactoring Tasks

Once the prerequisite task is complete, the following tasks can be picked up and worked on in any order, respecting the priority outlined above.

### Task 2: Refactor `Delete` Operation
-   **Description**: Ensure the `Delete` operation fails correctly when targeting a tombstoned stream.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Management.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/Management/DeleteTests.cs`
-   **Actions**:
    1.  **Update Result Variant**: In `StreamsClientModels.cs`, add `ErrorDetails.StreamTombstoned` to the `DeleteStreamError` variant.
    2.  **Refactor `Delete`**: Update the `catch` block to call `GetStreamState` and return a `StreamTombstoned` error if the stream is already tombstoned.
    3.  **Update Tests**: In `DeleteTests.cs`, update the test that attempts to delete a tombstoned stream to assert the new `StreamTombstoned` error.
    4.  **Update Documentation**: Update the XML comments for `Delete`.

### Task 3: Refactor `Tombstone` Operation
-   **Description**: Ensure the `Tombstone` operation fails correctly when targeting an already-tombstoned stream.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Management.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/Management/TombstoneTests.cs`
-   **Actions**:
    1.  **Update Result Variant**: In `StreamsClientModels.cs`, add `ErrorDetails.StreamTombstoned` to the `TombstoneError` variant.
    2.  **Refactor `Tombstone`**: Update the `catch` block to call `GetStreamState` and return a `StreamTombstoned` error if the stream is already tombstoned.
    3.  **Update Tests**: Add a test to `TombstoneTests.cs` to verify that attempting to tombstone an already-tombstoned stream fails with `StreamTombstoned`.
    4.  **Update Documentation**: Update the XML comments for `Tombstone`.

### Task 4: Refactor Read Operations
-   **Description**: Correct the error handling for all read operations (`ReadStream`, `ReadAll`, etc.) to return specific, accurate errors.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Read.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/ReadTests.cs`
-   **Actions**:
    1.  **Update Result Variant**: In `StreamsClientModels.cs`, add `ErrorDetails.StreamTombstoned` to the `ReadError` variant.
    2.  **Refactor `ReadCore`**: In the `catch (StreamDeletedException)` block of the `ReadCore` method, replace the existing logic. The new logic must call `GetStreamState` and return the appropriate error: `StreamTombstoned`, `StreamDeleted`, or `StreamNotFound`.
    3.  **Update Tests**: In `ReadTests.cs`, update any tests that incorrectly expect `StreamDeleted` to now expect `StreamTombstoned`. Add a new test to verify that reading a soft-deleted stream correctly returns a `StreamDeleted` error.
    4.  **Update Documentation**: Update the XML comments for `ReadStream` and `ReadAll` to document the new, precise error results.
    5.  **Cleanup**: Remove the obsolete `TODO` comment from `ReadCore`.

### Task 5: Refactor Subscription Operations
-   **Description**: Fix the critical silent failure bug in the `Subscribe` operation.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Subscriptions.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/SusbcriptionTests.cs`
-   **Actions**:
    1.  **Refactor `SubscribeCore`**: Add a `catch (StreamDeletedException)` block. This block must call `GetStreamState`. If the state is `Tombstoned`, the method must return a `Result.Failure(new StreamTombstoned(...))`. For any other state (`Deleted`, `NotFound`), the exception must be suppressed, and the operation must proceed successfully.
    2.  **Add Integration Tests**: In `SusbcriptionTests.cs`, add new tests to verify:
        -   Subscribing to a non-existent stream succeeds.
        -   Subscribing to a deleted stream succeeds.
        -   Subscribing to a tombstoned stream fails with `StreamTombstoned`.
    3.  **Update Documentation**: Update the XML comments for the `Subscribe` method to explain that it will now correctly fail for tombstoned streams.

### Task 6: Refactor Append Operations
-   **Description**: Correct the behavior of the `Append` operation to allow recreating soft-deleted streams and to fail correctly for tombstoned streams.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Append.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/AppendTests.cs`
-   **Actions**:
    1.  **Update Result Variant**: In `StreamsClientModels.cs`, add `ErrorDetails.StreamTombstoned` to the `AppendStreamFailure` variant.
    2.  **Refactor `Append`**: Modify the failure handling logic. When a `StreamDeleted` failure is detected, it must call `GetStreamState`. If `Tombstoned`, it must return a `StreamTombstoned` error. If `Deleted`, it should allow the operation to proceed to recreate the stream.
    3.  **Add Integration Tests**: In `AppendTests.cs`, add new tests to verify:
        -   Appending to a deleted stream succeeds.
        -   Appending to a tombstoned stream fails with `StreamTombstoned`.
    4.  **Update Documentation**: Update the XML comments for the `Append` method to explain the new behavior.

### Task 7: Refactor `GetStreamInfo` Operation
-   **Description**: Ensure the `GetStreamInfo` operation returns accurate information by using the new `GetStreamState` operation.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Management.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/Management/StreamInfoTests.cs`
-   **Actions**:
    1.  **Refactor `GetStreamInfo`**: Replace the flawed error handling by calling the new `GetStreamState` operation. Use its result to accurately set the `IsDeleted` and `IsTombstoned` boolean flags on the returned `StreamInfo` object.
    2.  **Update Tests**: Add tests to `StreamInfoTests.cs` to verify the `IsDeleted` and `IsTombstoned` flags are set correctly for all stream states.
    3.  **Update Documentation**: Update the XML comments for `GetStreamInfo`.
    4.  **Cleanup**: Remove the obsolete `TODO` comment from `GetStreamInfo`.

### Task 8: Refactor `Truncate` Operation
-   **Description**: Ensure the `Truncate` operation fails correctly when targeting a deleted or tombstoned stream.
-   **Files to Modify**:
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/StreamsClientModels.cs`
    -   `src/KurrentDB.Client/Kurrent.Client/Model/Streams/KurrentStreamsClient.Management.cs`
    -   `test/Kurrent.Client.Integration.Tests/Streams/Management/TruncateTests.cs`
-   **Actions**:
    1.  **Update Result Variant**: In `StreamsClientModels.cs`, add `ErrorDetails.StreamTombstoned` to the `TruncateStreamError` variant.
    2.  **Refactor `Truncate`**: The `Truncate` operation is implemented via `SetStreamMetadata`. Ensure the error path is updated to call `GetStreamState` and return `StreamDeleted` or `StreamTombstoned` as appropriate.
    3.  **Add Integration Tests**: Add tests to `TruncateTests.cs` to verify that attempting to truncate a deleted or tombstoned stream fails with the correct specific error.
    4.  **Update Documentation**: Update the XML comments for `Truncate`.