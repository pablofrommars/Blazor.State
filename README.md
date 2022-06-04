Blazor.State
===============

[![NuGet](https://img.shields.io/nuget/vpre/Blazor.State.svg)](https://www.nuget.org/packages/Blazor.State)
[![NuGet](https://img.shields.io/nuget/dt/Blazor.State.svg)](https://www.nuget.org/packages/Blazor.State) 

Source generator for Minimalistic State Management in Blazor.

# Installation

Install [Blazor.State with NuGet](https://www.nuget.org/packages/Blazor.State):

    Install-Package Blazor.State
    
Or via the .NET Core command line interface:

    dotnet add package Blazor.State

# Usage

## Define your schema including state, commands and events

```csharp
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

## Reducers instruct how commands modify states and publish events

```csharp
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

## Wire your components

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

## Register Dependency Injection

```csharp
builder.Services.AddBlazorState();
```