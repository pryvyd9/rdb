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

    do 
        AvaloniaXamlLoader.Load(this)
        let dataSource = DataSource.testTable

        this.FindControl<Button>("loadButton").Click.AddHandler(fun _ _ -> 
            let items = DataSource.load dataSource []
            this.FindControl<DynamicTable>("table").SetItems dataSource.columns items
        )
        this.FindControl<Button>("testButton").Click.AddHandler(fun _ _ -> 
            let table = this.FindControl<DynamicTable>("table")
            let items = table.GetItems()
            ()
        )
        let filter = this.FindControl<DynamicTable>("filter")
        filter.SetItems dataSource.columns [(dataSource.columns |> List.map (fun _ -> box ""))]
        filter.HorizontalScrollBarVisibility <- Primitives.ScrollBarVisibility.Auto
        filter.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Disabled


