# zero-logging

Zero File logger provider for [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging), the logging subsystem used by ASP.NET Core.

### Install

**First**, install the _Zero.Logging.File_ [NuGet package](https://www.nuget.org/packages/Zero.Logging.File) into your app:

```powershell
dotnet add package Zero.Logging.File --version 2.0.0-preview1
```

### Configure

**Next**, add file section config in appsettings.json:

```json
{
  "Logging": {
    "IncludeScopes": false,
    "Debug": {
      "LogLevel": {
        "Default": "Warning"
      }
    },
    "Console": {
      "LogLevel": {
        "Default": "Warning"
      }
    },
    "File":{
      "LogLevel": {
        "Default": "Information"
      },
      "Path": "Logs\\my-{Date}.log"
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
            logging.AddFile(hostingContext.Configuration.GetSection("Logging:File"));
        })
        .UseStartup<Startup>()
        .Build();
```

### Demonstrate

That's it! With the level bumped up a little you will see log output like:

```
2017-09-05 16:30:11.244 +08:00 [info] Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[0]: User profile is available. Using 'C:\Users\xxx\AppData\Local\ASP.NET\DataProtection-Keys' as key repository and Windows DPAPI to encrypt keys at rest.
2017-09-05 16:30:11.955 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[1]: Request starting HTTP/1.1 GET http://localhost:5000/  
2017-09-05 16:30:12.045 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[2]: Request finished in 89.4786ms 404 
2017-09-05 16:30:12.119 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[1]: Request starting HTTP/1.1 GET http://localhost:5000/favicon.ico  
2017-09-05 16:30:12.119 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[2]: Request finished in 0.4669ms 404 
2017-09-05 16:30:17.055 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[1]: Request starting HTTP/1.1 GET http://localhost:5000/api/values  
2017-09-05 16:30:17.088 +08:00 [info] Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker[1]: Executing action method WebAPI.Controllers.ValuesController.Get (WebAPI) with arguments ((null)) - ModelState is Valid
2017-09-05 16:30:17.095 +08:00 [info] Microsoft.AspNetCore.Mvc.Internal.ObjectResultExecutor[1]: Executing ObjectResult, writing value Microsoft.AspNetCore.Mvc.ControllerContext.
2017-09-05 16:30:17.153 +08:00 [info] Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker[2]: Executed action WebAPI.Controllers.ValuesController.Get (WebAPI) in 69.5004ms
2017-09-05 16:30:17.154 +08:00 [info] Microsoft.AspNetCore.Hosting.Internal.WebHost[2]: Request finished in 98.9511ms 200 application/json; charset=utf-8
```

