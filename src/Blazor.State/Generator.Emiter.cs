namespace Blazor.State;

public partial class Generator
{
	internal sealed class Emiter
	{
		public static void Execute(
	SourceProductionContext context,
	Compilation compilation,
	ImmutableArray<ClassDeclarationSyntax> reducerCandidates,
	ImmutableArray<ClassDeclarationSyntax> handlerCandidates,
	ImmutableArray<PropertyDeclarationSyntax> injectedStateCandidates)
		{
			if (reducerCandidates.IsDefaultOrEmpty)
			{
				return;
			}

			var reducerAttribute = compilation.GetTypeByMetadataName("Blazor.State.ReducerAttribute");
			if (reducerAttribute == null)
			{
				return;
			}

			var schemas = new Dictionary<(string @namespace, string name), (INamedTypeSymbol state, INamedTypeSymbol command, List<string> commands, INamedTypeSymbol @event, List<string> events, (string @namespace, string name, string publishStrategy) reducer)>();

			// * https://github.com/dotnet/runtime/blob/6b11d64e7ef8f1685570f87859a3a3fe1c177d0f/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Parser.cs#L102
			foreach (var syntaxTreeGroup in reducerCandidates.GroupBy(x => x.SyntaxTree))
			{
				SemanticModel? semanticModel = null;
				foreach (var @class in syntaxTreeGroup)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					semanticModel ??= compilation.GetSemanticModel(@class.SyntaxTree);

					var symbol = semanticModel.GetDeclaredSymbol(@class, context.CancellationToken);
					if (symbol is null)
					{
						break;
					}

					bool isValidReducer = false;
					INamedTypeSymbol? schemaType = default;
					string? publishStrategy = null;

					foreach (var attributeListSyntax in @class.AttributeLists)
					{
						if (isValidReducer)
						{
							break;
						}

						foreach (var attributeSyntax in attributeListSyntax.Attributes)
						{
							if (semanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol is not IMethodSymbol ctor
								|| !reducerAttribute.Equals(ctor.ContainingType, SymbolEqualityComparer.Default))
							{
								continue;
							}

							var attributes = symbol?.GetAttributes();
							if (attributes == null)
							{
								continue;
							}

							foreach (var attributeData in attributes)
							{
								if (!attributeData.ConstructorArguments.Any())
								{
									continue;
								}

								var arguments = attributeData.ConstructorArguments;
								if (arguments.Length != 2)
								{
									break;
								}

								if (arguments[0] is not { Kind: TypedConstantKind.Type, IsNull: false })
								{
									break;
								}

								if (arguments[1] is not { Kind: TypedConstantKind.Enum, IsNull: false })
								{
									break;
								}

								schemaType = arguments[0].Value as INamedTypeSymbol;

								var values = (arguments[1].Type as INamedTypeSymbol)!.MemberNames!;

								var index = (int)arguments[1].Value!;
								if (index >= values.Count() || index < 0)
								{
									index = 0;
								}

								publishStrategy = values.ElementAt(index);

								isValidReducer = true;
							}
						}
					}

					if (!isValidReducer)
					{
						context.ReportDiagnostic(Diagnostic.Create(
							DiagnosticDescriptors.InvalidReducer,
							symbol.Locations[0],
							symbol.Name,
							"Invalid Reducer Attribute"));

						continue;
					}

					if (schemaType is not { IsAbstract: true, IsRecord: true })
					{
						context.ReportDiagnostic(Diagnostic.Create(
							DiagnosticDescriptors.InvalidReducer,
							schemaType?.Locations[0],
							schemaType?.Name,
							"Must be abstract record"));

						continue;
					}

					INamedTypeSymbol? state = null;
					INamedTypeSymbol? commandBase = null;
					INamedTypeSymbol? eventBase = null;
					List<string>? commands = default;
					List<string>? events = default;

					foreach (var member in schemaType.GetTypeMembers())
					{
						if (member.Name == "State")
						{
							if (member is not { IsAbstract: false, IsRecord: true })
							{
								context.ReportDiagnostic(Diagnostic.Create(
									DiagnosticDescriptors.InvalidSchema,
									member.Locations[0],
									schemaType?.Name,
									"Invalid State"));

								break;
							}

							state = member;
						}
						else if (member.Name == "Command")
						{
							if (member is not { IsAbstract: true, IsRecord: true })
							{
								context.ReportDiagnostic(Diagnostic.Create(
									DiagnosticDescriptors.InvalidSchema,
									member.Locations[0],
									schemaType?.Name,
									"Invalid Command Schema"));

								break;
							}

							commandBase = member;

							foreach (var command in member.GetTypeMembers())
							{
								if (!member.Equals(command.BaseType, SymbolEqualityComparer.Default))
								{
									context.ReportDiagnostic(Diagnostic.Create(
										DiagnosticDescriptors.InvalidSchema,
										@command.Locations[0],
										schemaType?.Name,
										"Invalid command base type"));

									continue;
								}

								commands ??= new();

								commands.Add(command.Name);
							}
						}
						else if (member is { Name: "Event", IsAbstract: true, IsRecord: true })
						{
							if (member is not { IsAbstract: true, IsRecord: true })
							{
								context.ReportDiagnostic(Diagnostic.Create(
									DiagnosticDescriptors.InvalidSchema,
									member.Locations[0],
									schemaType?.Name,
									"Invalid Event Schema"));

								break;
							}

							eventBase = member;

							foreach (var @event in member.GetTypeMembers())
							{
								if (!member.Equals(@event.BaseType, SymbolEqualityComparer.Default))
								{
									context.ReportDiagnostic(Diagnostic.Create(
										DiagnosticDescriptors.InvalidSchema,
										@event.Locations[0],
										schemaType?.Name,
										"Invalid event base type"));

									continue;
								}

								events ??= new();

								events.Add(@event.Name);
							}
						}
					}

					if (state is null || commandBase is null || eventBase is null)
					{
						continue;
					}

					var commandHandlerCount = 0;

					foreach (var member in symbol!.GetMembers())
					{
						if (member is not IMethodSymbol { Name: "Handle", Parameters.Length: 2 } method
							|| method.ReturnType is not INamedTypeSymbol { Name: "ReducerResult", TypeArguments.Length: 2 } returnType
							|| !method.Parameters[0].Type.Equals(state, SymbolEqualityComparer.Default)
							|| method.Parameters[1].Type is not INamedTypeSymbol parameter1type
							|| parameter1type.BaseType is not INamedTypeSymbol parameter1typeBaseType
							|| !parameter1typeBaseType.Equals(commandBase, SymbolEqualityComparer.Default)
							|| !returnType.TypeArguments[0].Equals(state, SymbolEqualityComparer.Default)
							|| !returnType.TypeArguments[1].Equals(eventBase, SymbolEqualityComparer.Default))
						{
							context.ReportDiagnostic(Diagnostic.Create(
								DiagnosticDescriptors.InvalidReducer,
								member.Locations[0],
								symbol.Name,
								"Invalid command handler"));

							continue;
						}

						commandHandlerCount++;
					}

					if (commandHandlerCount != commands?.Count)
					{
						context.ReportDiagnostic(Diagnostic.Create(
							DiagnosticDescriptors.InvalidReducer,
							symbol.Locations[0],
							symbol.Name,
							"Invalid command handler count"));

						continue;
					}

					schemas[(schemaType!.GetNamespace()!, schemaType!.Name)] = (state!, commandBase, commands!, eventBase, events!, (symbol.GetNamespace()!, symbol.Name, publishStrategy!));
				}
			}

