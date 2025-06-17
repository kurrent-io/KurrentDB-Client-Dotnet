namespace Kurrent.Client.Tests.Infra.Result;

public class ResultCollectionsTests
{
    [Test]
    public void sequence_returns_success_of_enumerable_when_all_results_are_success()
    {
        // Arrange
        var successResults = new[]
        {
            Result<int, string>.AsObsoleteSuccess(1),
            Result<int, string>.AsObsoleteSuccess(2),
            Result<int, string>.AsObsoleteSuccess(3)
        };

        // Act
        var finalResult = successResults.Sequence();

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void sequence_returns_first_error_when_any_result_is_an_error()
    {
        // Arrange
        var mixedResults = new[]
        {
            Result<int, string>.AsObsoleteSuccess(1),
            Result<int, string>.AsObsoleteError("First error"),
            Result<int, string>.AsObsoleteError("Second error")
        };

        // Act
        var finalResult = mixedResults.Sequence();

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe("First error");
    }

    [Test]
    public void sequence_returns_success_with_empty_enumerable_when_input_is_empty()
    {
        // Arrange
        var emptyResults = Enumerable.Empty<Result<int, string>>();

        // Act
        var finalResult = emptyResults.Sequence();

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBeEmpty();
    }

    [Test]
    public void traverse_returns_success_of_enumerable_when_all_mappings_succeed()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        Result<int, string> Map(int i) => Result<int, string>.AsObsoleteSuccess(i * 2);

        // Act
        var finalResult = source.Traverse(Map);

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBe(new[] { 2, 4, 6 });
    }

    [Test]
    public void traverse_returns_first_error_when_any_mapping_fails()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        Result<int, string> Map(int i) => i == 2 ? Result<int, string>.AsObsoleteError("Error on 2") : Result<int, string>.AsObsoleteSuccess(i);

        // Act
        var finalResult = source.Traverse(Map);

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe("Error on 2");
    }

    [Test]
    public async Task sequence_async_returns_success_when_all_tasks_succeed()
    {
        // Arrange
        var tasks = new[]
        {
            ValueTask.FromResult(Result<int, string>.AsObsoleteSuccess(1)),
            ValueTask.FromResult(Result<int, string>.AsObsoleteSuccess(2))
        };

        // Act
        var finalResult = await tasks.SequenceAsync();

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public async Task sequence_async_returns_first_error_when_any_task_fails()
    {
        // Arrange
        var tasks = new[]
        {
            ValueTask.FromResult(Result<int, string>.AsObsoleteSuccess(1)),
            ValueTask.FromResult(Result<int, string>.AsObsoleteError("First error")),
            ValueTask.FromResult(Result<int, string>.AsObsoleteError("Second error"))
        };

        // Act
        var finalResult = await tasks.SequenceAsync();

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe("First error");
    }

    [Test]
    public async Task traverse_async_returns_success_when_all_async_mappings_succeed()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        ValueTask<Result<int, string>> MapAsync(int i) => ValueTask.FromResult(Result<int, string>.AsObsoleteSuccess(i * 2));

        // Act
        var finalResult = await source.TraverseAsync(MapAsync);

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value.ShouldBe(new[] { 2, 4, 6 });
    }

    [Test]
    public async Task traverse_async_returns_first_error_when_any_async_mapping_fails()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        ValueTask<Result<int, string>> MapAsync(int i) => i == 2
            ? ValueTask.FromResult(Result<int, string>.AsObsoleteError("Async error on 2"))
            : ValueTask.FromResult(Result<int, string>.AsObsoleteSuccess(i));

        // Act
        var finalResult = await source.TraverseAsync(MapAsync);

        // Assert
        finalResult.IsFailure.ShouldBeTrue();
        finalResult.Error.ShouldBe("Async error on 2");
    }
}
