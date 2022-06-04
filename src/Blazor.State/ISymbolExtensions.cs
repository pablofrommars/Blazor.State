using Microsoft.CodeAnalysis;

namespace Blazor.State;

public static class ISymbolExtensions
{
	public static string? GetNamespace(this ISymbol symbol)
	{
		if (symbol.ContainingNamespace is null)
		{
			return null;
		}

		if (string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
		{
			return null;
		}

		var parent = GetNamespace(symbol.ContainingNamespace);
		if (parent is null)
		{
			return symbol.ContainingNamespace.Name;
		}
		else
		{
			return parent + "." + symbol.ContainingNamespace.Name;
		}
	}
}