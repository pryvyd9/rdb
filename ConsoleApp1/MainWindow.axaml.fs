namespace ConsoleApp1

open System
open Avalonia
open Avalonia.Logging.Serilog
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Input

type MainWindow() as this =
    inherit Window()

  
    let stackPanel = StackPanel()
    let dynamicTable = DynamicTable()

    do 
        this.Content <- stackPanel

        let rb = Button()
        rb.Click.AddHandler(fun a b -> printfn "here")
        rb.Content <- "Refresh"
        stackPanel.Children.Add(rb)

        let lb = Button()
        lb.Click.AddHandler(fun a b -> 
            let columnDefinitions = 
                [
                    {
                        name = "1"
                        validate = fun _ -> None
                        toString = string
                        toValue = fun a -> box a 
                    }
                    {
                        name = "2"
                        validate = fun _ -> None
                        toString = string
                        toValue = fun a -> box a 
                    }
                ]

            let items = [["0:0"; "0:1"];["1:0";"1:1"];["2:0";"2:1"]]
            let items = items |> List.map (List.map box)


            dynamicTable.SetItems columnDefinitions items (fun a -> true)
        )

        //this.Initialized.AddHandler(fun a b -> 
        //    let columnDefinitions = 
        //        [
        //            {
        //                name = "1"
        //                validate = fun _ -> None
        //                toString = string
        //                toValue = fun a -> box a 
        //            }
        //            {
        //                name = "2"
        //                validate = fun _ -> None
        //                toString = string
        //                toValue = fun a -> box a 
        //            }
        //        ]

        //    let items = [["0:0"; "0:1"];["1:0";"1:1"];["2:0";"2:1"]]
        //    let items = items |> List.map (List.map box)


        //    dynamicTable.SetItems columnDefinitions items (fun a -> true)
        //)

        lb.Content <- "Load"
        stackPanel.Children.Add(lb)
        stackPanel.Children.Add(dynamicTable)

        //this.Content <- dynamicTable
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


        //dynamicTable.SetItems columnDefinitions items (fun a -> true)

    do AvaloniaXamlLoader.Load(this)
#if DEBUG
    do this.AttachDevTools()
#endif


    //member _.SetTable columnDefinitions items =
    //    dynamicTable.SetItems columnDefinitions items (fun a -> true)
