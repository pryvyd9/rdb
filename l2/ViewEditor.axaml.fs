namespace l2

open System
open Avalonia
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Media



module List =
    let inline replace index newItem = List.mapi (fun i a -> if i = index then newItem else a)
    let inline update index updateFunc = List.mapi (fun i a -> if i = index then updateFunc a else a)

type ViewEditor() as this =
    inherit UserControl()
    do AvaloniaXamlLoader.Load(this)

    let grid = this.FindControl<Grid>("itemGrid")

    let mutable state:Column list = []
    let mutable onViewUpdated:(Column list->unit) = fun _ -> ()

    let clear() =
        state <- []
        grid.Children.Clear()
        grid.RowDefinitions.Clear()

    //let update func = state <- func state
    let update index func = state <- List.update index func state

    let addFilter (column: Column) =
        let rowId = grid.RowDefinitions.Count
        grid.RowDefinitions.Add(RowDefinition(26., GridUnitType.Pixel))

        let columnName = TextBox()
        columnName.SetValue(Grid.ColumnProperty, 0) |> ignore
        columnName.SetValue(Grid.RowProperty, rowId) |> ignore
        columnName.Text <- column.name
        columnName.IsReadOnly <- true
        columnName.CaretBrush <- Brushes.Transparent
        grid.Children.Add(columnName)
        
        let shouldSelect = CheckBox()
        shouldSelect.SetValue(Grid.ColumnProperty, 1) |> ignore
        shouldSelect.SetValue(Grid.RowProperty, rowId) |> ignore
        shouldSelect.IsChecked <- true

        shouldSelect.Checked.Add (fun _ -> update rowId (fun a -> {a with shouldSelect = true}))
        shouldSelect.Checked.Add (fun _ -> onViewUpdated state)
        shouldSelect.Unchecked.Add (fun _ -> update rowId (fun a -> {a with shouldSelect = false}))
        shouldSelect.Unchecked.Add (fun _ -> onViewUpdated state)
        grid.Children.Add(shouldSelect)

        let condition = DropDown()
        condition.SetValue(Grid.ColumnProperty, 2) |> ignore
        condition.SetValue(Grid.RowProperty, rowId) |> ignore
        condition.Items <- " "::(column.supportedConditions |> List.map Condition.toString)
        condition.SelectedIndex <- 0
        condition.SelectionChanged.Add (fun _ -> update rowId (fun a -> {a with condition = Condition.fromString (unbox condition.SelectedItem) }))
        condition.SelectionChanged.Add (fun _ -> onViewUpdated state)
        grid.Children.Add(condition)

        let filterValue = TextBox()
        filterValue.SetValue(Grid.ColumnProperty, 3) |> ignore
        filterValue.SetValue(Grid.RowProperty, rowId) |> ignore
        filterValue.GetObservable(TextBox.TextProperty).Subscribe (fun text -> update rowId (fun a -> {a with  conditionValue = a.toValue text})) |> ignore
        filterValue.TextInput.Add (fun _ -> onViewUpdated state)
        grid.Children.Add(filterValue)


    member _.SetColumns (columns: Column list) =
        clear()
        state <- columns
        for c in columns do addFilter c
    member _.OnViewUpdated 
        with get() = onViewUpdated 
        and set(v) = onViewUpdated <- v
    member _.Columns with get() = state


        
