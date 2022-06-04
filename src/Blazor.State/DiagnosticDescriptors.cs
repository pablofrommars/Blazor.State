namespace Blazor.State;

public static class DiagnosticDescriptors
{
	public static DiagnosticDescriptor InvalidReducer { get; } = new(
		id: "BSTATE1001",
		title: "Invalid Reducer",
		messageFormat: "{} {0}",
		category: "BlazorStateGenerator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static DiagnosticDescriptor InvalidSchema { get; } = new(
		id: "BSTATE1002",
		title: "Invalid Schema",
		messageFormat: "{0} {1}",
		category: "BlazorStateGenerator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static DiagnosticDescriptor InvalidIEventHandler { get; } = new(
		id: "BSTATE1003",
		title: "Invalid IEventHandler",
		messageFormat: "{0} {1}",
		category: "BlazorStateGenerator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static DiagnosticDescriptor InvalidIState { get; } = new(
		id: "BSTATE1004",
		title: "Invalid IState",
		messageFormat: "{0} {1}",
		category: "BlazorStateGenerator",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
}