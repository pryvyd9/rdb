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
        let dataSource = DataSource.warehouseTable
        let mutable filter:(Column*obj option) list = []

        let loadButton = this.FindControl<Button>("loadButton")
        loadButton.Click.AddHandler(fun _ _ -> 
            let items = DataSource.load dataSource filter
            this.FindControl<DynamicTable>("table").SetItems dataSource.columns items
        )

        let filterTable = this.FindControl<DynamicTable>("filter")
        filterTable.SetItems dataSource.columns.Tail [(dataSource.columns.Tail |> List.map (fun _ -> box ""))]
        filterTable.HorizontalScrollBarVisibility <- Primitives.ScrollBarVisibility.Auto
        filterTable.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Disabled
        filterTable.OnItemUpdated <-
            fun _ j v -> 
                let nf = List.zip dataSource.columns.Tail (filterTable.GetItemsString().Head |> List.ofArray)
                if nf |> List.forall (fun (c,v) -> String.IsNullOrWhiteSpace v || v |> c.toValue |> Option.isSome)
                then 
                    loadButton.IsEnabled <- true
                    filter <- List.zip dataSource.columns.Tail (filterTable.GetItems().Head |> List.ofArray)
                else
                    loadButton.IsEnabled <- false
