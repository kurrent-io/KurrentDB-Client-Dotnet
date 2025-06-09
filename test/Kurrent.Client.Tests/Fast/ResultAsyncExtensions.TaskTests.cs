using KurrentDB.Client;
using KurrentDB.Client.V2;

namespace Kurrent.Client.Tests.Fast;

public class ResultAsyncExtensionsTaskTests : ResultAsyncExtensionsTestFixture {
    #region . ThenAsync .

    [Test]
    public async Task returns_success_when_task_source_is_success_and_task_result_binder_returns_success() {
        var                                     source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<Result<string, string>>> binder = val => Task.FromResult(Result<string, string>.Success(val.ToString()));

        var result = await source.ThenAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue.ToString());
    }

    [Test]
    public async Task returns_error_when_task_source_is_success_and_task_result_binder_returns_error() {
        var                                     source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<Result<string, string>>> binder = _ => Task.FromResult(Result<string, string>.Error(AnotherError));

        var result = await source.ThenAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(AnotherError);
    }

    [Test]
    public async Task propagates_error_when_task_source_is_error_and_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var binderCalled = false;
        Func<int, Task<Result<string, string>>> binder = _ => {
            binderCalled = true;
            return Task.FromResult(Result<string, string>.Success(MappedStringSuccessValue));
        };

        var result = await source.ThenAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_result_binder_is_null_for_task_source() {
        var                                     source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<Result<string, string>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.ThenAsync(binder));
    }

    [Test]
    public async Task returns_success_with_state_when_task_source_is_success_and_task_result_binder_returns_success() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task<Result<string, string>>> binder = (val, state) => Task.FromResult(Result<string, string>.Success((val + state).ToString()));

        var result = await source.ThenAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe((InitialSuccessValue + StateValue).ToString());
    }

    [Test]
    public async Task returns_error_with_state_when_task_source_is_success_and_task_result_binder_returns_error() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task<Result<string, string>>> binder = (_, _) => Task.FromResult(Result<string, string>.Error(AnotherError));

        var result = await source.ThenAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(AnotherError);
    }

    [Test]
    public async Task propagates_error_with_state_when_task_source_is_error_and_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var binderCalled = false;
        Func<int, int, Task<Result<string, string>>> binder = (_, _) => {
            binderCalled = true;
            return Task.FromResult(Result<string, string>.Success(MappedStringSuccessValue));
        };

        var result = await source.ThenAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_result_binder_with_state_is_null_for_task_source() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task<Result<string, string>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.ThenAsync(binder, StateValue));
    }

    [Test]
    public async Task returns_success_when_task_source_is_success_and_value_task_result_binder_returns_success() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, ValueTask<Result<string, string>>> binder = val => new ValueTask<Result<string, string>>(Result<string, string>.Success(val.ToString()));

        var result = await source.ThenAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue.ToString());
    }

    [Test]
    public async Task returns_error_when_task_source_is_success_and_value_task_result_binder_returns_error() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, ValueTask<Result<string, string>>> binder = _ => new ValueTask<Result<string, string>>(Result<string, string>.Error(AnotherError));

        var result = await source.ThenAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(AnotherError);
    }

    [Test]
    public async Task propagates_error_when_task_source_is_error_and_value_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var binderCalled = false;
        Func<int, ValueTask<Result<string, string>>> binder = _ => {
            binderCalled = true;
            return new ValueTask<Result<string, string>>(Result<string, string>.Success(MappedStringSuccessValue));
        };

        var result = await source.ThenAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_result_binder_is_null_for_task_source() {
        var                                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, ValueTask<Result<string, string>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.ThenAsync(binder));
    }

    [Test]
    public async Task returns_success_with_state_when_task_source_is_success_and_value_task_result_binder_returns_success() {
        var                                               source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, ValueTask<Result<string, string>>> binder = (val, state) => new ValueTask<Result<string, string>>(Result<string, string>.Success((val + state).ToString()));

        var result = await source.ThenAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe((InitialSuccessValue + StateValue).ToString());
    }

    [Test]
    public async Task returns_error_with_state_when_task_source_is_success_and_value_task_result_binder_returns_error() {
        var                                               source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, ValueTask<Result<string, string>>> binder = (_, _) => new ValueTask<Result<string, string>>(Result<string, string>.Error(AnotherError));

        var result = await source.ThenAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(AnotherError);
    }

    [Test]
    public async Task propagates_error_with_state_when_task_source_is_error_and_value_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var binderCalled = false;
        Func<int, int, ValueTask<Result<string, string>>> binder = (_, _) => {
            binderCalled = true;
            return new ValueTask<Result<string, string>>(Result<string, string>.Success(MappedStringSuccessValue));
        };

        var result = await source.ThenAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_result_binder_with_state_is_null_for_task_source() {
        var                                               source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, ValueTask<Result<string, string>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.ThenAsync(binder, StateValue));
    }

    #endregion

    #region . MapAsync .

    [Test]
    public async Task returns_mapped_success_when_task_source_is_success_and_task_mapper_is_used() {
        var                     source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<string>> mapper = val => Task.FromResult(val.ToString());

        var result = await source.MapAsync(mapper);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue.ToString());
    }

    [Test]
    public async Task propagates_error_when_task_source_is_error_and_task_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var mapperCalled = false;
        Func<int, Task<string>> mapper = _ => {
            mapperCalled = true;
            return Task.FromResult(MappedStringSuccessValue);
        };

        var result = await source.MapAsync(mapper);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_mapper_is_null_for_task_source() {
        var                     source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<string>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapAsync(mapper));
    }

    [Test]
    public async Task returns_mapped_success_with_state_when_task_source_is_success_and_task_mapper_is_used() {
        var                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task<string>> mapper = (val, state) => Task.FromResult((val + state).ToString());

        var result = await source.MapAsync(mapper, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe((InitialSuccessValue + StateValue).ToString());
    }

    [Test]
    public async Task propagates_error_with_state_when_task_source_is_error_and_task_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var mapperCalled = false;
        Func<int, int, Task<string>> mapper = (_, _) => {
            mapperCalled = true;
            return Task.FromResult(MappedStringSuccessValue);
        };

        var result = await source.MapAsync(mapper, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_mapper_with_state_is_null_for_task_source() {
        var                          source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task<string>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapAsync(mapper, StateValue));
    }

    [Test]
    public async Task returns_mapped_success_when_task_source_is_success_and_value_task_mapper_is_used() {
        var source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));

        Func<int, Task<string>> mapper = val => Task.FromResult(val.ToString());

        var result = await source.MapAsync(mapper);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue.ToString());
    }

    [Test]
    public async Task propagates_error_when_task_source_is_error_and_value_task_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var mapperCalled = false;

        Func<int, Task<string>> mapper = _ => {
            mapperCalled = true;
            return Task.FromResult(MappedStringSuccessValue);
        };

        var result = await source.MapAsync(mapper);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_mapper_is_null_for_task_source() {
        var source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));

        Func<int, Task<string>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapAsync(mapper));
    }

    [Test]
    public async Task returns_mapped_success_with_state_when_task_source_is_success_and_value_task_mapper_is_used() {
        var source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));

        Func<int, int, Task<string>> mapper = (val, state) => Task.FromResult((val + state).ToString());

        var result = await source.MapAsync(mapper, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe((InitialSuccessValue + StateValue).ToString());
    }

    [Test]
    public async Task propagates_error_with_state_when_task_source_is_error_and_value_task_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Error(DefaultError));
        var mapperCalled = false;

        Func<int, int, Task<string>> mapper = (_, _) => {
            mapperCalled = true;
            return Task.FromResult(MappedStringSuccessValue);
        };

        var result = await source.MapAsync(mapper, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_mapper_with_state_is_null_for_task_source() {
        var source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));

        Func<int, int, Task<string>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapAsync(mapper, StateValue));
    }

    #endregion

    #region . OrElseAsync .

    [Test]
    public async Task returns_success_from_binder_when_task_source_is_error_and_task_result_binder_returns_success() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = _ => Task.FromResult(Result<string, int>.Success(MappedSuccessValue));

        var result = await source.OrElseAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(MappedSuccessValue);
    }

    [Test]
    public async Task returns_error_from_binder_when_task_source_is_error_and_task_result_binder_returns_error() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = err => Task.FromResult(Result<string, int>.Error(err + " bound"));

        var result = await source.OrElseAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError + " bound");
    }

    [Test]
    public async Task propagates_success_when_task_source_is_success_and_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var binderCalled = false;
        Func<string, Task<Result<string, int>>> binder = _ => {
            binderCalled = true;
            return Task.FromResult(Result<string, int>.Error(AnotherError));
        };

        var result = await source.OrElseAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_result_binder_is_null_for_orelse_on_task_source() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OrElseAsync(binder));
    }

    [Test]
    public async Task returns_success_from_binder_with_state_when_task_source_is_error_and_task_result_binder_returns_success() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = (_, state) => Task.FromResult(Result<string, int>.Success(MappedSuccessValue + state));

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(MappedSuccessValue + StateValue);
    }

    [Test]
    public async Task returns_error_from_binder_with_state_when_task_source_is_error_and_task_result_binder_returns_error() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = (err, state) => Task.FromResult(Result<string, int>.Error(err + " bound " + state));

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError + " bound " + StateValue);
    }

    [Test]
    public async Task propagates_success_with_state_when_task_source_is_success_and_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var binderCalled = false;
        Func<string, int, Task<Result<string, int>>> binder = (_, _) => {
            binderCalled = true;
            return Task.FromResult(Result<string, int>.Error(AnotherError));
        };

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_result_binder_with_state_is_null_for_orelse_on_task_source() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OrElseAsync(binder, StateValue));
    }

    [Test]
    public async Task returns_success_from_binder_when_task_source_is_error_and_value_task_result_binder_returns_success() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = _ => Task.FromResult(Result<string, int>.Success(MappedSuccessValue));

        var result = await source.OrElseAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(MappedSuccessValue);
    }

    [Test]
    public async Task returns_error_from_binder_when_task_source_is_error_and_value_task_result_binder_returns_error() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = err => Task.FromResult(Result<string, int>.Error(err + " bound"));

        var result = await source.OrElseAsync(binder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError + " bound");
    }

    [Test]
    public async Task propagates_success_when_task_source_is_success_and_value_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var binderCalled = false;
        Func<string, Task<Result<string, int>>> binder = _ => {
            binderCalled = true;
            return Task.FromResult(Result<string, int>.Error(AnotherError));
        };

        var result = await source.OrElseAsync(binder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_result_binder_is_null_for_orelse_on_task_source() {
        var                                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<Result<string, int>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OrElseAsync(binder));
    }

    [Test]
    public async Task returns_success_from_binder_with_state_when_task_source_is_error_and_value_task_result_binder_returns_success() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = (_, state) => Task.FromResult(Result<string, int>.Success(MappedSuccessValue + state));

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(MappedSuccessValue + StateValue);
    }

    [Test]
    public async Task returns_error_from_binder_with_state_when_task_source_is_error_and_value_task_result_binder_returns_error() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = (err, state) => Task.FromResult(Result<string, int>.Error(err + " bound " + state));

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError + " bound " + StateValue);
    }

    [Test]
    public async Task propagates_success_with_state_when_task_source_is_success_and_value_task_result_binder_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var binderCalled = false;
        Func<string, int, Task<Result<string, int>>> binder = (_, _) => {
            binderCalled = true;
            return Task.FromResult(Result<string, int>.Error(AnotherError));
        };

        var result = await source.OrElseAsync(binder, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        binderCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_result_binder_with_state_is_null_for_orelse_on_task_source() {
        var                                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<Result<string, int>>> binder = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OrElseAsync(binder, StateValue));
    }

    #endregion

    #region . MapErrorAsync .

    [Test]
    public async Task returns_mapped_error_when_task_source_is_error_and_task_mapper_is_used() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<int>> mapper = err => Task.FromResult(err.Length);

        var result = await source.MapErrorAsync<string, int, int>(mapper);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError.Length);
    }

    [Test]
    public async Task propagates_success_when_task_source_is_success_and_task_error_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var mapperCalled = false;
        Func<string, Task<int>> mapper = _ => {
            mapperCalled = true;
            return Task.FromResult(0);
        };

        var result = await source.MapErrorAsync<string, int, int>(mapper);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_error_mapper_is_null_for_task_source() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<int>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapErrorAsync<string, int, int>(mapper));
    }

    [Test]
    public async Task returns_mapped_error_with_state_when_task_source_is_error_and_task_mapper_is_used() {
        var                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<int>> mapper = (err, state) => Task.FromResult(err.Length + state);

        var result = await source.MapErrorAsync<string, int, int, int>(mapper, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError.Length + StateValue);
    }

    [Test]
    public async Task propagates_success_with_state_when_task_source_is_success_and_task_error_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var mapperCalled = false;
        Func<string, int, Task<int>> mapper = (_, _) => {
            mapperCalled = true;
            return Task.FromResult(0);
        };

        var result = await source.MapErrorAsync<string, int, int, int>(mapper, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_error_mapper_with_state_is_null_for_task_source() {
        var                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<int>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapErrorAsync<string, int, int, int>(mapper, StateValue));
    }

    [Test]
    public async Task returns_mapped_error_when_task_source_is_error_and_value_task_mapper_is_used() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<int>> mapper = err => Task.FromResult(err.Length);

        var result = await source.MapErrorAsync<string, int, int>(mapper);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError.Length);
    }

    [Test]
    public async Task propagates_success_when_task_source_is_success_and_value_task_error_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var mapperCalled = false;
        Func<string, Task<int>> mapper = _ => {
            mapperCalled = true;
            return Task.FromResult(0);
        };

        var result = await source.MapErrorAsync<string, int, int>(mapper);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_error_mapper_is_null_for_task_source() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task<int>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapErrorAsync<string, int, int>(mapper));
    }

    [Test]
    public async Task returns_mapped_error_with_state_when_task_source_is_error_and_value_task_mapper_is_used() {
        var                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<int>> mapper = (err, state) => Task.FromResult(err.Length + state);

        var result = await source.MapErrorAsync<string, int, int, int>(mapper, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError.Length + StateValue);
    }

    [Test]
    public async Task propagates_success_with_state_when_task_source_is_success_and_value_task_error_mapper_is_not_called() {
        var source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var mapperCalled = false;
        Func<string, int, Task<int>> mapper = (_, _) => {
            mapperCalled = true;
            return Task.FromResult(0);
        };

        var result = await source.MapErrorAsync<string, int, int, int>(mapper, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        mapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_error_mapper_with_state_is_null_for_task_source() {
        var                          source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task<int>> mapper = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.MapErrorAsync<string, int, int, int>(mapper, StateValue));
    }

    #endregion

    #region . OnSuccessAsync .

    [Test]
    public async Task executes_task_action_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<int, Task> action = val => {
            val.ShouldBe(InitialSuccessValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_task_action_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<int, Task> action = _ => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_action_is_null_for_onsuccess_on_task_source() {
        var             source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnSuccessAsync(action));
    }

    [Test]
    public async Task executes_task_action_with_state_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<int, int, Task> action = (val, state) => {
            val.ShouldBe(InitialSuccessValue);
            state.ShouldBe(StateValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_task_action_with_state_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<int, int, Task> action = (_, _) => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_action_with_state_is_null_for_onsuccess_on_task_source() {
        var                  source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnSuccessAsync(action, StateValue));
    }

    [Test]
    public async Task executes_value_task_action_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<int, Task> action = val => {
            val.ShouldBe(InitialSuccessValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_value_task_action_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<int, Task> action = _ => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_action_is_null_for_onsuccess_on_task_source() {
        var             source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnSuccessAsync(action));
    }

    [Test]
    public async Task executes_value_task_action_with_state_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<int, int, Task> action = (val, state) => {
            val.ShouldBe(InitialSuccessValue);
            state.ShouldBe(StateValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_value_task_action_with_state_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<int, int, Task> action = (_, _) => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnSuccessAsync(action, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_action_with_state_is_null_for_onsuccess_on_task_source() {
        var                  source = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnSuccessAsync(action, StateValue));
    }

    #endregion

    #region . OnErrorAsync .

    [Test]
    public async Task executes_task_action_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<string, Task> action = err => {
            err.ShouldBe(DefaultError);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_task_action_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<string, Task> action = _ => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_action_is_null_for_onerror_on_task_source() {
        var                source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnErrorAsync(action));
    }

    [Test]
    public async Task executes_task_action_with_state_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<string, int, Task> action = (err, state) => {
            err.ShouldBe(DefaultError);
            state.ShouldBe(StateValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_task_action_with_state_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<string, int, Task> action = (_, _) => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_task_action_with_state_is_null_for_onerror_on_task_source() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnErrorAsync(action, StateValue));
    }

    [Test]
    public async Task executes_value_task_action_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<string, Task> action = err => {
            err.ShouldBe(DefaultError);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_value_task_action_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<string, Task> action = _ => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_action_is_null_for_onerror_on_task_source() {
        var                source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnErrorAsync(action));
    }

    [Test]
    public async Task executes_value_task_action_with_state_when_task_source_is_error() {
        var source         = Task.FromResult(Result<string, int>.Error(DefaultError));
        var actionExecuted = false;
        Func<string, int, Task> action = (err, state) => {
            err.ShouldBe(DefaultError);
            state.ShouldBe(StateValue);
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action, StateValue);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        actionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task does_not_execute_value_task_action_with_state_when_task_source_is_success() {
        var source         = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var actionExecuted = false;
        Func<string, int, Task> action = (_, _) => {
            actionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source.OnErrorAsync(action, StateValue);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue);
        actionExecuted.ShouldBeFalse();
    }

    [Test]
    public async Task throws_argument_null_exception_when_value_task_action_with_state_is_null_for_onerror_on_task_source() {
        var                     source = Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, int, Task> action = null!;

        await Should.ThrowAsync<ArgumentNullException>(async () => await source.OnErrorAsync(action, StateValue));
    }

    #endregion

    #region . Chained Operations .

    [Test]
    public async Task returns_mapped_success_when_task_source_then_async_map_async_succeeds() {
        var                                     source     = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<Result<string, double>>> thenBinder = val => Task.FromResult(Result<string, double>.Success(val / 2.0));
        // Changed ValueTask to Task for mapMapper to avoid compiler issue
        Func<double, Task<string>> mapMapper = val => Task.FromResult(val.ToString("F1"));

        var result = await source
            .ThenAsync(thenBinder)
            .MapAsync(mapMapper);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe((InitialSuccessValue / 2.0).ToString("F1"));
    }

    [Test]
    public async Task propagates_error_when_task_source_then_async_fails_before_map_async() {
        var                                          source          = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, ValueTask<Result<string, double>>> thenBinder      = _ => new ValueTask<Result<string, double>>(Result<string, double>.Error(DefaultError));
        var                                          mapMapperCalled = false;
        Func<double, Task<string>> mapMapper = _ => {
            mapMapperCalled = true;
            return Task.FromResult("mapped");
        };

        var result = await source
            .ThenAsync(thenBinder)
            .MapAsync(mapMapper);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        mapMapperCalled.ShouldBeFalse();
    }

    [Test]
    public async Task returns_success_from_orelse_when_task_source_then_async_fails_and_orelse_succeeds() {
        var                                     source     = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, Task<Result<string, string>>> thenBinder = _ => Task.FromResult(Result<string, string>.Error(DefaultError));
        // Changed ValueTask to Task for orElseBinder to avoid compiler issue
        Func<string, Task<Result<string, string>>> orElseBinder = err => Task.FromResult(Result<string, string>.Success($"recovered from {err}"));

        var result = await source
            .ThenAsync(thenBinder)
            .OrElseAsync(orElseBinder);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe($"recovered from {DefaultError}");
    }

    [Test]
    public async Task propagates_error_from_orelse_when_task_source_then_async_fails_and_orelse_also_fails() {
        var                                          source       = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        Func<int, ValueTask<Result<string, string>>> thenBinder   = _ => new ValueTask<Result<string, string>>(Result<string, string>.Error(DefaultError));
        Func<string, Task<Result<string, string>>>   orElseBinder = _ => Task.FromResult(Result<string, string>.Error(AnotherError));

        var result = await source
            .ThenAsync(thenBinder)
            .OrElseAsync(orElseBinder);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(AnotherError);
    }

    [Test]
    public async Task executes_onsuccess_actions_when_task_source_chain_succeeds() {
        var source               = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var firstActionExecuted  = false;
        var secondActionExecuted = false;

        Func<int, Task> firstAction = val => {
            val.ShouldBe(InitialSuccessValue);
            firstActionExecuted = true;
            return Task.CompletedTask;
        };

        Func<int, ValueTask<Result<string, int>>> thenBinder = val => new ValueTask<Result<string, int>>(Result<string, int>.Success(val * 2));
        Func<int, Task> secondAction = val => {
            val.ShouldBe(InitialSuccessValue * 2);
            secondActionExecuted = true;
            return Task.CompletedTask;
        };

        var result = await source
            .OnSuccessAsync(firstAction)
            .ThenAsync(thenBinder)
            .OnSuccessAsync(secondAction);

        result.IsSuccess.ShouldBeTrue();
        result.SuccessValue().ShouldBe(InitialSuccessValue * 2);
        firstActionExecuted.ShouldBeTrue();
        secondActionExecuted.ShouldBeTrue();
    }

    [Test]
    public async Task executes_onerror_action_and_propagates_error_when_task_source_chain_fails_midway() {
        var source                          = Task.FromResult(Result<string, int>.Success(InitialSuccessValue));
        var onErrorActionExecuted           = false;
        var onSuccessActionAfterErrorCalled = false;

        Func<int, Task<Result<string, int>>> thenBinderFails = _ => Task.FromResult(Result<string, int>.Error(DefaultError));
        Func<string, Task> onErrorAction = err => {
            err.ShouldBe(DefaultError);
            onErrorActionExecuted = true;
            return Task.CompletedTask;
        };

        Func<int, Task> onSuccessAction = _ => {
            onSuccessActionAfterErrorCalled = true;
            return Task.CompletedTask;
        };

        var result = await source
            .ThenAsync(thenBinderFails)
            .OnErrorAsync(onErrorAction)
            .OnSuccessAsync(onSuccessAction);

        result.IsError.ShouldBeTrue();
        result.ErrorValue().ShouldBe(DefaultError);
        onErrorActionExecuted.ShouldBeTrue();
        onSuccessActionAfterErrorCalled.ShouldBeFalse();
    }

    #endregion
}
