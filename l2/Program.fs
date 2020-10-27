open l2
open Avalonia;
//open Avalonia.Logging.Serilog;

[<EntryPoint>]
let main argv =
    AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug(Logging.LogEventLevel.Debug).StartWithClassicDesktopLifetime(argv) |> ignore
    //let columnDefinitions = 
    //    [
    //        {
    //            name = "1"
    //            validate = fun _ -> None
    //            toString = string
    //            toValue = fun a -> box a 
    //        }
    //        {
    //            name = "2"
    //            validate = fun _ -> None
    //            toString = string
    //            toValue = fun a -> box a 
    //        }
    //    ]

    //let items = [["0:0"; "0:1"];["1:0";"1:1"];["2:0";"2:1"]]
    //let items = items |> List.map (List.map box)

    0 // return an integer exit code