# Hangfire.Console

[![Build status](https://ci.appveyor.com/api/projects/status/b57hb7438d7dvxa2/branch/master?svg=true&passingText=master%20%u2714)](https://ci.appveyor.com/project/pieceofsummer/hangfire-console/branch/master)
[![NuGet](https://img.shields.io/nuget/v/Hangfire.Console.svg)](https://www.nuget.org/packages/Hangfire.Console/)
![MIT License](https://img.shields.io/badge/license-MIT-orange.svg)

Inspired by AppVeyor, Hangfire.Console provides a console-like logging experience for your jobs. 

![dashboard](dashboard.png)

## Features

 - **Provider-agnostic**: (allegedly) works with any job storage provider (currently tested with SqlServer and MongoDB). 
 - **100% Safe**: no Hangfire-managed data (e.g. jobs, states) is ever updated, hence there's no risk to corrupt it.
 - **With Live Updates**: new messages will appear as they're logged, as if you're looking at real console.
 - (blah-blah-blah)

## Setup

In .NET Core's Startup.cs:
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHangfire(config =>
    {
        config.UseSqlServerStorage("connectionSting");
        config.UseConsole();
    });
}
```

Otherwise,
```c#
GlobalConfiguration.Configuration
    .UseSqlServerStorage("connectionSting")
    .UseConsole();
```

**NOTE**: If you have Dashboard and Server running separately, 
you'll need to call `UseConsole()` on both.

### Additional options

As usual, you may provide additional options for `UseConsole()` method.

Here's what you can configure:
- **ExpireIn** – time to keep console sessions (default: 24 hours)
- **FollowJobRetentionPolicy** – expire all console sessions along with parent job (default: true)
- **PollInterval** – poll interval for live updates, ms (default: 1000)
- **BackgroundColor** – console background color (default: #0d3163)
- **TextColor** – console default text color (default: #ffffff)
- **TimestampColor** – timestamp text color (default: #00aad7)

**NOTE**: After you initially add Hangfire.Console (or change the options above) you may need to clear browser cache, as generated CSS/JS can be cached by browser.

## Log

Hangfire.Console provides extension methods on `PerformContext` object, 
hence you'll need to add it as a job argument. 

**NOTE**: Like `IJobCancellationToken`, `PerformContext` is a special argument type which Hangfire will substitute automatically. You should pass `null` when enqueuing a job.

Now you can write to console:

```c#
public void TaskMethod(PerformContext context)
{
    context.WriteLine("Hello, world!");
}
```

Like with `System.Console`, you can specify text color for your messages:

```c#
public void TaskMethod(PerformContext context)
{
    context.SetTextColor(ConsoleTextColor.Red);
    context.WriteLine("Error!");
    context.ResetTextColor();
}
```

## Progress bars

Version 1.1.0 adds support for inline progress bars:

![progress](progress.png)

```c#
public void TaskMethod(PerformContext context)
{
    // create progress bar
    var progress = context.WriteProgressBar();
    
    // update value for previously created progress bar
    progress.SetValue(100);
}
```

You can create multiple progress bars and update them separately.

By default, progress bar is initialized with value `0`. You can specify initial value and progress bar color as optional arguments for `WriteProgressBar()`.

### Enumeration progress

To easily track progress of enumeration over a collection in a for-each loop, library adds an extension method `WithProgress`:

```c#
public void TaskMethod(PerformContext context)
{
    var bar = context.WriteProgressBar();
    
    foreach (var item in collection.WithProgress(bar))
    {
        // do work
    }
}
```

It will automatically update progress bar during enumeration, and will set progress to 100% if for-each loop was interrupted with a `break` instruction.

**NOTE**: If the number of items in the collection cannot be determined automatically (e.g. collection doesn't implement `ICollection`/`ICollection<T>`/`IReadOnlyCollection<T>`, you'll need to pass additional argument `count` to the extension method).

## License

Copyright (c) 2016 Alexey Skalozub

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
