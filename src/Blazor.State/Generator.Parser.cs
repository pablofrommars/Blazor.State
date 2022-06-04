namespace Blazor.State;

public partial class Generator
{
	internal sealed class Parser
	{
		public static bool ReducerPredicate(SyntaxNode node)
			=> node is ClassDeclarationSyntax
			{
				Modifiers.Count: > 0,
				AttributeLists.Count: > 0
			};

		public static ClassDeclarationSyntax? ReducerTransform(GeneratorSyntaxContext context)
		{
			var @class = (ClassDeclarationSyntax)context.Node;

			var isStatic = false;

			foreach (var modifier in @class.Modifiers)
			{
				if (modifier.IsKind(SyntaxKind.StaticKeyword))
				{
					isStatic = true;
					break;
				}
			}

			if (!isStatic)
			{
				return null;
			}

			foreach (var attributeListSyntax in @class.AttributeLists)
			{
				foreach (var attributeSyntax in attributeListSyntax.Attributes)
				{
					var name = attributeSyntax.Name.ToFullString();
					if (name == "Reducer")
					{
						return @class;
					}
				}
			}

			return null;
		}

		public static bool SchemaHandlerPredicate(SyntaxNode node)
			=> node is ClassDeclarationSyntax;

		public static ClassDeclarationSyntax? SchemaHandlerTransform(GeneratorSyntaxContext context)
		{
			var @class = (ClassDeclarationSyntax)context.Node;

			var isPartial = false;

			foreach (var modifier in @class.Modifiers)
			{
				if (modifier.IsKind(SyntaxKind.PartialKeyword))
				{
					isPartial = true;
					break;
				}
			}

			if (!isPartial)
			{
				return null;
			}

			if (@class.BaseList is null)
			{
				return null;
			}

			foreach (var type in @class.BaseList.Types)
			{
				if (type.Type is GenericNameSyntax { Arity: 1, Identifier.Text: "IEventHandler" })
				{
					return @class;
				}
			}

			return null;
		}

		public static bool InjectedStatePredicate(SyntaxNode node)
			=> node is PropertyDeclarationSyntax;

		public static PropertyDeclarationSyntax? InjectedStateTransform(GeneratorSyntaxContext context)
		{
			var property = (PropertyDeclarationSyntax)context.Node;

			var hasInjectAttribute = false;

			foreach (var attributeListSyntax in property.AttributeLists)
			{
				foreach (var attributeSyntax in attributeListSyntax.Attributes)
				{
					var name = attributeSyntax.Name.ToFullString();
					if (name == "Inject")
					{
						hasInjectAttribute = true;
					}
				}
			}

			if (!hasInjectAttribute)
			{
				return null;
			}

			if (property.Type is not GenericNameSyntax { Arity: 1, Identifier.Text: "IState" })
			{
				return null;
			}

			return property;
		}
	}
}