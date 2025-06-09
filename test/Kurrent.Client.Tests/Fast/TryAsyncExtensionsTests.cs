using KurrentDB.Client;

namespace Kurrent.Client.Tests.Fast;

/// <summary>
/// Tests for the asynchronous extension methods for <see cref="Try{TSuccess}"/>.
/// </summary>
public class TryAsyncExtensionsTests {
    const           string    OriginalSuccessValue      = "success";
    const           string    MappedSuccessValue        = "mapped";
    const           string    BoundSuccessValue         = "bound";
    static readonly Exception ActionException           = new("Action Exception");
    static readonly Exception BinderException           = new("Binder Exception");
    static readonly Exception FailedSourceTaskException = new("Failed Source Task Exception");
    static readonly Exception MapperException           = new("Mapper Exception");
    static readonly Exception TestException             = new("Test Exception");

    // Helper methods for creating source Tasks/ValueTasks
    Task<Try<string>> SuccessSourceTask(string value = OriginalSuccessValue) => Task.FromResult(new Try<string>(value));
    Task<Try<string>> ErrorSourceTask(Exception? ex = null)                  => Task.FromResult(new Try<string>(ex ?? TestException));
    Task<Try<string>> FailedSourceTask(Exception? ex = null)                 => Task.FromException<Try<string>>(ex ?? FailedSourceTaskException);

    Try<string> SuccessSource(string value = OriginalSuccessValue) => new(value);
    Try<string> ErrorSource(Exception? ex = null)                  => new(ex ?? TestException);

    #region . ThenAsync Tests .

