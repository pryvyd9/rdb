namespace l2

open System
open Avalonia.Markup.Xaml
open Avalonia.Controls

type MainWindow() as this =
    inherit Window()

    do 
        AvaloniaXamlLoader.Load(this)
        let warehouseTable = {
               columns = [
                   { Column.string with name = "dbSource"; displayName = "Source" }
                   { Column.int with name = "id" }
                   { Column.int with name = "item_id" }
                   { Column.float32 with name = "quantity" }
               ]
               name = "warehouse"
           }

        let mutable dataView:DataView = warehouseTable

        let viewEditor = this.FindControl<ViewEditor>("viewEditor")
        viewEditor.SetColumns dataView.columns


        let loadButton = this.FindControl<Button>("loadButton")
        loadButton.Click.AddHandler(fun _ _ -> 
            let items = DataView.select {dataView with columns = viewEditor.Columns}
            let columnsToDisplay = viewEditor.Columns |> List.filter (fun a -> a.shouldSelect)
            this.FindControl<DynamicTable>("table").SetItems columnsToDisplay items
        )