namespace Kurrent.Client.Tests.Infra.Result;

public class ResultCollectionsTests
{
    [Test]
    public void sequence_returns_success_of_enumerable_when_all_results_are_success()
    {
        // Arrange
        var successResults = new[]
        {
            Kurrent.Client.Result<int, string>.Success(1),
            Kurrent.Client.Result<int, string>.Success(2),
            Kurrent.Client.Result<int, string>.Success(3)
        };

        // Act
        var finalResult = successResults.Sequence();

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.AsSuccess.ShouldBe([1, 2, 3]);
    }

    [Test]
    public void sequence_returns_first_error_when_any_result_is_an_error()
    {
        // Arrange
        var mixedResults = new[]
        {
            Kurrent.Client.Result<int, string>.Success(1),
            Kurrent.Client.Result<int, string>.Error("First error"),
            Kurrent.Client.Result<int, string>.Error("Second error")
        };

        // Act
        var finalResult = mixedResults.Sequence();

        // Assert
        finalResult.IsError.ShouldBeTrue();
        finalResult.AsError.ShouldBe("First error");
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
        finalResult.AsSuccess.ShouldBeEmpty();
    }

    [Test]
    public void traverse_returns_success_of_enumerable_when_all_mappings_succeed()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        Result<int, string> Map(int i) => Kurrent.Client.Result<int, string>.Success(i * 2);

        // Act
        var finalResult = source.Traverse(Map);

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.AsSuccess.ShouldBe(new[] { 2, 4, 6 });
    }

    [Test]
    public void traverse_returns_first_error_when_any_mapping_fails()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        Result<int, string> Map(int i) => i == 2 ? Kurrent.Client.Result<int, string>.Error("Error on 2") : Kurrent.Client.Result<int, string>.Success(i);

        // Act
        var finalResult = source.Traverse(Map);

        // Assert
        finalResult.IsError.ShouldBeTrue();
        finalResult.AsError.ShouldBe("Error on 2");
    }

    [Test]
    public async Task sequence_async_returns_success_when_all_tasks_succeed()
    {
        // Arrange
        var tasks = new[]
        {
            ValueTask.FromResult(Kurrent.Client.Result<int, string>.Success(1)),
            ValueTask.FromResult(Kurrent.Client.Result<int, string>.Success(2))
        };

        // Act
        var finalResult = await tasks.SequenceAsync();

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.AsSuccess.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public async Task sequence_async_returns_first_error_when_any_task_fails()
    {
        // Arrange
        var tasks = new[]
        {
            ValueTask.FromResult(Kurrent.Client.Result<int, string>.Success(1)),
            ValueTask.FromResult(Kurrent.Client.Result<int, string>.Error("First error")),
            ValueTask.FromResult(Kurrent.Client.Result<int, string>.Error("Second error"))
        };

        // Act
        var finalResult = await tasks.SequenceAsync();

        // Assert
        finalResult.IsError.ShouldBeTrue();
        finalResult.AsError.ShouldBe("First error");
    }

    [Test]
    public async Task traverse_async_returns_success_when_all_async_mappings_succeed()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        ValueTask<Result<int, string>> MapAsync(int i) => ValueTask.FromResult(Kurrent.Client.Result<int, string>.Success(i * 2));

        // Act
        var finalResult = await source.TraverseAsync(MapAsync);

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.AsSuccess.ShouldBe(new[] { 2, 4, 6 });
    }

    [Test]
    public async Task traverse_async_returns_first_error_when_any_async_mapping_fails()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };
        ValueTask<Result<int, string>> MapAsync(int i) => i == 2
            ? ValueTask.FromResult(Kurrent.Client.Result<int, string>.Error("Async error on 2"))
            : ValueTask.FromResult(Kurrent.Client.Result<int, string>.Success(i));

        // Act
        var finalResult = await source.TraverseAsync(MapAsync);

        // Assert
        finalResult.IsError.ShouldBeTrue();
        finalResult.AsError.ShouldBe("Async error on 2");
    }
}
