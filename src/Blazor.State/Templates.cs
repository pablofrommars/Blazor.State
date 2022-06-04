namespace Blazor.State;

public static class Templates
{
	public const string PublishStrategyText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

// https://github.com/jbogard/MediatR/tree/master/samples/MediatR.Examples.PublishStrategies

public enum PublishStrategy
{
	Async = 0,
	FireAndForget = 1,
	SyncContinueOnException = 2,
	SyncStopOnException = 3
}";

	public const string ReducerAttributeText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

[global::System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReducerAttribute : global::System.Attribute
{
	public ReducerAttribute(global::System.Type schema, PublishStrategy publishStrategy = PublishStrategy.Async)
	{
		Schema = schema;
		PublishStrategy = publishStrategy;
	}

	public global::System.Type Schema { get; }
	public PublishStrategy PublishStrategy { get; }
}";

	public const string ReducerResultText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public record ReducerResult<TState, TEvent>
{
	public TState? State { get; init; }

	public TEvent[]? Events { get; init; }
}";

	public const string IEventHandlerText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public interface IEventHandler<T>
{
	global::System.Threading.Tasks.ValueTask HandleAsync(T @event, CancellationToken token = default);
}";

	public const string IStateText = @"namespace Blazor.State;
public interface IState<T>
{
	T Value { get; }
}";

	public const string ISubscriptionText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;
 
public interface ISubscription : global::System.IDisposable
{
}";

	public const string IBrokerText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;
 
public interface IBroker
{
	void Unsubscribe(global::System.Guid guid);
}

public interface IBroker<TCommand, TEventHandler> : IBroker
{
	global::System.Threading.Tasks.ValueTask SendAsync(TCommand command, global::System.Threading.CancellationToken token = default);

	ISubscription Subscribe(TEventHandler handler);
}";

	public const string SubscriptionText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;
 
internal sealed class Subscription : ISubscription
{
	private readonly global::System.Guid guid;
	private readonly IBroker broker;

	public Subscription(global::System.Guid guid, IBroker broker)
	{
		this.guid = guid;
		this.broker = broker;
	}

	public void Dispose()
	{
		broker.Unsubscribe(guid);
	}
}";

	public const string SubscriptionRegistryText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;
 
internal sealed class SubscriptionRegistry<T>
{
	private readonly global::System.Collections.Concurrent.ConcurrentDictionary<global::System.Guid, T> subscribers = new();

	private readonly IBroker broker;

	public SubscriptionRegistry(IBroker broker)
	{
		this.broker = broker;
	}

	public global::System.Collections.Generic.IEnumerable<T> Subscribers => subscribers.Values;

	public ISubscription Subscribe(T handler)
	{
		var guid = global::System.Guid.NewGuid();

		subscribers.AddOrUpdate(guid, handler, (_, o) => o);

		return new Subscription(guid, broker);
	}

	public void Unsubscribe(global::System.Guid id)
	{
		subscribers.TryRemove(id, out _);
	}
}";

	public const string IStoreText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public interface IStore<TState, TCommand, TEvent>
	: IBroker<TCommand, IEventHandler<TEvent>>, IState<TState>, IAsyncDisposable
	where TState : new()
{
}";

	public const string StoreBaseText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public abstract class StoreBase<TState, TCommand, TEvent> 
	: IStore<TState, TCommand, TEvent>
	where TState : new()
{
	private readonly global::System.Threading.CancellationTokenSource cancellation = new();

	private readonly PublishStrategy publishStrategy;
	private readonly SubscriptionRegistry<IEventHandler<TEvent>> registry;

	public StoreBase(PublishStrategy publishStrategy)
	{
		this.publishStrategy = publishStrategy;
		registry = new(this);
	}

	private readonly global::System.Threading.Channels.Channel<TCommand> channel = global::System.Threading.Channels.Channel.CreateUnbounded<TCommand>(new()
	{
		SingleReader = true,
		SingleWriter = false
	});

	public global::System.Threading.Tasks.ValueTask SendAsync(TCommand command, global::System.Threading.CancellationToken token = default)
		=> channel.Writer.WriteAsync(command, token);

	public ISubscription Subscribe(IEventHandler<TEvent> handler)
		=> registry.Subscribe(handler);

	public void Unsubscribe(global::System.Guid guid)
	{
		registry.Unsubscribe(guid);
	}

	public TState Value { get; private set; } = new();

	private async global::System.Threading.Tasks.Task PublishAsync(TEvent[] events, global::System.Threading.CancellationToken token)
	{
		var tasks = new global::System.Collections.Generic.List<global::System.Threading.Tasks.Task>();
		var exceptions = new global::System.Collections.Generic.List<global::System.Exception>();

		foreach (var handler in registry.Subscribers)
		{
			foreach (var @event in events)
			{
				try
				{
					tasks.Add(handler.HandleAsync(@event, token).AsTask());
				}
				catch (global::System.Exception ex) when (!(ex is global::System.OutOfMemoryException || ex is global::System.StackOverflowException))
				{
					exceptions.Add(ex);
				}
			}
		}

		try
		{
			await global::System.Threading.Tasks.Task.WhenAll(tasks);
		}
		catch (global::System.AggregateException ex)
		{
			exceptions.AddRange(ex.Flatten().InnerExceptions);
		}
		catch (global::System.Exception ex) when (!(ex is global::System.OutOfMemoryException || ex is global::System.StackOverflowException))
		{
			exceptions.Add(ex);
		}

		if (exceptions.Count > 0)
		{
			throw new global::System.AggregateException(exceptions);
		}
	}

	private global::System.Threading.Tasks.Task PublishFireAndForget(TEvent[] events, global::System.Threading.CancellationToken token)
	{
		foreach (var handler in registry.Subscribers)
		{
			foreach (var @event in events)
			{
#pragma warning disable CS4014
				global::System.Threading.Tasks.Task.Run(() => handler.HandleAsync(@event, token));
#pragma warning restore CS4014
			}
		}

		return global::System.Threading.Tasks.Task.CompletedTask;
	}

	private async global::System.Threading.Tasks.Task PublishSyncStopOnException(TEvent[] events, global::System.Threading.CancellationToken token)
	{
		foreach (var handler in registry.Subscribers)
		{
			foreach (var @event in events)
			{
				await handler.HandleAsync(@event, token);
			}
		}
	}

	private async global::System.Threading.Tasks.Task PublishSyncContinueOnException(TEvent[] events, global::System.Threading.CancellationToken token)
	{
		var exceptions = new global::System.Collections.Generic.List<global::System.Exception>();

		foreach (var handler in registry.Subscribers)
		{
			foreach (var @event in events)
			{
				try
				{
					await handler.HandleAsync(@event, token);
				}
				catch (global::System.AggregateException ex)
				{
					exceptions.AddRange(ex.Flatten().InnerExceptions);
				}
				catch (global::System.Exception ex) when (!(ex is global::System.OutOfMemoryException || ex is global::System.StackOverflowException))
				{
					exceptions.Add(ex);
				}
			}
		}

		if (exceptions.Count > 0)
		{
			throw new global::System.AggregateException(exceptions);
		}
	}

	protected abstract ReducerResult<TState, TEvent> Reduce(TState state, TCommand command);

	private async global::System.Threading.Tasks.ValueTask HandleAsync(TCommand command, global::System.Threading.CancellationToken token)
	{
		var result = Reduce(Value, command);

		if (result.State is not null)
		{
			Value = result.State;
		}

		if (result.Events is not null)
		{
			switch (publishStrategy)
			{
				case PublishStrategy.Async:
					await PublishAsync(result.Events, token);
					break;

				case PublishStrategy.FireAndForget:
					await PublishFireAndForget(result.Events, token);
					break;

				case PublishStrategy.SyncContinueOnException:
					await PublishSyncContinueOnException(result.Events, token);
					break;

				case PublishStrategy.SyncStopOnException:
					await PublishSyncStopOnException(result.Events, token);
					break;

				default:
					throw new global::System.NotImplementedException();
			}
		}
	}

	private global::System.Threading.Tasks.Task? background;

	private async global::System.Threading.Tasks.Task RunAsync()
	{
		try
		{
			while (!cancellation.IsCancellationRequested)
			{
				TCommand command;

				try
				{
					if (!await channel.Reader.WaitToReadAsync(cancellation.Token))
					{
						return;
					}

					command = await channel.Reader.ReadAsync(CancellationToken.None);

				}
				catch (global::System.Exception)
				{
					return;
				}

				await HandleAsync(command, cancellation.Token);
			}
		}
		catch (global::System.Exception)
		{
		}
	}

	public IStore<TState, TCommand, TEvent> Start()
	{
		background = RunAsync();

		return this;
	}

	private int disposing = 0;

	public async global::System.Threading.Tasks.ValueTask DisposeAsync()
	{
		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return;
		}

		channel.Writer.TryComplete();

		cancellation.Cancel();

		if (background is not null)
		{
			await background;
		}

		cancellation.Dispose();
	}
}";