			var handlers = new Dictionary<(string @namespace, string name), List<((string @namespace, string name) schema, List<string> events)>>();

			// * https://github.com/dotnet/runtime/blob/6b11d64e7ef8f1685570f87859a3a3fe1c177d0f/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Parser.cs#L102
			foreach (var syntaxTreeGroup in handlerCandidates.GroupBy(x => x.SyntaxTree))
			{
				SemanticModel? semanticModel = null;
				foreach (var @class in syntaxTreeGroup)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					semanticModel ??= compilation.GetSemanticModel(@class.SyntaxTree);

					var symbol = semanticModel.GetDeclaredSymbol(@class);
					if (symbol is null)
					{
						break;
					}

					var handles = new List<((string @namespace, string name) schema, List<string> events)>();

					foreach (var @interface in symbol.AllInterfaces)
					{
						if (@interface is INamedTypeSymbol { Name: "IEventHandler", TypeArguments.Length: 1 } iSchemaHandler)
						{
							var schemaType = iSchemaHandler.TypeArguments[0];

							var schemaFound = false;

							foreach (var schema in schemas)
							{
								if (schemaType.Equals(schema.Value.@event, SymbolEqualityComparer.Default))
								{
									handles.Add((schema.Key, schema.Value.events));
									schemaFound = true;
									break;
								}
							}

							if (!schemaFound)
							{
								context.ReportDiagnostic(Diagnostic.Create(
									DiagnosticDescriptors.InvalidIEventHandler,
									symbol.Locations[0],
									symbol.Name,
									"Schema not found"));
							}
						}
					}

					handlers[(symbol.GetNamespace()!, symbol.Name)] = handles;
				}
			}

			var injectedStates = new Dictionary<(string @namespace, string name), (string @namespace, string name)>();

			// * https://github.com/dotnet/runtime/blob/6b11d64e7ef8f1685570f87859a3a3fe1c177d0f/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Parser.cs#L102
			foreach (var syntaxTreeGroup in injectedStateCandidates.GroupBy(x => x.SyntaxTree))
			{
				SemanticModel? semanticModel = null;
				foreach (var property in syntaxTreeGroup)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					semanticModel ??= compilation.GetSemanticModel(property.SyntaxTree);

					if (semanticModel.GetSymbolInfo(property.Type).Symbol is not INamedTypeSymbol { TypeArguments.Length: 1 } iState)
					{
						break;
					}

					if (property.Parent is null || semanticModel.GetDeclaredSymbol(property.Parent) is not INamedTypeSymbol parent)
					{
						break;
					}

					var stateType = iState.TypeArguments[0];

					var schemaFound = false;

					foreach (var schema in schemas)
					{
						if (stateType.Equals(schema.Value.state, SymbolEqualityComparer.Default))
						{
							injectedStates[(parent.GetNamespace()!, parent.Name)] = schema.Key;
							schemaFound = true;
							break;
						}
					}

					if (!schemaFound)
					{
						context.ReportDiagnostic(Diagnostic.Create(
							DiagnosticDescriptors.InvalidIState,
							iState.Locations[0],
							property.Identifier.Text,
							"Schema not found"));
					}
				}
			}

			var builder = new StringBuilder();

			{
				builder.Clear();

				Templates.IServiceCollectionExtensions(builder, schemas.Keys);

				context.AddSource("Blazor.State.IServiceCollectionExtensions.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
			}

			foreach (var schema in schemas)
			{
				builder.Clear();

				Templates.Store(builder, schema.Key, schema.Value.commands, schema.Value.reducer);

				context.AddSource($"Blazor.State.{schema.Key.name}Store.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
			}

			foreach (var handler in handlers)
			{
				builder.Clear();

				Templates.Handler(builder, handler.Key, handler.Value);

				context.AddSource($"{handler.Key.@namespace}.{handler.Key.name}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
			}
		}
	}
}