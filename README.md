# zero-logging

Zero logger provider for [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging), the logging subsystem used by ASP.NET Core.

## Logging in Elasticsearch

PLEASE Read [Zero.Logging.Elasticsearch](https://github.com/RainingNight/zero-logging/blob/dev/src/Zero.Logging.Elasticsearch/readme.md).

## Logging in File

### Install

**First**, install the _Zero.Logging.File_ [NuGet package](https://www.nuget.org/packages/Zero.Logging.File) into your app:

```powershell
dotnet add package Zero.Logging.File --version 1.0.0-alpha3-20180228
```

### Configure

**Next**, add file section config in appsettings.json:

```json
{
  "Logging": {
    "IncludeScopes": false,
    "Console": {
      "LogLevel": {
        "Default": "Warning"
      }
    },
    "File": {
      "LogLevel": {
        "Default": "Error"
      },
      "RollingInterval": "Minute"
    }
  }
}
```

**Finally**, in your application's _Program.cs_ file, configure _Zeor.Logging.File_ first:

```csharp
public static IWebHost BuildWebHost(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .ConfigureLogging((hostingContext, logging) =>
        {
            logging.AddFile();
        })
        .UseStartup<Startup>()
        .Build();
```

### Demonstrate

Call logging methods on that logger object:

```csharp
public class ValuesController : Controller
{
    private readonly ILogger _logger;

    public ValuesController(ILogger<ValuesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public void Get()
    {
        _logger.LogTrace("Log Trace.");
        _logger.LogInformation("Log Information.");
        _logger.LogDebug("Log Debug.");
        try
        {
            throw new Exception("Boom");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(1, ex, "Unexpected critical error starting application");
            _logger.LogError(1, ex, "Unexpected error");
            _logger.LogWarning(1, ex, "Unexpected warning");
        }
    }
}
```

That's it! With the level bumped up a little you will see log output like:

```text
# logs/log-201802271502.txt

2018-02-27 15:02:40.608 +08:00 [Critical] WebApplication1.Controllers.ValuesController: Unexpected critical error starting application
System.Exception: Boom
   at WebApplication1.Controllers.ValuesController.Get() in C:\Users\rainging\source\repos\WebApplication1\WebApplication1\Controllers\ValuesController.cs:line 28
2018-02-27 15:02:40.631 +08:00 [Error] WebApplication1.Controllers.ValuesController: Unexpected error
System.Exception: Boom
   at WebApplication1.Controllers.ValuesController.Get() in C:\Users\rainging\source\repos\WebApplication1\WebApplication1\Controllers\ValuesController.cs:line 28
```

