Blazor.State
===============

Source generator for Minimalistic State Management in Blazor

# Installation

# Usage

```csharp
// ! Define your schema including state, commands and events

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
```

```csharp
// ! A Reducer instructs how commands modify states and publish events

[Reducer<DarkMode>(PublishStrategy.Async)]
public static class Reducer
{
	public static ReducerResult<DarkMode.State, DarkMode.Event> Handle(DarkMode.State current, DarkMode.Command.Toggle command)
		=> new()
		{
			State = current with { IsDark = !current.IsDark },
			Events = new[] { new DarkMode.Event.StateChanged() }
		};
}
```

```csharp
@implements IEventHandler<DarkMode.Event>
@implements IDisposable

@inject IState<DarkMode.State> State

<main class="@(State.Value.IsDark ? "dark" : "")">
    @Body
</main>

@code {
    protected override void OnInitialized()
    {
        Subscribe();
    }

    public async ValueTask HandleAsync(DarkMode.Event.StateChanged @event, CancellationToken token)
    {
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Unsubscribe();
    }
}
```

```csharp
// ! Last but not least Register Dependency Injection

builder.Services.AddBlazorState();
```
