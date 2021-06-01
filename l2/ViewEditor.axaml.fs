namespace l2

open System
open Avalonia
open Avalonia.Markup.Xaml
open Avalonia.Controls
open Avalonia.Media
open Infrastracture


type ViewEditor() as this =
    inherit UserControl()
    do AvaloniaXamlLoader.Load(this)

    let grid = this.FindControl<Grid>("itemGrid")

    let mutable state:Column list = []
    let mutable onViewCreated:(string->Column list->unit) = fun _ _ -> ()

    do this.FindControl<Button>("createView").Click.Add (fun _ ->
        let textbox = this.FindControl<TextBox>("viewName")
        if String.IsNullOrWhiteSpace textbox.Text 
        then ()
        else
            onViewCreated (textbox.Text) state
            textbox.Text <- String.Empty
    )


    let clear() =
        state <- []
        grid.Children.Clear()
        grid.RowDefinitions.Clear()

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
        shouldSelect.Unchecked.Add (fun _ -> update rowId (fun a -> {a with shouldSelect = false}))
        grid.Children.Add(shouldSelect)

        let condition = DropDown()
        condition.SetValue(Grid.ColumnProperty, 2) |> ignore
        condition.SetValue(Grid.RowProperty, rowId) |> ignore
        condition.Items <- " "::(column.supportedConditions |> List.map Condition.toString)
        condition.SelectedIndex <- 0
        condition.SelectionChanged.Add (fun _ -> update rowId (fun a -> {a with condition = Condition.fromString (unbox condition.SelectedItem) }))
        grid.Children.Add(condition)

        let filterValue = TextBox()
        filterValue.SetValue(Grid.ColumnProperty, 3) |> ignore
        filterValue.SetValue(Grid.RowProperty, rowId) |> ignore
        filterValue.GetObservable(TextBox.TextProperty).Subscribe (fun text -> update rowId (fun a -> {a with  conditionValue = a.toValue text})) |> ignore
        grid.Children.Add(filterValue)


    member _.SetColumns (columns: Column list) =
        clear()
        state <- columns
        for c in columns do addFilter c
    member _.OnViewCreated 
        with get() = onViewCreated 
        and set(v) = onViewCreated <- v
    member _.Columns with get() = state


        
