using KurrentDB.Client;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class KurrentDBClientOperationOptionsTests {
	[RetryFact]
	public void setting_options_on_clone_should_not_modify_original() {
		var options = KurrentDBClientOperationOptions.Default;

		var clonedOptions = options.Clone();
		clonedOptions.BatchAppendSize = int.MaxValue;

		Assert.Equal(options.BatchAppendSize, KurrentDBClientOperationOptions.Default.BatchAppendSize);
		Assert.Equal(int.MaxValue, clonedOptions.BatchAppendSize);
	}
}
