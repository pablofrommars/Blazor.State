namespace Blazor.State;

public static class Templates
{
	public const string PublishStrategyText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

// https://github.com/jbogard/MediatR/tree/master/samples/MediatR.Examples.PublishStrategies

/// <summary>
/// Strategy to use when publishing notifications
/// </summary>
public enum PublishStrategy
{
	SyncContinueOnException
}";

	public const string ReducerAttributeText = @"// ! Auto Generated
#nullable enable

namespace Blazor.State;

[global::System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReducerAttribute : global::System.Attribute
{
	public ReducerAttribute(global::System.Type schema, PublishStrategy publishStrategy = PublishStrategy.SyncContinueOnException)
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

public interface IEventHandler<T> : global::System.IDisposable
{
	ValueTask HandleAsync(T @event, CancellationToken token = default);
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
 
internal interface IBroker
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

namespace Blazor.State;";

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
				.AddScoped(_ => new global::{@namespace}.{name}Store().Start())
				.AddScoped<IBroker<global::{@namespace}.{name}.Command, IEventHandler<global::{@namespace}.{name}.Event>>>(provider => provider.GetRequiredService<IStore<global::{@namespace}.{name}.State, global::{@namespace}.{name}.Command, global::{@namespace}.{name}.Event>>())
				.AddScoped<IState<global::{@namespace}.{name}.State>>(provider => provider.GetRequiredService<IStore<global::{@namespace}.{name}.State, global::{@namespace}.{name}.Command, global::{@namespace}.{name}.Event>>());");
		}

		builder.Append(@"		return services;
	}
}");
	}

	public static void Store(StringBuilder builder, (string @namespace, string name) schema, IEnumerable<string> commands, (string @namespace, string name) reducer)
	{
		builder.AppendLine(@$"// ! Auto Generated
#nullable enable

namespace Blazor.State;

public sealed class {schema.name}Store : StoreBase<global::{schema.@namespace}.{schema.name}.State, global::{schema.@namespace}.{schema.name}.Command, global::{schema.@namespace}.{schema.name}.Event>
{{
	protected override ReducerResult<global::{schema.@namespace}.{schema.name}.State, global::{schema.@namespace}.{schema.name}.Event> Reduce(global::{schema.@namespace}.{schema.name}.Command command)
		=> command switch
		{{");

		foreach (var command in commands)
		{
			var lower = char.ToLowerInvariant(command[0]) + command.Substring(1);

			builder.AppendLine($"			global::{schema.@namespace}.{schema.name}.Command {lower} => global::{reducer.@namespace}.{reducer.name}.Handle(State, {lower}),");
		}

		builder.Append(@"			_ => throw new global::System.NotSupportedException()
		};
}");
	}

	public static void HandlerEvents(StringBuilder builder, (string @namespace, string name) schema, List<string> events)
	{
		builder.AppendLine(@$"    [global::Microsoft.AspNetCore.Components.InjectAttribute]
    private IBroker<global::{schema.@namespace}.{schema.name}.Command, IEventHandler<global::{schema.@namespace}.{schema.name}.Event>>? {schema.name}Broker {{ get; init; }} = default!;

	public global::System.Threading.ValueTask HandleAsync(global::{schema.@namespace}.{schema.name}.Event @event, global::System.Threading.CancellationToken token)
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
			return global::System.Threading.ValueTask.FromException(new global::System.NotSupportedException());
		}
	}");
	}

	public static void Handler(StringBuilder builder, (string @namespace, string name) handler, IEnumerable<((string @namespace, string name) schema, List<string> events)> schemas)
	{
		builder.AppendLine(@$"// ! Auto Generated
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
			HandlerEvents(builder, schema.schema, schema.events);
		}

		builder.Append("}");
	}
}