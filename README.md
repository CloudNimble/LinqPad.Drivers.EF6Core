# CloudNimble's LINQPad Drivers for EF6 on .NET Core
[![Build status](https://dev.azure.com/cloudnimble/LinqPad.Drivers.EF6Core/_apis/build/status/LinqPad.Drivers.EF6Core-CI)](https://dev.azure.com/cloudnimble/LinqPad.Drivers.EF6Core/_build/latest?definitionId=-1)![NuGet](https://img.shields.io/nuget/v/CloudNimble.LinqPad.Drivers.EF6Core)

## What is this?
The team at [BurnRate.io](https://burnrate.io) leverages EF6 and Microsoft.Data.SqlClient to power our game-changing tools for scaling companies.
We use LINQPad for testing out our services and Blazor apps, as well as prototyping new features.

But the existing EF6 drivers don't support Microsoft.Data.SqlClient.

So we built our own.

This version takes a simpler approach to EF6 support that takes advantage of our extensive experience with EF metadata to provide a more robust experience.

## Features
  - Uses Microsoft.Data.SqlClient without metadata changes
  - Uses the very latest dependencies that avoid Azure vulnerabilities
  - Faster schema loading
  - Multiple EntityContainer support
  - Correctly labels primary and foreign key relationships
  - Improved connection experience with built-in connection testing
  - Updated connection icons
  - Demonstrates advanced XAML techniques for LINQPad drivers
  - Supports LINQPad 6 and later

## Dependencies
  - ErikEJ.EntityFramework.SqlServer
  - Microsoft.Data.SqlClient
  - ModernWpfUI
  - SharpVectors.WPF
  - Z.EntityFramework.Plus.EF6

## Installation
1. Open LINQPad 6 or later and click "Add connection" in the connection tree.
2. In the "Choose Data Context" dialog, select "View more drivers" in the bottom left corner.
3. In the "LINQPad NuGet Manager" dialog, select the "Show all drivers" option at the top of the center column.
4. Search for [CloudNimble.LinqPad.Drivers.EF6Core](https://nuget.org/packages/CloudNimble.LinqPad.Drivers.EF6Core) and install it.
5. Close the dialog. 

## Supported DbContexts
Your DbContext must have a public constructor accepting a `nameOrConnectionString` string as a parameter:

```cs
public class MyDbContext : DbContext
{

    public MyDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
    {
    }

}
```

## Configuration

1. You should now see "EF6 + Microsoft.Data.SqlClient on .NET 6 and later" in the "Choose Data Context" dialog. Select it and click "Next".
![Selection](https://github.com/CloudNimble/LinqPad.Drivers.EF6Core/assets/1657085/f754a1d3-e994-4152-a818-49c56c2058cb)

2. You will be presented with the dialog below.
![Configuration](https://github.com/CloudNimble/LinqPad.Drivers.EF6Core/assets/1657085/c61a67cc-cebd-483f-8620-994895d5b214)

3. Start by entering a name for the connection.
4. Click the first orange "Browse" link and select the assembly containing your DbContext.
5. Click the second orange "Choose" link and select your DbContext type.
6. Either select your appSettings.json file or an app.config file in the third orange "Browse" link, or enter a connection string directly in the fourth textbox.
7. Click "Test Connection" and observe the results, making changes as necessary.
8. Click "Create Connection" to complete the process.

## Known Issues

1. The `EF6MemberProvider` needs further updates to reduce its reliance on Reflection.

## Troubleshooting

1. If you're using a configuration file and you get an error that says "The connection string '[SomeName]' cannot be found", try switching to using a raw connection string instead.
