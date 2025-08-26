using Kurrent.Client.Streams;
using Kurrent.Client.Testing.Sample;
using Kurrent.Client.Testing.TUnit;

namespace Kurrent.Client.Tests.Streams;

[Category("Streams")]
public class AppendLoadTests : KurrentClientTestFixture {
    public class CreatesMultipleStreamsTransactionallyState : TestCaseGenerator<int> {
        protected override IEnumerable<int> Data() => [
            2, 5, 6, 8, 10, 15, 20, 25, 30, 50, 100
        ];
    }

    [Test]
    [CreatesMultipleStreamsTransactionallyState]
    public async Task creates_multiple_streams_transactionally(int streams, CancellationToken ct) {
        var requests = GenerateMultipleCreateGameRequests(streams);

        await AutomaticClient.Streams
            .Append(requests, ct)
            .ShouldNotThrowAsync()
            .OnFailureAsync(failures => {
                failures.Count.ShouldBeGreaterThan(0);
                Should.NotThrow(() => failures.FirstOrDefault().Error.Throw());
            })
            .OnSuccessAsync(success => {
                success.Count.ShouldBe(requests.Count);

                for (var i = 0; i < requests.Count; i++) {
                    var request = requests[i];
                    var result  = success[i];

                    var expectedRevision = StreamRevision.From(request.Messages.Count() - 1);

                    result.Stream.ShouldBe(request.Stream);
                    result.StreamRevision.ShouldBe(expectedRevision);
                    result.Position.ShouldBeGreaterThan(LogPosition.Earliest);
                }
            });
    }
}
