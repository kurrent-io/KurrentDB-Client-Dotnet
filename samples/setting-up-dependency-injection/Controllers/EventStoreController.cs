using Microsoft.AspNetCore.Mvc;

namespace setting_up_dependency_injection.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class EventStoreController : ControllerBase {
		#region using-dependency
		private readonly KurrentDBClient _KurrentDBClient;

		public EventStoreController(KurrentDBClient KurrentDBClient) {
			_KurrentDBClient = KurrentDBClient;
		}

		[HttpGet]
		public IAsyncEnumerable<ResolvedEvent> Get() {
			return _KurrentDBClient.ReadAllAsync(Direction.Forwards, Position.Start);
		}
		#endregion using-dependency
	}
}
