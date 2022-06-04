namespace Blazor.State.Tests;

public class GeneratorTests
{
	[Fact]
	public void Generated_Source()
	{
		var source = @"
using Blazor.State;

namespace Blazor.State.Tests; 

public abstract record DarkMode
{
	public record State
	{
		public bool IsDark { get; init; } = true;
	}

	public abstract record Command
	{
		public record Toggle() : Command;
	}

	public abstract record Event
	{
		public record StateChanged() : Event;
	}
}

[Reducer(typeof(DarkMode), PublishStrategy.FireAndForget)]
public static class DarkModeReducer
{
	public static ReducerResult<DarkMode.State, DarkMode.Event> Handle(DarkMode.State current, DarkMode.Command.Toggle command)
		=> new()
		{
			State = current with { IsDark = !current.IsDark },
			Events = new[] { new DarkMode.Event.StateChanged() }
		};
}

public partial class Component : IEventHandler<DarkMode.Event>
{
	[Inject]
	public IState<DarkMode.State> State { get; init; } = default!;

	protected override void OnInitialized()
	{
		Subscribe();
	}

	public ValueTask HandleAsync(DarkMode.Event.StateChanged @event, CancellationToken token)
		=> ValueTask.CompletedTask;

	public void Dispose()
	{
		Unsubscribe();
	}
}
";

		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(o => !o.IsDynamic)
			.Select(o => MetadataReference.CreateFromFile(o.Location))
			.ToList();

		var compilation = CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: new[]
			{
				CSharpSyntaxTree.ParseText(source)
			},
			references: references
		);

		var result = CSharpGeneratorDriver
			.Create(new Generator())
			.RunGenerators(compilation)
			.GetRunResult();

		Assert.True(result.Diagnostics.IsEmpty);

		Assert.True(result.Results.Any());
		Assert.True(result.Results[0].GeneratedSources.Length >= 2);

		var generatedStore = result.Results[0].GeneratedSources[^2].SourceText.ToString();
		var generatedComponent = result.Results[0].GeneratedSources[^1].SourceText.ToString();
	}
}