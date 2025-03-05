using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public interface IAggregate<TEvent>: IState<TEvent>{
	TEvent[] DequeueUncommittedEvents();
}

public class Aggregate : Aggregate<object>;

public abstract class Aggregate<TEvent>: IAggregate<TEvent>
{
	readonly Queue<TEvent> _uncommittedEvents = new();

	public virtual void Apply(TEvent @event) { }

	TEvent[] IAggregate<TEvent>.DequeueUncommittedEvents()
	{
		var dequeuedEvents = _uncommittedEvents.ToArray();

		_uncommittedEvents.Clear();

		return dequeuedEvents;
	}

	protected void Enqueue(TEvent @event) {
		Apply(@event);
		_uncommittedEvents.Enqueue(@event);
	}
}
