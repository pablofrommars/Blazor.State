namespace Blazor.State;

[Generator]
public partial class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(context =>
		{
			context.AddSource("Blazor.State.PublishStrategy.g.cs", Templates.PublishStrategyText);
			context.AddSource("Blazor.State.ReducerAttribute.g.cs", Templates.ReducerAttributeText);
			context.AddSource("Blazor.State.ReducerResult.g.cs", Templates.ReducerResultText);
			context.AddSource("Blazor.State.IEventHandler.g.cs", Templates.IEventHandlerText);
			context.AddSource("Blazor.State.IState.g.cs", Templates.IStateText);
			context.AddSource("Blazor.State.ISubscription.g.cs", Templates.ISubscriptionText);
			context.AddSource("Blazor.State.IBroker.g.cs", Templates.IBrokerText);
			context.AddSource("Blazor.State.Subscription.g.cs", Templates.SubscriptionText);
			context.AddSource("Blazor.State.SubscriptionRegistry.g.cs", Templates.SubscriptionRegistryText);
			context.AddSource("Blazor.State.IStore.g.cs", Templates.IStoreText);
			context.AddSource("Blazor.State.StoreBase.g.cs", Templates.StoreBaseText);
		});

		var reducers = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => Parser.ReducerPredicate(node),
				transform: static (context, _) => Parser.ReducerTransform(context))
			.Where(static o => o is not null)
			.Select(static (o, _) => o!);

		var handlers = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => Parser.SchemaHandlerPredicate(node),
				transform: static (context, _) => Parser.SchemaHandlerTransform(context))
			.Where(static o => o is not null)
			.Select(static (o, _) => o!);

		var injectedStates = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => Parser.InjectedStatePredicate(node),
				transform: static (context, _) => Parser.InjectedStateTransform(context))
			.Where(static o => o is not null)
			.Select(static (o, _) => o!);

		var handlersAndInjectedStates = handlers.Collect().Combine(injectedStates.Collect());
		var reducersAndHandlersAndInjectedStates = reducers.Collect().Combine(handlersAndInjectedStates);
		var compilationAndSyntax = context.CompilationProvider.Combine(reducersAndHandlersAndInjectedStates);

		context.RegisterSourceOutput(compilationAndSyntax, static (context, o) => Emiter.Execute(context, o.Left, o.Right.Left, o.Right.Right.Left, o.Right.Right.Right));
	}
}