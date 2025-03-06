using Kurrent.Client.Core.Serialization;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Streams.DecisionMaking;

public interface IAggregate<TEvent>: IState<TEvent>{
	Message[] DequeueUncommittedMessages();
}

public class Aggregate : Aggregate<object>;

public abstract class Aggregate<TEvent>: IAggregate<TEvent> where TEvent : notnull {
	readonly Queue<Message> _uncommittedEvents = new();

	public virtual void Apply(TEvent @event) { }

	Message[] IAggregate<TEvent>.DequeueUncommittedMessages()
	{
		var dequeuedEvents = _uncommittedEvents.ToArray();

		_uncommittedEvents.Clear();

		return dequeuedEvents;
	}

	protected void Enqueue(TEvent @event) {
		Apply(@event);
		_uncommittedEvents.Enqueue(Message.From(@event));
	}

	protected void Enqueue(Message message) {
		Apply((TEvent)message.Data);
		_uncommittedEvents.Enqueue(message);
	}
}
