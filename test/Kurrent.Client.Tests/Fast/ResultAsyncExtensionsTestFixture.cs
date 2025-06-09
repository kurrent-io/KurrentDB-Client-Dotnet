using KurrentDB.Client;

namespace Kurrent.Client.Tests.Fast;

public abstract class ResultAsyncExtensionsTestFixture {
    protected const string DefaultError              = "Operation failed";
    protected const string AnotherError              = "Another failure";
    protected const int    InitialSuccessValue       = 10;
    protected const int    MappedSuccessValue        = 20;
    protected const string InitialStringSuccessValue = "initial";
    protected const string MappedStringSuccessValue  = "mapped";
    protected const int    StateValue                = 5;

    protected static Task<Result<string, int>> CreateSuccessTask(int value) => Task.FromResult(Result<string, int>.Success(value));

    protected static Task<Result<string, int>> CreateErrorTask(string error) => Task.FromResult(Result<string, int>.Error(error));

    protected static Task<Result<string, string>> CreateStringSuccessTask(string value) => Task.FromResult(Result<string, string>.Success(value));

    protected static Task<Result<string, string>> CreateStringErrorTask(string error) => Task.FromResult(Result<string, string>.Error(error));

    protected static ValueTask<Result<string, int>> CreateSuccessValueTask(int value) => new(Result<string, int>.Success(value));

    protected static ValueTask<Result<string, int>> CreateErrorValueTask(string error) => new(Result<string, int>.Error(error));

    protected static ValueTask<Result<string, string>> CreateStringSuccessValueTask(string value) => new(Result<string, string>.Success(value));

    protected static ValueTask<Result<string, string>> CreateStringErrorValueTask(string error) => new(Result<string, string>.Error(error));
}
