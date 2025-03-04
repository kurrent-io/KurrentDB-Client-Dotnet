using KurrentDb.Client;

namespace KurrentDb.Client.Tests;

[Trait("Category", "Target:Misc")]
public class KurrentDbClientOperationOptionsTests {
	[RetryFact]
	public void setting_options_on_clone_should_not_modify_original() {
		var options = KurrentDbClientOperationOptions.Default;

		var clonedOptions = options.Clone();
		clonedOptions.BatchAppendSize = int.MaxValue;

		Assert.Equal(options.BatchAppendSize, KurrentDbClientOperationOptions.Default.BatchAppendSize);
		Assert.Equal(int.MaxValue, clonedOptions.BatchAppendSize);
	}
}
