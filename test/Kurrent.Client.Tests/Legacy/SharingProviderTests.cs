#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using KurrentDB.Client;

namespace Kurrent.Client.Tests.Legacy;

[Category("Legacy")]
public class SharingProviderTests {
    [Test, Retry(3)]
    public async Task can_get_current() {
        using var sut = new SharingProvider<int, int>(
            async (x, _) => x + 1,
            TimeSpan.FromSeconds(0),
            5
        );

        (await sut.CurrentAsync).ShouldBe(6);
    }

    [Test, Retry(3)]
    public async Task can_reset() {
        var count = 0;
        using var sut = new SharingProvider<bool, int>(
            async (_, _) => count++,
            TimeSpan.FromSeconds(0),
            true
        );

        (await sut.CurrentAsync).ShouldBe(0);
        sut.Reset();
        (await sut.CurrentAsync).ShouldBe(1);
    }

    [Test, Retry(3)]
    public async Task can_return_broken() {
        Action<bool>? onBroken = null;
        var           count    = 0;
        using var sut = new SharingProvider<bool, int>(
            async (_, f) => {
                onBroken = f;
                return count++;
            },
            TimeSpan.FromSeconds(0),
            true
        );

        (await sut.CurrentAsync).ShouldBe(0);

        onBroken?.Invoke(true);
        (await sut.CurrentAsync).ShouldBe(1);

        onBroken?.Invoke(true);
        (await sut.CurrentAsync).ShouldBe(2);
    }

    [Test, Retry(3)]
    public async Task can_return_same_box_twice() {
        Action<bool>? onBroken = null;
        var           count    = 0;
        using var sut = new SharingProvider<bool, int>(
            async (_, f) => {
                onBroken = f;
                return count++;
            },
            TimeSpan.FromSeconds(0),
            true
        );

        (await sut.CurrentAsync).ShouldBe(0);

        var firstOnBroken = onBroken;
        firstOnBroken?.Invoke(true);
        firstOnBroken?.Invoke(true);
        firstOnBroken?.Invoke(true);

        // factory is only executed once
        (await sut.CurrentAsync).ShouldBe(1);
    }

    [Test, Retry(3)]
    public async Task can_return_pending_box() {
        var           trigger  = new SemaphoreSlim(0);
        Action<bool>? onBroken = null;
        var           count    = 0;
        using var sut = new SharingProvider<bool, int>(
            async (_, f) => {
                onBroken = f;
                count++;
                await trigger.WaitAsync();
                return count;
            },
            TimeSpan.FromSeconds(0),
            true
        );

        var currentTask = sut.CurrentAsync;

        currentTask.IsCompleted.ShouldBeFalse();

        // return it even though it is pending
        onBroken?.Invoke(true);

        // box wasn't replaced
        sut.CurrentAsync.ShouldBe(currentTask);

        // factory was not called again
        count.ShouldBe(1);

        // complete whatever factory calls
        trigger.Release(100);

        // can get the value now
        (await sut.CurrentAsync).ShouldBe(1);

        // factory still wasn't called again
        count.ShouldBe(1);
    }

    [Test, Retry(3)]
    public async Task factory_can_throw() {
        using var sut = new SharingProvider<int, int>(
            (x, _) => throw new($"input {x}"),
            TimeSpan.FromSeconds(0),
            0
        );

        // exception propagated to consumer
        var ex = await Should.ThrowAsync<Exception>(async () => { await sut.CurrentAsync; });

        ex.Message.ShouldBe("input 0");
    }

    // safe to call onBroken before the factory has returned, but it doesn't
    // do anything because the box is not populated yet.
    // the factory has to indicate failure by throwing.
    [Test, Retry(3)]
    public async Task factory_can_call_on_broken_synchronously() {
        using var sut = new SharingProvider<int, int>(
            async (x, onBroken) => {
                if (x == 0)
                    onBroken(5);

                return x;
            },
            TimeSpan.FromSeconds(0),
            0
        );

        // onBroken was called but it didn't do anything
        (await sut.CurrentAsync).ShouldBe(0);
    }

    [Test, Retry(3)]
    public async Task factory_can_call_on_broken_synchronously_and_throw() {
        using var sut = new SharingProvider<int, int>(
            async (x, onBroken) => {
                if (x == 0) {
                    onBroken(5);
                    throw new($"input {x}");
                }

                return x;
            },
            TimeSpan.FromSeconds(0),
            0
        );

        var ex = await Should.ThrowAsync<Exception>(async () => { await sut.CurrentAsync; });

        ex.Message.ShouldBe("input 0");
    }

    [Test, Retry(3)]
    public async Task stops_after_being_disposed() {
        Action<bool>? onBroken = null;

        var count = 0;

        using var sut = new SharingProvider<bool, int>(
            async (_, f) => {
                onBroken = f;
                return count++;
            },
            TimeSpan.FromSeconds(0),
            true
        );

        (await sut.CurrentAsync).ShouldBe(0);
        count.ShouldBe(1);

        sut.Dispose();

        // return the box
        onBroken?.Invoke(true);

        // the factory method isn't called any more
        await Should.ThrowAsync<ObjectDisposedException>(async () => await sut.CurrentAsync);
        count.ShouldBe(1);
    }

    [Test, Retry(3)]
    public async Task example_usage() {
        // factory waits to be signalled by completeConstruction being released
        // sometimes the factory succeeds, sometimes it throws.
        // failure of the produced item is trigged by
        var completeConstruction  = new SemaphoreSlim(0);
        var constructionCompleted = new SemaphoreSlim(0);

        var triggerFailure = new SemaphoreSlim(0);
        var failed         = new SemaphoreSlim(0);

        async Task<int> Factory(int input, Action<int> onBroken) {
            await completeConstruction.WaitAsync();
            try {
                if (input == 2) {
                    throw new($"fail to create {input} in factory");
                }
                else {
                    _ = triggerFailure.WaitAsync().ContinueWith(_ => {
                            onBroken(input + 1);
                            failed.Release();
                        }
                    );

                    return input;
                }
            }
            finally {
                constructionCompleted.Release();
            }
        }

        using var sut = new SharingProvider<int, int>(Factory, TimeSpan.FromSeconds(0), 0);

        // got an item (0)
        completeConstruction.Release();
        (await sut.CurrentAsync).ShouldBe(0);

        // when item 0 fails
        triggerFailure.Release();
        await failed.WaitAsync();

        // then a new item is produced (1)
        await constructionCompleted.WaitAsync();
        completeConstruction.Release();
        (await sut.CurrentAsync).ShouldBe(1);

        // when item 1 fails
        triggerFailure.Release();
        await failed.WaitAsync();

        // then item 2 is not created
        var t = sut.CurrentAsync;
        await constructionCompleted.WaitAsync();
        completeConstruction.Release();
        var ex = await Should.ThrowAsync<Exception>(async () => { await t; });
        ex.Message.ShouldBe("fail to create 2 in factory");

        // when the factory is allowed to produce another item (0), it does:
        await constructionCompleted.WaitAsync();
        completeConstruction.Release();
        // the previous box failed to be constructured, the factory will be called to produce another
        // one. but until this has happened the old box with the error is the current one.
        // therefore wait until the factory has had a chance to attempt another construction.
        // the previous awaiting this semaphor are only there so that we can tell when
        // this one is done.
        await constructionCompleted.WaitAsync();
        (await sut.CurrentAsync).ShouldBe(0);
    }
}
