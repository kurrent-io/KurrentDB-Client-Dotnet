using EventStore.Client;
using EventStore.Client.Projections;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	public async ValueTask<Result<bool, Exception>> Update(
		string name, string query, bool? emitEnabled = null, CancellationToken cancellationToken = default
	) {
		var options = new UpdateReq.Types.Options {
			Name  = name,
			Query = query
		};

		if (emitEnabled.HasValue)
			options.EmitEnabled = emitEnabled.Value;
		else
			options.NoEmitOptions = new Empty();

		var request = new UpdateReq {
			Options = options
		};

		var resp = await ServiceClient.UpdateAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

		return resp is null
			? true
			: new Exception("Failed to update projection.");
	}
}