	public static void IServiceCollectionExtensions(StringBuilder builder, IEnumerable<(string @namespace, string name)> schemas)
	{
		builder.AppendLine(@"// ! Auto Generated
#nullable enable

using Microsoft.Extensions.DependencyInjection;

namespace Blazor.State;

public static class IServiceCollectionExtensions
{
	public static IServiceCollection AddBlazorState(this IServiceCollection services)
	{");

		foreach (var (@namespace, name) in schemas)
		{
			builder.AppendLine(@$"		services
			.AddScoped(_ => new {name}Store().Start())
			.AddScoped<IBroker<global::{@namespace}.{name}.Command, IEventHandler<global::{@namespace}.{name}.Event>>>(provider => provider.GetRequiredService<IStore<global::{@namespace}.{name}.State, global::{@namespace}.{name}.Command, global::{@namespace}.{name}.Event>>())
			.AddScoped<IState<global::{@namespace}.{name}.State>>(provider => provider.GetRequiredService<IStore<global::{@namespace}.{name}.State, global::{@namespace}.{name}.Command, global::{@namespace}.{name}.Event>>());");
		}

		builder.Append(@"
		return services;
	}
}");
	}

	public static void Store(StringBuilder builder, (string @namespace, string name) schema, IEnumerable<string> commands, (string @namespace, string name, string publishStrategy) reducer)
	{
		builder.AppendLine(@$"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public sealed class {schema.name}Store : StoreBase<global::{schema.@namespace}.{schema.name}.State, global::{schema.@namespace}.{schema.name}.Command, global::{schema.@namespace}.{schema.name}.Event>
{{
	public {schema.name}Store()
		: base(PublishStrategy.{reducer.publishStrategy})
	{{
	}}

	protected override ReducerResult<global::{schema.@namespace}.{schema.name}.State, global::{schema.@namespace}.{schema.name}.Event> Reduce(global::{schema.@namespace}.{schema.name}.State state, global::{schema.@namespace}.{schema.name}.Command command)
		=> command switch
		{{");

		foreach (var command in commands)
		{
			var lower = char.ToLowerInvariant(command[0]) + command.Substring(1);

			builder.AppendLine($"			global::{schema.@namespace}.{schema.name}.Command.{command} {lower} => global::{reducer.@namespace}.{reducer.name}.Handle(state, {lower}),");
		}

		builder.Append(@"			_ => throw new global::System.NotSupportedException()
		};
}");
	}

	public static void HandlerEventBroker(StringBuilder builder, (string @namespace, string name) schema)
	{
		builder.AppendLine(@$"    [global::Microsoft.AspNetCore.Components.InjectAttribute]
    private IBroker<global::{schema.@namespace}.{schema.name}.Command, IEventHandler<global::{schema.@namespace}.{schema.name}.Event>>? {schema.name}Broker {{ get; init; }} = default!;
");
	}

	public static void HandlerEvents(StringBuilder builder, (string @namespace, string name) schema, List<string> events)
	{
		builder.AppendLine(@$"    public global::System.Threading.Tasks.ValueTask HandleAsync(global::{schema.@namespace}.{schema.name}.Event @event, global::System.Threading.CancellationToken token)
	{{");

		foreach (var @event in events)
		{
			var lower = char.ToLowerInvariant(@event[0]) + @event.Substring(1);

			builder.AppendLine(@$"		if (@event is global::{schema.@namespace}.{schema.name}.Event.{@event} {lower})
		{{
		    return HandleAsync({lower}, token);
		}}");
		}

		builder.AppendLine(@"		else
		{
			return global::System.Threading.Tasks.ValueTask.FromException(new global::System.NotSupportedException());
		}
	}
");
	}

	public static void HandlerSendCommand(StringBuilder builder, (string @namespace, string name) schema)
	{
		builder.AppendLine(@$"    protected global::System.Threading.Tasks.ValueTask SendAsync(global::{schema.@namespace}.{schema.name}.Command command, global::System.Threading.CancellationToken token = default)
		=> {schema.name}Broker!.SendAsync(command, token);
");
	}

	public static void HandlerSubscription(StringBuilder builder, IEnumerable<((string @namespace, string name) schema, List<string> events)> schemas)
	{
		foreach (var schema in schemas)
		{
			builder.AppendLine(@$"    private ISubscription? {schema.schema.name[0]}{schema.schema.name.Substring(1)}Subscription = null;");
		}

		builder.AppendLine(@"
	protected void Subscribe()
	{");
		foreach (var schema in schemas)
		{
			builder.AppendLine(@$"        {schema.schema.name[0]}{schema.schema.name.Substring(1)}Subscription = {schema.schema.name}Broker?.Subscribe(this);");
		}

		builder.AppendLine(@"    }
		
	protected void Unsubscribe()
	{");
		foreach (var schema in schemas)
		{
			builder.AppendLine(@$"        {schema.schema.name[0]}{schema.schema.name.Substring(1)}Subscription?.Dispose();");
		}

		builder.AppendLine(@"    }");
	}

	public static void Handler(StringBuilder builder, (string @namespace, string name) handler, IEnumerable<((string @namespace, string name) schema, List<string> events)> schemas)
	{
		builder.Append(@$"// ! Auto Generated
#nullable enable

namespace {handler.@namespace};

public partial class {handler.name} : ");

		var first = true;

		foreach (var schema in schemas)
		{
			foreach (var @event in schema.events)
			{
				if (!first)
				{
					builder.AppendLine(", ");
				}

				builder.AppendLine($"IEventHandler<global::{schema.schema.@namespace}.{schema.schema.name}.Event.{@event}>");

				first = false;
			}
		}

		builder.AppendLine("{");

		foreach (var schema in schemas)
		{
			HandlerEventBroker(builder, schema.schema);
			HandlerEvents(builder, schema.schema, schema.events);
			HandlerSendCommand(builder, schema.schema);
		}

		HandlerSubscription(builder, schemas);

		builder.Append("}");
	}
}