using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public interface IAggregate<in TEvent> : IState<TEvent> {
	Message[] DequeueUncommittedMessages();
}

public interface IAggregate : IAggregate<object>;

public class Aggregate : Aggregate<object>, IAggregate;

public abstract class Aggregate<TEvent> : IAggregate<TEvent> where TEvent : notnull {
	readonly Queue<Message> _uncommittedEvents = new();

	public virtual void Apply(TEvent @event) { }

	Message[] IAggregate<TEvent>.DequeueUncommittedMessages() {
		var dequeuedEvents = _uncommittedEvents.ToArray();

		_uncommittedEvents.Clear();

		return dequeuedEvents;
	}

	protected void Enqueue(TEvent message) {
		Apply(message);
		_uncommittedEvents.Enqueue(Message.From(message));
	}

	protected void Enqueue(Message message) {
		if (message.Data is TEvent @event)
			Apply(@event);

		_uncommittedEvents.Enqueue(message);
	}
}
