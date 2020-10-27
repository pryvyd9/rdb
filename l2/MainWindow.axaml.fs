namespace l2

open System
open Avalonia.Markup.Xaml
open Avalonia.Controls

type MainWindow() as this =
    inherit Window()

    do 
        AvaloniaXamlLoader.Load(this)
        let dataSource = DataSource.warehouseTable
        let mutable filter:(Column*obj option) list = []

        let loadButton = this.FindControl<Button>("loadButton")
        loadButton.Click.AddHandler(fun _ _ -> 
            let items = DataSource.select dataSource filter
            this.FindControl<DynamicTable>("table").SetItems dataSource.columns items
        )

        let filterTable = this.FindControl<DynamicTable>("filter")
        filterTable.SetItems dataSource.columns [(dataSource.columns |> List.map (fun _ -> box ""))]
        filterTable.HorizontalScrollBarVisibility <- Primitives.ScrollBarVisibility.Auto
        filterTable.VerticalScrollBarVisibility <- Primitives.ScrollBarVisibility.Disabled
        filterTable.OnItemUpdated <-
            fun _ j v -> 
                let nf = List.zip dataSource.columns (filterTable.GetItemsString().Head |> List.ofArray)
                if nf |> List.forall (fun (c,v) -> String.IsNullOrWhiteSpace v || v |> c.toValue |> Option.isSome)
                then 
                    loadButton.IsEnabled <- true
                    filter <- List.zip dataSource.columns (filterTable.GetItems().Head |> List.ofArray)
                else
                    loadButton.IsEnabled <- false
