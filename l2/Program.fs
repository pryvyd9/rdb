open l2
open Avalonia;

[<EntryPoint>]
let main argv =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug(Logging.LogEventLevel.Debug).StartWithClassicDesktopLifetime(argv) |> ignore

    0 // return an integer exit code