    [Test]
    public async Task then_async_task_source_task_binder_success_path() {
        var source = SuccessSourceTask();
        var result = await source.ThenAsync(s => Task.FromResult(new Try<string>(s + BoundSuccessValue)));

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue + BoundSuccessValue);
    }

    [Test]
    public async Task then_async_task_source_task_binder_propagates_source_error() {
        var source       = ErrorSourceTask();
        var binderCalled = false;
        var result = await source.ThenAsync(s => {
                binderCalled = true;
                return Task.FromResult(new Try<string>(s + BoundSuccessValue));
            }
        );

        binderCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task then_async_task_source_task_binder_catches_binder_exception() {
        var source = SuccessSourceTask();
        var result = await source.ThenAsync<string, string>(_ => {
                throw BinderException;
                return Task.FromResult(new Try<string>("unreachable")); // Keep a return to satisfy Task-returning delegate
            }
        );

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(BinderException);
    }

    [Test]
    public async Task then_async_task_source_task_binder_catches_binder_task_exception() {
        var source = SuccessSourceTask();
        var result = await source.ThenAsync<string, string>(_ => Task.FromException<Try<string>>(BinderException));

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(BinderException);
    }

    [Test]
    public async Task then_async_task_source_task_binder_handles_failed_source_task() {
        var source       = FailedSourceTask();
        var binderCalled = false;
        var result = await source.ThenAsync(s => {
                binderCalled = true;
                return Task.FromResult(new Try<string>(s + BoundSuccessValue));
            }
        );

        binderCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(FailedSourceTaskException);
    }

    [Test]
    public async Task then_async_try_source_task_binder_success_path() {
        var source = SuccessSource();
        var result = await source.ThenAsync(s => Task.FromResult(new Try<string>(s + BoundSuccessValue)));

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue + BoundSuccessValue);
    }

    [Test]
    public async Task then_async_try_source_task_binder_propagates_source_error() {
        var source       = ErrorSource();
        var binderCalled = false;
        var result = await source.ThenAsync(s => {
                binderCalled = true;
                return Task.FromResult(new Try<string>(s + BoundSuccessValue));
            }
        );

        binderCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task then_async_try_source_task_binder_catches_binder_exception() {
        var source = SuccessSource();
        var result = await source.ThenAsync<string, string>(_ => {
                throw BinderException;
                return Task.FromResult(new Try<string>("unreachable")); // Keep a return to satisfy Task-returning delegate
            }
        );

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(BinderException);
    }

    #endregion

    #region . MapAsync Tests .

    [Test]
    public async Task map_async_task_source_task_mapper_success_path() {
        var source = SuccessSourceTask();
        var result = await source.MapAsync(s => Task.FromResult(s + MappedSuccessValue));

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue + MappedSuccessValue);
    }

    [Test]
    public async Task map_async_task_source_task_mapper_propagates_source_error() {
        var source       = ErrorSourceTask();
        var mapperCalled = false;
        var result = await source.MapAsync(s => {
                mapperCalled = true;
                return Task.FromResult(s + MappedSuccessValue);
            }
        );

        mapperCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task map_async_task_source_task_mapper_catches_mapper_exception() {
        var source = SuccessSourceTask();
        var result = await source.MapAsync<string, string>(_ => {
                throw MapperException;
                return Task.FromResult("unreachable"); // Keep a return to satisfy Task-returning delegate
            }
        );

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(MapperException);
    }

    [Test]
    public async Task map_async_task_source_task_mapper_catches_mapper_task_exception() {
        var source = SuccessSourceTask();
        var result = await source.MapAsync<string, string>(_ => Task.FromException<string>(MapperException));

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(MapperException);
    }

    [Test]
    public async Task map_async_task_source_task_mapper_handles_failed_source_task() {
        var source       = FailedSourceTask();
        var mapperCalled = false;
        var result = await source.MapAsync(s => {
                mapperCalled = true;
                return Task.FromResult(s + MappedSuccessValue);
            }
        );

        mapperCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(FailedSourceTaskException);
    }

    [Test]
    public async Task map_async_try_source_task_mapper_success_path() {
        var source = SuccessSource();
        var result = await source.MapAsync(s => Task.FromResult(s + MappedSuccessValue));

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue + MappedSuccessValue);
    }

    [Test]
    public async Task map_async_try_source_task_mapper_propagates_source_error() {
        var source       = ErrorSource();
        var mapperCalled = false;
        var result = await source.MapAsync(s => {
                mapperCalled = true;
                return Task.FromResult(s + MappedSuccessValue);
            }
        );

        mapperCalled.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task map_async_try_source_task_mapper_catches_mapper_exception() {
        var source = SuccessSource();
        var result = await source.MapAsync<string, string>(_ => {
                throw MapperException;
                return Task.FromResult("unreachable"); // Keep a return to satisfy Task-returning delegate
            }
        );

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(MapperException);
    }

    #endregion

    #region . OnSuccessAsync Tests .

    [Test]
    public async Task on_success_async_task_source_task_action_executes_on_success() {
        var source         = SuccessSourceTask();
        var actionExecuted = false;
        var result = await source.OnSuccessAsync(s => {
                s.ShouldBe(OriginalSuccessValue);
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue);
    }

    [Test]
    public async Task on_success_async_task_source_task_action_skips_on_error() {
        var source         = ErrorSourceTask();
        var actionExecuted = false;
        var result = await source.OnSuccessAsync(_ => {
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task on_success_async_task_source_task_action_propagates_action_exception() {
        var source = SuccessSourceTask();
        var ex = await Should.ThrowAsync<Exception>(async () => {
                await source.OnSuccessAsync(_ => {
                        throw ActionException;
                        return Task.CompletedTask; // Keep a return to satisfy Task-returning delegate
                    }
                );
            }
        );

        ex.ShouldBe(ActionException);
    }

    [Test]
    public async Task on_success_async_task_source_task_action_propagates_action_task_exception() {
        var source = SuccessSourceTask();
        var ex     = await Should.ThrowAsync<Exception>(async () => { await source.OnSuccessAsync(_ => Task.FromException(ActionException)); });

        ex.ShouldBe(ActionException);
    }

    [Test]
    public async Task on_success_async_task_source_task_action_handles_failed_source_task() {
        var source         = FailedSourceTask();
        var actionExecuted = false;
        var result = await source.OnSuccessAsync(_ => {
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(FailedSourceTaskException);
    }

    #endregion

    #region . OnErrorAsync Tests .

    [Test]
    public async Task on_error_async_task_source_task_action_executes_on_error() {
        var source         = ErrorSourceTask();
        var actionExecuted = false;
        var result = await source.OnErrorAsync(e => {
                e.ShouldBe(TestException);
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeTrue();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task on_error_async_task_source_task_action_skips_on_success() {
        var source         = SuccessSourceTask();
        var actionExecuted = false;
        var result = await source.OnErrorAsync(_ => {
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeFalse();
        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(OriginalSuccessValue);
    }

    [Test]
    public async Task on_error_async_task_source_task_action_propagates_action_exception() {
        var source = ErrorSourceTask();
        var ex = await Should.ThrowAsync<Exception>(async () => {
                await source.OnErrorAsync(_ => {
                        throw ActionException;
                        return Task.CompletedTask; // Keep a return to satisfy Task-returning delegate
                    }
                );
            }
        );

        ex.ShouldBe(ActionException);
    }

    [Test]
    public async Task on_error_async_task_source_task_action_propagates_action_task_exception() {
        var source = ErrorSourceTask();
        var ex     = await Should.ThrowAsync<Exception>(async () => { await source.OnErrorAsync(_ => Task.FromException(ActionException)); });
        ex.ShouldBe(ActionException);
    }

    [Test]
    public async Task on_error_async_task_source_task_action_handles_failed_source_task() {
        var source         = FailedSourceTask();
        var actionExecuted = false;
        var result = await source.OnErrorAsync(_ => {
                actionExecuted = true;
                return Task.CompletedTask;
            }
        );

        actionExecuted.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(FailedSourceTaskException);
    }

    #endregion

    #region . Chaining Tests .

    [Test]
    public async Task can_chain_multiple_async_try_operations_successfully() {
        var result = await SuccessSourceTask("start")
            .MapAsync(s => Task.FromResult(s + "-mapped1"))
            .ThenAsync(s => Task.FromResult(new Try<string>(s + "-bound1")))
            .OnSuccessAsync(s => {
                    s.ShouldBe("start-mapped1-bound1");
                    return Task.CompletedTask;
                }
            )
            .MapAsync(s => Task.FromResult(s + "-mapped2"));

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe("start-mapped1-bound1-mapped2");
    }

    [Test]
    public async Task chaining_short_circuits_on_first_error_in_map() {
        var map2Called = false;
        var result = await SuccessSourceTask("start")
            .MapAsync<string, string>(_ => Task.FromException<string>(TestException)) // Fails here
            .ThenAsync(s => Task.FromResult(new Try<string>(s + "-bound1")))
            .MapAsync(s => {
                    map2Called = true;
                    return Task.FromResult(s + "-mapped2");
                }
            );

        map2Called.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    [Test]
    public async Task chaining_short_circuits_on_first_error_in_then() {
        var map2Called = false;
        var result = await SuccessSourceTask("start")
            .MapAsync(s => Task.FromResult(s + "-mapped1"))
            .ThenAsync<string, string>(_ => Task.FromException<Try<string>>(TestException)) // Fails here
            .MapAsync(s => {
                    map2Called = true;
                    return Task.FromResult(s + "-mapped2");
                }
            );

        map2Called.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(TestException);
    }

    #endregion

    #region . Cancellation Tests .

    [Test]
    public async Task then_async_task_source_task_binder_respects_cancellation_before_source() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Be explicit with the delegate type to resolve ambiguity
        Func<string, Task<Try<string>>> binder = s => Task.FromResult(new Try<string>(s));
        await Should.ThrowAsync<OperationCanceledException>(SuccessSourceTask().ThenAsync(binder, cts.Token));
    }

    [Test]
    public async Task then_async_task_source_task_binder_respects_cancellation_before_binder() {
        using var cts = new CancellationTokenSource();
        var source = SuccessSourceTask().ContinueWith(t => {
                cts.Cancel(); // Cancel after source completes but before binder runs
                return t.Result;
            }
        );

        // Be explicit with the delegate type to resolve ambiguity
        Func<string, Task<Try<string>>> binder = s => Task.FromResult(new Try<string>(s + BoundSuccessValue));
        await Should.ThrowAsync<OperationCanceledException>(source.ThenAsync(binder, cts.Token));
    }

    [Test]
    public async Task map_async_task_source_task_mapper_respects_cancellation_before_source() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Be explicit with the delegate type to resolve ambiguity
        Func<string, Task<string>> mapper = s => Task.FromResult(s);
        await Should.ThrowAsync<OperationCanceledException>(SuccessSourceTask().MapAsync(mapper, cts.Token));
    }

    [Test]
    public async Task on_success_async_task_source_task_action_respects_cancellation_before_source() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Be explicit with the delegate type to resolve ambiguity
        Func<string, Task> action = _ => Task.CompletedTask;
        await Should.ThrowAsync<OperationCanceledException>(SuccessSourceTask().OnSuccessAsync(action, cts.Token));
    }

    [Test]
    public async Task on_error_async_task_source_task_action_respects_cancellation_before_source() {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Be explicit with the delegate type to resolve ambiguity
        Func<Exception, Task> action = _ => Task.CompletedTask;
        await Should.ThrowAsync<OperationCanceledException>(ErrorSourceTask().OnErrorAsync(action, cts.Token));
    }

    [Test]
    public async Task then_async_task_source_task_binder_respects_cancellation_during_binder() {
        using var cts    = new CancellationTokenSource();
        var       source = SuccessSourceTask();

        // Explicitly type the delegate to resolve ambiguity
        Func<string, Task<Try<string>>> binder = async s => {
            await Task.Delay(500, cts.Token); // Simulate work that respects cancellation
            return new Try<string>(s + BoundSuccessValue);
        };

        var binderTask = source.ThenAsync(binder, cts.Token);

        // Cancel shortly after starting
        cts.CancelAfter(50);

        await Should.ThrowAsync<OperationCanceledException>(binderTask);
    }

    #endregion
}
