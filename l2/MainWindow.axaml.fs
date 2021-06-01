namespace l2

open System
open System.Linq
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia
open Infrastracture



type MainWindow() as this =
    inherit Window()
  
    let insertListItem a =
        //DataView.generateData()
        let viewList = this.FindControl<ListBox>("viewList")
        let item = ListBoxItem()

        let label = Label()
        label.Content <- a
        label.Name <- "viewName"
        label.SetValue(Grid.ColumnProperty, 0) |> ignore

        let button = Button()
        button.Content <- "X"
        button.Click.Add (fun _ -> 
            DataView.deleteCustomView a |> ignore

            // Due to some bug in Avalonia removing a single item removes all following
            // items from rendering.
            // So remove all the items, remove the required item from the list and assign
            // the whole list to the ListBox again.
            let items = (viewList.Items :?> Avalonia.Collections.AvaloniaList<Object>).ToList()
            items.Remove item |> ignore

            (viewList.Items :?> Avalonia.Collections.AvaloniaList<Object>).Clear()
            viewList.Items <- new Avalonia.Collections.AvaloniaList<Object>(items)
        )
        button.SetValue(Grid.ColumnProperty, 1) |> ignore

        let grid = Grid()
        grid.ColumnDefinitions.Add (ColumnDefinition())
        grid.ColumnDefinitions.Add (ColumnDefinition(24., GridUnitType.Pixel))
        grid.Children.Add label
        grid.Children.Add button

        item.Content <- grid
        (viewList.Items :?> Avalonia.Collections.AvaloniaList<Object>).Add item
        item

    let getViewName a = 
        let h = ((unbox a) :> ListBoxItem)
        let t = (h.Content :?> Grid)
        let j = t.Children.[0] :?> Label
        j.Content :?> string

    let mutable views:ListBoxItem list = []



    do 
        AvaloniaXamlLoader.Load(this)
        let warehouseTable = {
               columns = [
                   { Column.string with name = "dbsource"; displayName = "Source" }
                   { Column.int with name = "id" }
                   { Column.int with name = "item_id" }
                   { Column.float32 with name = "quantity" }
               ]
               name = "warehouse"
           }

        let viewList = this.FindControl<ListBox>("viewList")
        viewList.SelectionMode <- SelectionMode.AlwaysSelected ||| SelectionMode.Single
        viewList.SelectedIndex <- 0

        views <- DataView.getCustomViews () |> List.map insertListItem

        let viewEditor = this.FindControl<ViewEditor>("viewEditor")
        viewEditor.SetColumns warehouseTable.columns
        viewEditor.OnViewCreated <- (fun name columns -> 
            try 
                DataView.createCustomView name {warehouseTable with columns = columns} |> ignore
                views <- (insertListItem name)::views
            with _ -> ()
        )

        let loadButton = this.FindControl<Button>("loadButton")
        loadButton.Click.AddHandler(fun _ _ -> 
            try
                let items, columnsToDisplay = DataView.selectView (getViewName viewList.SelectedItem) warehouseTable.columns
                this.FindControl<DynamicTable>("table").SetItems columnsToDisplay items
            with _ -> 
                this.FindControl<DynamicTable>("table").SetItems warehouseTable.columns []
        )