namespace l2

open System
open Avalonia
open Avalonia.Logging
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Input

type MainWindow() as this =
    inherit Window()

    let getData() =
        let columnDefinitions = 
            [
                {
                    name = "col1"
                    validate = fun _ -> None
                    toString = string
                    toValue = fun a -> box a 
                }
                {
                    name = "col2"
                    validate = fun _ -> None
                    toString = string
                    toValue = fun a -> box a 
                }
            ]

        let items = [["0:0"; "0:1"];["1:0";"1:1"];["2:0";"2:1"]]
        let items = items |> List.map (List.map box)
        columnDefinitions, items

    do 
        AvaloniaXamlLoader.Load(this)
        let columnDefinitions, items = getData()

        this.FindControl<Button>("loadButton").Click.AddHandler(fun _ _ -> 
            this.FindControl<DynamicTable>("table").SetItems columnDefinitions items
        )
        let filter = this.FindControl<DynamicTable>("filter")
        filter.SetItems columnDefinitions [(columnDefinitions |> List.map (fun _ -> box ""))]
        filter.HorizontalScrollBarVisibility <- Primitives.ScrollBarVisibility.Auto
        filter.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Disabled


